using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using MamiMotovlog.Data;
using MamiMotovlog.Models;
using MamiMotovlog.ViewModels;
using MamiMotovlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

public class RiderRoutesController(RiderDbContext db, IHttpClientFactory clients, IConfiguration cfg, ILogger<RiderRoutesController> logger, IWebHostEnvironment environment, IstanbulFuelPriceService fuelPrices) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private const string RiderScheme = "Identity.Application";

    [HttpGet("/rota-planlayici")]
    public async Task<IActionResult> Planner()
    {
        var rider = await HttpContext.AuthenticateAsync(RiderScheme);
        if (rider.Succeeded && rider.Principal is not null) HttpContext.User = rider.Principal;
        ViewData["RoutingAvailable"] = !string.IsNullOrWhiteSpace(cfg["Routing:OsrmBaseUrl"]);
        ViewBag.IsRider = rider.Succeeded;
        var memberId = rider.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.MemberId = memberId;
        var measured = new Dictionary<int, decimal?>();
        if (!string.IsNullOrWhiteSpace(memberId))
        {
            var bikeIds = await db.RiderBikes.AsNoTracking().Where(b => b.OwnerId == memberId).Select(b => b.Id).ToListAsync();
            var fuels = await db.RiderFuels.AsNoTracking().Where(f => bikeIds.Contains(f.BikeId)).ToListAsync();
            foreach (var id in bikeIds)
                measured[id] = FuelConsumptionCalculator.Calculate(fuels.Where(f => f.BikeId == id)).LitresPer100Km;
        }
        ViewBag.MeasuredFuel = measured;
        return View();
    }

    // Uses verified road geometry from the configured OSRM-compatible HTTPS provider.
    // Stops are kept in their chosen order; no straight-line fake routes are produced.
    [HttpGet("/rota-planlayici/hesapla")][EnableRateLimiting("routing")]
    public async Task<IActionResult> Calculate([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] double lat1, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] double lon1, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] double lat2, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] double lon2, string? waypoints, CancellationToken ct)
    {
        if (!ModelState.IsValid || !InTurkeyBounds(lat1, lon1) || !InTurkeyBounds(lat2, lon2))
            return BadRequest(new { error = "Başlangıç ve varış Türkiye harita alanında olmalı." });

        var endpoints = new[] { cfg["Routing:OsrmBaseUrl"], cfg["Routing:BackupOsrmBaseUrl"] }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .ToList();
        if (endpoints.Count == 0) return StatusCode(503, new { error = "Yönlendirme servisi henüz yapılandırılmadı." });

        var inv = CultureInfo.InvariantCulture;
        var points = new List<(double Lat, double Lon)> { (lat1, lon1) };
        if (!string.IsNullOrWhiteSpace(waypoints))
        {
            var stops = waypoints.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (stops.Length > 5) return BadRequest(new { error = "En fazla 5 ara durak ekleyebilirsin." });
            foreach (var stop in stops)
            {
                var pair = stop.Split(',');
                if (pair.Length != 2 ||
                    !double.TryParse(pair[0], NumberStyles.Float, inv, out var lat) ||
                    !double.TryParse(pair[1], NumberStyles.Float, inv, out var lon) ||
                    !InTurkeyBounds(lat, lon))
                    return BadRequest(new { error = "Ara durak koordinatları geçersiz." });
                points.Add((lat, lon));
            }
        }
        points.Add((lat2, lon2));
        if (points.Zip(points.Skip(1)).Any(p => p.First == p.Second))
            return BadRequest(new { error = "Ardışık rota noktaları farklı olmalıdır." });

        string CoordinateString(IEnumerable<(double Lat, double Lon)> pts) =>
            string.Join(";", pts.Select(x => $"{x.Lon.ToString(inv)},{x.Lat.ToString(inv)}"));

        var path = $"/route/v1/driving/{CoordinateString(points)}?overview=full&geometries=geojson&steps=false&alternatives=true";
        var client = clients.CreateClient("rider-routing");
        var failed = new List<string>();
        foreach (var endpoint in endpoints)
        {
            try
            {
                var found = await FetchOptions(endpoint + path, client, ct);
                if (found.Count == 0 && points.Count > 2)
                    found = await FetchOptions(endpoint + path.Replace("&alternatives=true", "&alternatives=false"), client, ct);
                if (found.Count != 0)
                {
                    var first = found[0];
                    return Json(new
                    {
                        distanceKm = first.DistanceKm, durationMinutes = first.DurationMinutes,
                        geometry = first.Geometry,
                        alternatives = found.Select((r, index) => new
                        {
                            id = index, label = index == 0 ? "İlk güzergâh" : $"Alternatif {index}",
                            distanceKm = r.DistanceKm, durationMinutes = r.DurationMinutes,
                            geometry = r.Geometry
                        }),
                        stopCount = points.Count - 2,
                        provider = new Uri(endpoint).Host
                    });
                }
                failed.Add($"{new Uri(endpoint).Host}: rota bulunamadı veya yanıt kullanılamıyor");
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("OSRM sağlayıcısı zaman aşımı: {Host}", new Uri(endpoint).Host);
                failed.Add($"{new Uri(endpoint).Host}: zaman aşımı");
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "OSRM güzergâh hatası: {Host}", new Uri(endpoint).Host);
                failed.Add($"{new Uri(endpoint).Host}: {(ex is HttpRequestException ? ex.Message : ex.GetType().Name)}");
            }
        }
        // A single multi-stop routing request can fail on some providers. Retry each road leg;
        // a continuous route is returned only if EVERY leg can be calculated on real roads.
        if (points.Count > 2)
        {
            foreach (var endpoint in endpoints)
            {
                try
                {
                    double distance = 0;
                    int minutes = 0;
                    var joined = new List<JsonElement>();
                    var allLegsFound = true;
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var segment = $"/route/v1/driving/{CoordinateString(new[] { points[i], points[i + 1] })}?overview=full&geometries=geojson&steps=false";
                        var options = await FetchOptions(endpoint + segment, client, ct);
                        if (options.Count == 0) { allLegsFound = false; break; }
                        var leg = options[0];
                        distance += leg.DistanceKm;
                        minutes += leg.DurationMinutes;
                        var coords = leg.Geometry.GetProperty("coordinates").EnumerateArray();
                        foreach (var coord in coords)
                        {
                            if (joined.Count > 0 && joined[^1].GetRawText() == coord.GetRawText()) continue;
                            joined.Add(coord.Clone());
                        }
                    }
                    if (!allLegsFound || joined.Count < 2) continue;
                    var joinedGeometry = JsonSerializer.SerializeToElement(new { type = "LineString", coordinates = joined });
                    if (joinedGeometry.GetRawText().Length > 1_800_000) continue;
                    return Json(new
                    {
                        distanceKm = Math.Round(distance, 1), durationMinutes = minutes,
                        geometry = joinedGeometry,
                        alternatives = new[] { new { id = 0, label = "Tüm duraklardan geçen rota", distanceKm = Math.Round(distance, 1), durationMinutes = minutes, geometry = joinedGeometry } },
                        stopCount = points.Count - 2, provider = new Uri(endpoint).Host,
                        note = "Ara duraklar gerçek yol segmentleriyle birleştirildi."
                    });
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Ara durak yedek güzergâh hatası {Provider}", new Uri(endpoint).Host);
                }
            }
        }

        var message = "Rota hesaplanamadı. Yol ağı üzerindeki noktaları deneyebilirsin; harita üzerinden düz çizgi üretmiyoruz.";
        if (environment.IsDevelopment() && failed.Count > 0)
            message += " Tanı: " + string.Join(" | ", failed);
        return StatusCode(502, new { error = message });
    }

    private sealed record RouteOption(double DistanceKm, int DurationMinutes, JsonElement Geometry);

    private static async Task<List<RouteOption>> FetchOptions(string url, HttpClient client, CancellationToken ct)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
        if (response.Content.Headers.ContentLength > 4_000_000) return new List<RouteOption>();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct),
            new JsonDocumentOptions { MaxDepth = 32 }, ct);
        var root = json.RootElement;
        if (!root.TryGetProperty("code", out var code) || code.GetString() != "Ok" ||
            !root.TryGetProperty("routes", out var array) || array.ValueKind != JsonValueKind.Array)
            return new List<RouteOption>();
        var result = new List<RouteOption>();
        foreach (var item in array.EnumerateArray().Take(3))
        {
            if (!item.TryGetProperty("geometry", out var geometry) ||
                geometry.ValueKind != JsonValueKind.Object ||
                !geometry.TryGetProperty("type", out var kind) || kind.GetString() != "LineString" ||
                !geometry.TryGetProperty("coordinates", out var coords) || coords.GetArrayLength() < 2 ||
                geometry.GetRawText().Length > 1_800_000) continue;
            var km = item.GetProperty("distance").GetDouble() / 1000;
            var min = (int)Math.Ceiling(item.GetProperty("duration").GetDouble() / 60);
            if (km <= 0 || km > 7000 || min <= 0 || min > 20000) continue;
            result.Add(new RouteOption(Math.Max(0.1, Math.Round(km, 1)), min, geometry.Clone()));
        }
        return result;
    }

    [HttpGet("/rota-planlayici/istanbul-yakit")][EnableRateLimiting("fuel")]
    public async Task<IActionResult> IstanbulFuel(string side = "anadolu", string type = "benzin", bool refresh = false, CancellationToken ct = default)
    {
        if (side is not ("anadolu" or "avrupa") || type != "benzin")
            return BadRequest(new { error = "Geçersiz İstanbul yakası veya yakıt türü." });
        var result = await fuelPrices.GetAsync(side, type, refresh, ct);
        if(result is null || !result.IsRecent)
        {
            var entry=await db.PlatformEntries.AsNoTracking().FirstOrDefaultAsync(x=>x.Key=="fuel:"+side,ct);
            if(entry is not null && entry.UpdatedUtc>=DateTime.UtcNow.AddHours(-24) && entry.UpdatedUtc<=DateTime.UtcNow)
            {
                ManualFuelReference? manual = null;
                try { manual = JsonSerializer.Deserialize<ManualFuelReference>(entry.Value); }
                catch (JsonException ex) { logger.LogWarning(ex, "Geçersiz yönetici yakıt referansı"); }
                if(manual is not null && manual.Price is >=1 and <=1000)
                    return Json(new FuelQuote(manual.Price,side,type,entry.UpdatedUtc.AddHours(3).ToString("yyyy-MM-dd"),new DateTimeOffset(DateTime.SpecifyKind(entry.UpdatedUtc,DateTimeKind.Utc)),true,"Yönetici doğrulaması: "+manual.Source,"https://mamimotovlog.com/"));
            }
        }
        if (result is null) return StatusCode(503, new { error = "Güncel fiyat kaynağına erişilemedi. Litre fiyatını elle girebilirsin." });
        return Json(result);
    }

    [HttpGet("/rota-planlayici/hava")][EnableRateLimiting("weather")]
    public async Task<IActionResult> Weather(double lat, double lon, CancellationToken ct)
    {
        if (!ModelState.IsValid || !InTurkeyBounds(lat, lon)) return BadRequest(new { error = "Hava durumu noktası Türkiye harita alanında olmalı." });
        if (!cfg.GetValue<bool>("Weather:Enabled")) return StatusCode(503, new { error = "Hava durumu servisi etkin değil." });
        var inv = CultureInfo.InvariantCulture;
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(inv)}&longitude={lon.ToString(inv)}" +
                  "&current=temperature_2m,precipitation,wind_speed_10m,weather_code" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max,weather_code" +
                  "&forecast_days=3&timezone=Europe%2FIstanbul";
        try
        {
            var client = clients.CreateClient("rider-routing");
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return StatusCode(502, new { error = "Hava durumu alınamadı, tekrar dene." });
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var current = json.RootElement.GetProperty("current");
            var daily = json.RootElement.GetProperty("daily");
            var days = new List<object>();
            var dates = daily.GetProperty("time");
            for (var i = 0; i < Math.Min(3, dates.GetArrayLength()); i++)
                days.Add(new
                {
                    date = dates[i].GetString(),
                    minC = daily.GetProperty("temperature_2m_min")[i].GetDouble(),
                    maxC = daily.GetProperty("temperature_2m_max")[i].GetDouble(),
                    rainProbability = daily.GetProperty("precipitation_probability_max")[i].GetDouble(),
                    code = daily.GetProperty("weather_code")[i].GetInt32()
                });
            return Json(new
            {
                temperatureC = current.GetProperty("temperature_2m").GetDouble(),
                precipitationMm = current.GetProperty("precipitation").GetDouble(),
                windKmh = current.GetProperty("wind_speed_10m").GetDouble(),
                weatherCode = current.GetProperty("weather_code").GetInt32(),
                time = current.GetProperty("time").GetString(), days,
                source = "Open-Meteo"
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Open-Meteo hava durumu erişilemedi");
            return StatusCode(502, new { error = "Hava durumu şu anda alınamıyor; daha sonra tekrar deneyebilirsin." });
        }
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpPost("/rota-planlayici/kaydet")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(RiderTripVm vm)
    {
        if (!ModelState.IsValid)
            return SaveError(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Geçersiz rota verisi." : e.ErrorMessage).Distinct().Take(4)));
        if (!InTurkeyBounds(vm.StartLat, vm.StartLon) || !InTurkeyBounds(vm.EndLat, vm.EndLon) ||
            string.IsNullOrEmpty(vm.GeoJson) || vm.GeoJson.Length > 1_800_000)
            return SaveError("Rota adı ve hesaplanan güzergâhı kontrol edin.");
        try
        {
            using var doc = JsonDocument.Parse(vm.GeoJson);
            if (doc.RootElement.GetProperty("type").GetString() != "LineString" ||
                doc.RootElement.GetProperty("coordinates").GetArrayLength() < 2 ||
                doc.RootElement.GetProperty("coordinates").EnumerateArray().Any(pair => pair.GetArrayLength() != 2 || !Valid(pair[1].GetDouble(), pair[0].GetDouble())))
                return SaveError("Rota geometrisi geçersiz; yeniden hesaplayın.");
        }
        catch (Exception) { return SaveError("Rota geometrisi geçersiz; yeniden hesaplayın."); }
        if (vm.BikeId.HasValue && !await db.RiderBikes.AnyAsync(x => x.Id == vm.BikeId && x.OwnerId == UserId)) return SaveError("Motosiklet kaydı sana ait değil.");
        // Distance/geometry are submitted by the browser: display estimates, not trusted billing/safety facts.
        var trip = new RiderTrip { OwnerId = UserId, Title = vm.Title.Trim(), StartLabel = string.IsNullOrWhiteSpace(vm.StartLabel) ? "Başlangıç" : vm.StartLabel.Trim(), EndLabel = string.IsNullOrWhiteSpace(vm.EndLabel) ? "Varış" : vm.EndLabel.Trim(),
            StartLat = vm.StartLat, StartLon = vm.StartLon, EndLat = vm.EndLat, EndLon = vm.EndLon,
            DistanceKm = vm.DistanceKm, DurationMinutes = vm.DurationMinutes,
            BikeId = vm.BikeId, GeoJson = vm.GeoJson };
        db.RiderTrips.Add(trip);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Rota kaydı başarısız"); return SaveError("Rota kaydedilemedi. Lütfen tekrar deneyin."); }
        TempData["RiderInfo"] = "Rota garajına kaydedildi.";
        if(Request.Headers["X-Mami-Async"]=="1") return Json(new { url=Url.Action(nameof(Trip),new{id=trip.Id}) });
        return RedirectToAction(nameof(Trip), new { id = trip.Id });
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpGet("/garaj/rotalar")]
    public async Task<IActionResult> Trips() => View(await db.RiderTrips.AsNoTracking().Where(x => x.OwnerId == UserId).OrderByDescending(x => x.CreatedUtc).ToListAsync());

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpGet("/garaj/rotalar/{id:int}")]
    public async Task<IActionResult> Trip(int id)
    {
        var item = await db.RiderTrips.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        return item is null ? NotFound() : View(item);
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpPost("/garaj/rotalar/{id:int}/tamamla")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, DateTime date, decimal? spendTry, string? journal)
    {
        var item = await db.RiderTrips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (item is null) return NotFound();
        if (!ModelState.IsValid || date.Date > MamiMotovlog.Services.TurkeyClock.Today || date.Year < 1990 || spendTry is < 0 or > 9999999 || journal?.Length > 2000)
            { TempData["RiderInfo"]="Tarih, masraf ve not alanlarını kontrol edin.";return RedirectToAction(nameof(Trip),new{id}); }
        item.CompletedOn = date.Date; item.ActualSpendTry = spendTry; item.Journal = (journal ?? "").Trim();
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Trip), new { id });
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpPost("/garaj/rotalar/{id:int}/paylas")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int id)
    {
        var item = await db.RiderTrips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (item is null) return NotFound();
        item.IsPublic = !item.IsPublic; item.IsApproved = false; // must re-moderate when republished
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Trip), new { id });
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpPost("/garaj/rotalar/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.RiderTrips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (item is null) return NotFound();
        db.RiderTrips.Remove(item); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Trips));
    }

    [HttpGet("/topluluk/rotalar")]
    public async Task<IActionResult> Community(string? q)
    {
        var query = db.RiderTrips.AsNoTracking().Where(x=>x.IsPublic && x.IsApproved);
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (q.Length>75) return BadRequest("Arama çok uzun.");
            q=q.Trim(); query=query.Where(x=>x.Title.Contains(q)||x.StartLabel.Contains(q)||x.EndLabel.Contains(q));
        }
        ViewBag.Query = q ?? "";
        var rider = (await HttpContext.AuthenticateAsync(RiderScheme)).Principal;
        if (rider?.Identity?.IsAuthenticated == true) HttpContext.User = rider;
        ViewBag.CanFavorite = rider?.Identity?.IsAuthenticated == true;
        if (rider?.Identity?.IsAuthenticated==true)
        {
            var userId = rider.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.FavoriteIds = (await db.RiderFavorites.AsNoTracking().Where(x=>x.OwnerId==userId)
                .Select(x=>x.TripId).ToListAsync()).ToHashSet();
        }
        else ViewBag.FavoriteIds = new HashSet<int>();
        return View(await query.OrderByDescending(x=>x.CreatedUtc).Take(60).ToListAsync());
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpPost("/topluluk/rotalar/{id:int}/favori")][ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var trip = await db.RiderTrips.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id && x.IsPublic && x.IsApproved);
        if (trip is null) return NotFound();
        var record = await db.RiderFavorites.FirstOrDefaultAsync(x=>x.TripId==id && x.OwnerId==UserId);
        if (record is null) db.RiderFavorites.Add(new RiderFavorite { TripId = id, OwnerId = UserId });
        else db.RiderFavorites.Remove(record);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Community));
    }

    [Authorize(AuthenticationSchemes = RiderScheme)]
    [HttpGet("/garaj/favoriler")]
    public async Task<IActionResult> Favorites() => View(await db.RiderFavorites.AsNoTracking()
        .Where(x=>x.OwnerId==UserId && x.Trip.IsPublic && x.Trip.IsApproved)
        .OrderByDescending(x=>x.CreatedUtc).Select(x=>x.Trip).ToListAsync());

    [HttpGet("/topluluk/rotalar/{id:int}")]
    public async Task<IActionResult> CommunityTrip(int id)
    {
        var item = await db.RiderTrips.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsPublic && x.IsApproved);
        return item is null ? NotFound() : View("Trip", item);
    }

    private IActionResult SaveError(string message)
    {
        if(Request.Headers["X-Mami-Async"]=="1") return BadRequest(new{error=message});
        Response.StatusCode = 400;
        return View("SaveError", message);
    }

    // Turkey display envelope (not a cadastral/national border polygon).
    private static bool InTurkeyBounds(double lat, double lon) => Valid(lat, lon) &&
        lat is >= 35.7 and <= 42.3 && lon is >= 25.5 and <= 45.0;
    private static bool Valid(double lat, double lon) => double.IsFinite(lat) && double.IsFinite(lon) && lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
}
