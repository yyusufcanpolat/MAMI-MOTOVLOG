using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MamiMotovlog.Services;

// Provides current online Istanbul reference prices (province median, NOT station quotes).
// If the source is stale, the client must NOT present the value as a live pump price.
public sealed class IstanbulFuelPriceService(
    IHttpClientFactory clients, IMemoryCache cache, ILogger<IstanbulFuelPriceService> logger, IConfiguration config)
{
    private const string FeedUrl = "https://guncelakaryakitfiyatlari.com/veri/fiyatlar.json";
    private const string Attribution = "Kaynak: EPDK; derleyen: Güncel Akaryakıt Fiyatları";

    public async Task<FuelQuote?> GetAsync(string side, string type, bool refresh, CancellationToken ct)
    {
        var key = $"mami-fuel-{side}-{type}";
        if (!refresh && cache.TryGetValue(key, out FuelQuote? old) && old is not null && (!old.IsRecent || DateTimeOffset.UtcNow-old.CheckedAt<=TimeSpan.FromHours(72))) return old;
        FuelQuote? stale=null;
        var feeds=new[]{FeedUrl,config["Fuel:BackupFeedUrl"]}.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        foreach(var feed in feeds)
        {
        if(!Uri.TryCreate(feed,UriKind.Absolute,out var uri)||uri.Scheme!="https")continue;
        try
        {
            var client = clients.CreateClient("istanbul-fuel");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > 12_000_000) continue;
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var payload = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) > 0)
            {
                if (payload.Length + read > 12_000_000) throw new InvalidDataException("Yakıt veri yanıtı çok büyük.");
                await payload.WriteAsync(buffer.AsMemory(0, read), deadline.Token);
            }
            payload.Position = 0;
            using var document = await JsonDocument.ParseAsync(payload,
                new JsonDocumentOptions { MaxDepth = 48 }, deadline.Token);
            var root = document.RootElement;
            var slug = side == "anadolu" ? "istanbul-anadolu-yakasi" : "istanbul-avrupa-yakasi";
            foreach (var province in root.GetProperty("provinces").EnumerateArray())
            {
                if (!province.TryGetProperty("slug", out var slugEl) || slugEl.GetString() != slug) continue;
                var median = province.GetProperty("median");
                if (!median.TryGetProperty(type, out var valueEl)) continue;
                var raw = valueEl.ValueKind == JsonValueKind.String ? valueEl.GetString() : valueEl.GetRawText();
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) ||
                    price is < 1 or > 1000) continue;
                var retrieved = root.GetProperty("retrieved_at").GetDateTimeOffset();
                // Consider a feed stale when the source has not checked prices recently.
                // An old publication must not be marketed as a live/current pump price.
                var isRecent = (DateTimeOffset.UtcNow - retrieved).TotalHours <= 72 && retrieved <= DateTimeOffset.UtcNow.AddHours(1);
                var result = new FuelQuote(price, side, type, province.GetProperty("effective").GetString() ?? "",
                    retrieved, isRecent, feed==FeedUrl?Attribution:"Yapılandırılmış alternatif veri kaynağı", feed==FeedUrl?"https://guncelakaryakitfiyatlari.com/acik-veri/":uri.GetLeftPart(UriPartial.Authority));
                if(isRecent){cache.Set(key,result,TimeSpan.FromMinutes(30));return result;}
                if(stale is null || stale.CheckedAt<result.CheckedAt)stale=result;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "İstanbul yakıt veri kaynağı erişilemedi");
        }
        }
        if(stale is not null)cache.Set(key,stale,TimeSpan.FromMinutes(10));
        return stale;
    }
}

public sealed record FuelQuote(decimal Price, string Side, string Type, string EffectiveDate,
    DateTimeOffset CheckedAt, bool IsRecent, string Attribution, string SourceUrl);

public sealed record ManualFuelReference(decimal Price,string Source);
