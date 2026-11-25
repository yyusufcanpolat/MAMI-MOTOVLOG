using System.Security.Cryptography;
using System.Text;

namespace MamiMotovlog.Services;

// All member documents stay OUTSIDE wwwroot. Serve only after ownership checks in a controller.
public sealed class RiderFileStore(IWebHostEnvironment env, ILogger<RiderFileStore> logger)
{
    public string Root => Path.Combine(env.ContentRootPath, "App_Data", "RiderUploads");
    public async Task<string> SaveAsync(IFormFile file, bool allowPdf, CancellationToken ct = default)
    {
        if (file is null || file.Length <= 12 || file.Length > 6_000_000)
            throw new ArgumentException("Dosya 6 MB altında olmalı.");
        var header = new byte[12];
        await using var input = file.OpenReadStream();
        try { await input.ReadExactlyAsync(header, ct); } catch (EndOfStreamException) { throw new ArgumentException("Dosya doğrulanamadı."); }
        string ext;
        if (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) ext = ".jpg";
        else if (header.AsSpan(0, 8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) ext = ".png";
        else if (Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP") ext = ".webp";
        else if (allowPdf && Encoding.ASCII.GetString(header, 0, 4) == "%PDF") ext = ".pdf";
        else throw new ArgumentException("Desteklenen türler: JPG, PNG, WEBP" + (allowPdf ? ", PDF" : "") + ".");
        var name = Convert.ToHexString(RandomNumberGenerator.GetBytes(20)).ToLowerInvariant() + ext;
        Directory.CreateDirectory(Root);
        input.Position = 0;
        try { await using var output = new FileStream(Path.Combine(Root, name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await input.CopyToAsync(output, ct); } catch { Delete(name); throw; }
        return name;
    }
    public FileStream Open(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name || name.Length > 90)
            throw new FileNotFoundException();
        return new FileStream(Path.Combine(Root, name), FileMode.Open, FileAccess.Read, FileShare.Read);
    }
    public void Delete(string? name)
    {
        if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name || name.Length > 90) return;
        var path = Path.Combine(Root, name);
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException ex) { logger.LogWarning(ex,"Dosya kaldırılamadı"); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex,"Dosya silme izni yok"); }
    }
    public static string Mime(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".pdf" => "application/pdf", _ => "application/octet-stream"
    };
}
