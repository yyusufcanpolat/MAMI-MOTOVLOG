using System.Security.Claims;
using System.Security.Cryptography;
using MamiMotovlog.Data;
using MamiMotovlog.Models;
using MamiMotovlog.Services;
using MamiMotovlog.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class AdminController(SiteDbContext db, IWebHostEnvironment env, RiderDbContext riderDb, IConfiguration config) : Controller
{
    private static readonly Dictionary<string, string> Kinds = new()
    {
        ["videolar"] = "Videolar", ["rotalar"] = "Gezi Rotaları", ["albumler"] = "Fotoğraf Albümleri", ["ekipmanlar"] = "Ekipmanlar"
    };
    private static bool IsHttpsUrl(string? url) => string.IsNullOrWhiteSpace(url) ||
        (Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps);

    [HttpGet("/yonetim/giris")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Dashboard));
        ViewData["Title"] = "Yönetici Girişi";
        return View(new LoginVm());
    }
    [HttpPost("/yonetim/giris")]
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        ViewData["Title"] = "Yönetici Girişi";
        if (!ModelState.IsValid) return View(vm);
        var account = await db.AdminAccounts.FirstOrDefaultAsync();
        if (account is null)
        {
            ModelState.AddModelError("", "Şifre hatalı.");
            return View(vm);
        }
        if (account.LockedUntilUtc is not null && account.LockedUntilUtc > DateTime.UtcNow)
        {
            ModelState.AddModelError("", "Çok sayıda başarısız deneme. Bir süre sonra yeniden deneyin.");
            return View(vm);
        }
        var result = new PasswordHasher<AdminAccount>().VerifyHashedPassword(account, account.PasswordHash, vm.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            account.FailedAttempts++;
            if (account.FailedAttempts >= 5) { account.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15); account.FailedAttempts = 0; }
            await db.SaveChangesAsync();
            ModelState.AddModelError("", "Şifre hatalı.");
            return View(vm);
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = new PasswordHasher<AdminAccount>().HashPassword(account, vm.Password);
        account.FailedAttempts = 0;
        account.LockedUntilUtc = null;
        await db.SaveChangesAsync();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, "Site Yöneticisi"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("admin_stamp", AdminSessionStamp.FromHash(account.PasswordHash))
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToAction(nameof(Dashboard));
    }
    [HttpPost("/yonetim/cikis")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
    [HttpGet("/yonetim")]
    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Yönetim Paneli";
        if(config.GetValue<bool>("Rider:Enabled"))
        {
            ViewBag.MemberCount=await riderDb.Users.CountAsync();
            ViewBag.BikeCount=await riderDb.RiderBikes.CountAsync();
            ViewBag.RouteCount=await riderDb.RiderTrips.CountAsync();
            ViewBag.PendingCount=await riderDb.RiderTrips.CountAsync(x=>x.IsPublic&&!x.IsApproved);
        }
        return View(new DashboardVm
        {
            Videos = await db.Videos.CountAsync(),
            Rides = await db.Rides.CountAsync(),
            Albums = await db.Albums.CountAsync(),
            Messages = await db.Messages.CountAsync(x => !x.IsRead),
            RecentMessages = await db.Messages.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync()
        });
    }
    [HttpGet("/yonetim/icerikler/{kind}")]
    public async Task<IActionResult> List(string kind)
    {
        if (!Kinds.TryGetValue(kind, out var name)) return NotFound();
        var items = kind switch
        {
            "videolar" => await db.Videos.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new ListItemVm { Id = x.Id, Title = x.Title, Summary = x.Summary, CreatedAt = x.CreatedAt, Published = x.IsPublished }).ToListAsync(),
            "rotalar" => await db.Rides.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new ListItemVm { Id = x.Id, Title = x.Title, Summary = x.Summary, CreatedAt = x.CreatedAt, Published = x.IsPublished }).ToListAsync(),
            "albumler" => await db.Albums.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new ListItemVm { Id = x.Id, Title = x.Title, Summary = x.Description, CreatedAt = x.CreatedAt, Published = x.IsPublished }).ToListAsync(),
            _ => (await db.Gear.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync()).Select(x => new ListItemVm { Id = x.Id, Title = x.Name, Summary = x.Summary, CreatedAt = DateTime.MinValue, Published = x.IsPublished }).ToList()
        };
        ViewData["Title"] = name;
        return View(new AdminListVm { Kind = kind, KindTitle = name, Items = items });
    }
    [HttpGet("/yonetim/duzenle/{kind}/{id:int?}")]
    public async Task<IActionResult> Edit(string kind, int? id)
    {
        if (!Kinds.ContainsKey(kind)) return NotFound();
        var vm = new AdminEditVm { Kind = kind, Id = id, Published = true };
        if (id.HasValue)
        {
            switch (kind)
            {
                case "videolar":
                    var v = await db.Videos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (v is null) return NotFound();
                    vm.Title = v.Title; vm.Summary = v.Summary; vm.Link = v.YouTubeUrl; vm.RideId = v.RideId; vm.Published = v.IsPublished;
                    break;
                case "rotalar":
                    var r = await db.Rides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (r is null) return NotFound();
                    vm.Title = r.Title; vm.Summary = r.Summary; vm.Body = r.Story; vm.Start = r.StartPoint; vm.End = r.EndPoint;
                    vm.Stops = r.Stops; vm.DistanceKm = r.DistanceKm; vm.MapUrl = r.MapUrl; vm.Published = r.IsPublished;
                    break;
                case "albumler":
                    var a = await db.Albums.AsNoTracking().Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (a is null) return NotFound();
                    vm.Title = a.Title; vm.Summary = a.Description; vm.RideId = a.RideId; vm.Photos = a.Photos.OrderBy(x => x.SortOrder).ToList(); vm.Published = a.IsPublished;
                    break;
                case "ekipmanlar":
                    var g = await db.Gear.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (g is null) return NotFound();
                    vm.Title = g.Name; vm.Summary = g.Summary; vm.Body = g.Review; vm.Link = g.ExternalUrl;
                    vm.Category = g.Category; vm.Published = g.IsPublished;
                    break;
            }
        }
        vm.Rides = await db.Rides.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
        ViewData["Title"] = id is null ? "Yeni İçerik" : "İçeriği Düzenle";
        return View(vm);
    }
    [HttpPost("/yonetim/duzenle/{kind}/{id:int?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string kind, int? id, AdminEditVm vm)
    {
        if (!Kinds.ContainsKey(kind)) return NotFound();
        vm.Kind = kind; vm.Id = id;
        if (string.IsNullOrWhiteSpace(vm.Title) || (vm.Title?.Length ?? 0) > 130) ModelState.AddModelError(nameof(vm.Title), "Başlık 1–130 karakter olmalı.");
        if ((vm.Summary?.Length ?? 0) > 550) ModelState.AddModelError(nameof(vm.Summary), "Açıklama en fazla 550 karakter olabilir.");
        if (kind == "videolar" && (YouTubeParser.GetVideoId(vm.Link) is null || (vm.Link?.Length ?? 0)>300)) ModelState.AddModelError(nameof(vm.Link), "Geçerli bir HTTPS YouTube video bağlantısı girin.");
        if (kind == "rotalar" && (vm.DistanceKm is < 0 or > 100000)) ModelState.AddModelError(nameof(vm.DistanceKm), "Mesafe geçerli olmalı.");
        if (kind == "rotalar" && !IsHttpsUrl(vm.MapUrl)) ModelState.AddModelError(nameof(vm.MapUrl), "Harita bağlantısı HTTPS ile başlamalı.");
        if (kind == "ekipmanlar" && !IsHttpsUrl(vm.Link)) ModelState.AddModelError(nameof(vm.Link), "Ürün bağlantısı HTTPS ile başlamalı.");
        if (vm.RideId.HasValue && !await db.Rides.AnyAsync(x => x.Id == vm.RideId.Value)) ModelState.AddModelError(nameof(vm.RideId), "Geçerli bir rota seçin.");
        if (!ModelState.IsValid)
        {
            vm.Rides = await db.Rides.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
            if (kind == "albumler" && id.HasValue)
                vm.Photos = await db.AlbumPhotos.AsNoTracking().Where(x => x.AlbumId == id.Value).OrderBy(x => x.SortOrder).ToListAsync();
            ViewData["Title"] = "İçeriği Düzenle";
            return View(vm);
        }
        if ((vm.Start?.Length ?? 0) > 130 || (vm.End?.Length ?? 0) > 130 || (vm.Stops?.Length ?? 0) > 350 || (vm.MapUrl?.Length ?? 0) > 500 || (vm.Link?.Length ?? 0) > 500 || (vm.Category?.Length ?? 0) > 40)
        {
            ModelState.AddModelError("", "Bir veya daha fazla alan izin verilen karakter uzunluğunu aşıyor.");
            vm.Rides = await db.Rides.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
            if (kind == "albumler" && id.HasValue)
                vm.Photos = await db.AlbumPhotos.AsNoTracking().Where(x => x.AlbumId == id.Value).OrderBy(x => x.SortOrder).ToListAsync();
            return View(vm);
        }
        var title = (vm.Title ?? "").Trim();
        switch (kind)
        {
            case "videolar":
                var v = id.HasValue ? await db.Videos.FindAsync(id.Value) : new Video();
                if (v is null) return NotFound();
                v.Title = title; v.Summary = (vm.Summary ?? "").Trim(); v.YouTubeUrl = (vm.Link ?? "").Trim(); v.RideId = vm.RideId; v.IsPublished = vm.Published;
                if (!id.HasValue) db.Videos.Add(v);
                break;
            case "rotalar":
                var r = id.HasValue ? await db.Rides.FindAsync(id.Value) : new Ride();
                if (r is null) return NotFound();
                r.Title = title; r.Summary = (vm.Summary ?? "").Trim(); r.Story = (vm.Body ?? "").Trim(); r.StartPoint = (vm.Start ?? "").Trim();
                r.EndPoint = (vm.End ?? "").Trim(); r.Stops = (vm.Stops ?? "").Trim(); r.DistanceKm = vm.DistanceKm; r.MapUrl = (vm.MapUrl ?? "").Trim(); r.IsPublished = vm.Published;
                if (!id.HasValue) db.Rides.Add(r);
                break;
            case "albumler":
                var a = id.HasValue ? await db.Albums.FindAsync(id.Value) : new Album();
                if (a is null) return NotFound();
                a.Title = title; a.Description = (vm.Summary ?? "").Trim(); a.RideId = vm.RideId; a.IsPublished = vm.Published;
                if (!id.HasValue) db.Albums.Add(a);
                break;
            case "ekipmanlar":
                var g = id.HasValue ? await db.Gear.FindAsync(id.Value) : new Gear();
                if (g is null) return NotFound();
                g.Name = title; g.Summary = (vm.Summary ?? "").Trim(); g.Review = (vm.Body ?? "").Trim(); g.ExternalUrl = (vm.Link ?? "").Trim();
                g.Category = string.IsNullOrWhiteSpace(vm.Category) ? "EKİPMAN" : (vm.Category ?? "").Trim().ToUpperInvariant(); g.IsPublished = vm.Published;
                if (!id.HasValue) { g.SortOrder = await db.Gear.CountAsync() + 1; db.Gear.Add(g); }
                break;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "İçerik kaydedildi.";
        return RedirectToAction(nameof(List), new { kind });
    }
    [HttpPost("/yonetim/sil/{kind}/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, int id)
    {
        if (!Kinds.ContainsKey(kind)) return NotFound();
        var removedFiles = new List<string>();
        switch (kind)
        {
            case "videolar":
                var v = await db.Videos.FindAsync(id); if (v is null) return NotFound(); db.Videos.Remove(v); break;
            case "rotalar":
                var r = await db.Rides.FindAsync(id); if (r is null) return NotFound(); db.Rides.Remove(r); break;
            case "albumler":
                var a = await db.Albums.Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id);
                if (a is null) return NotFound();
                removedFiles.AddRange(a.Photos.Select(p => p.ImagePath));
                db.Albums.Remove(a); break;
            case "ekipmanlar":
                var g = await db.Gear.FindAsync(id); if (g is null) return NotFound(); db.Gear.Remove(g); break;
        }
        await db.SaveChangesAsync();
        foreach (var path in removedFiles) DeleteUploadedFile(path);
        TempData["Success"] = "İçerik silindi.";
        return RedirectToAction(nameof(List), new { kind });
    }
    [HttpPost("/yonetim/album/{id:int}/fotograf")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(45_000_000)]
    public async Task<IActionResult> UploadPhoto(int id, List<IFormFile> images)
    {
        var album = await db.Albums.Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id);
        if (album is null) return NotFound();
        if (images is null || images.Count is 0 or > 6) { TempData["Error"] = "Tek seferde 1–6 fotoğraf seçin."; return RedirectToAction(nameof(Edit), new { kind = "albumler", id }); }
        var uploadPath = Path.Combine(env.WebRootPath, "uploads");
        var created = new List<string>();
        var order = album.Photos.Count == 0 ? 0 : album.Photos.Max(x => x.SortOrder) + 1;
        try
        {
            Directory.CreateDirectory(uploadPath);
            foreach (var file in images)
            {
                if (file.Length < 12 || file.Length > 7_000_000) throw new ArgumentException("Fotoğraflar 7 MB altında olmalı.");
                await using var input = file.OpenReadStream();
                var header = new byte[12];
                await input.ReadExactlyAsync(header);
                var ext = header[0] == 255 && header[1] == 216 && header[2] == 255 ? ".jpg" :
                    header.AsSpan(0, 8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) ? ".png" :
                    System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4)=="WEBP" ? ".webp" : null;
                if (ext is null) throw new ArgumentException("Sadece JPG, PNG ve WEBP kabul edilir.");
                var filename = Guid.NewGuid().ToString("N") + ext;
                input.Position = 0;
                var destination = Path.Combine(uploadPath, filename);
                created.Add("/uploads/" + filename);
                await using (var output = new FileStream(destination, FileMode.CreateNew)) await input.CopyToAsync(output);
                var caption = Path.GetFileNameWithoutExtension(file.FileName);
                db.AlbumPhotos.Add(new AlbumPhoto { AlbumId = id, ImagePath = created[^1], SortOrder = order++, Caption = caption.Length > 180 ? "Fotoğraf" : caption });
            }
            await db.SaveChangesAsync();
            TempData["Success"] = "Fotoğraflar eklendi.";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or DbUpdateException)
        {
            foreach (var path in created) DeleteUploadedFile(path);
            TempData["Error"] = ex is ArgumentException ? ex.Message : "Fotoğraf yüklemesi tamamlanamadı; tekrar deneyin.";
        }
        return RedirectToAction(nameof(Edit), new { kind = "albumler", id });
    }
    [HttpPost("/yonetim/fotograf/{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var photo = await db.AlbumPhotos.FindAsync(id);
        if (photo is null) return NotFound();
        var albumId = photo.AlbumId;
        db.AlbumPhotos.Remove(photo);
        await db.SaveChangesAsync();
        DeleteUploadedFile(photo.ImagePath);
        TempData["Success"] = "Fotoğraf kaldırıldı.";
        return RedirectToAction(nameof(Edit), new { kind = "albumler", id = albumId });
    }
    private void DeleteUploadedFile(string path)
    {
        if (!path.StartsWith("/uploads/", StringComparison.Ordinal) || Path.GetFileName(path) != path[9..]) return;
        var target = Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(path));
        try { if (System.IO.File.Exists(target)) System.IO.File.Delete(target); }
        catch (IOException) { TempData["Error"] = "Kayıt silindi ancak görsel dosyası temizlenemedi."; }
        catch (UnauthorizedAccessException) { TempData["Error"] = "Kayıt silindi ancak görsel dosyası temizlenemedi."; }
    }
    [HttpGet("/yonetim/ayarlar")]
    public async Task<IActionResult> Settings()
    {
        ViewData["Title"] = "Site Ayarları";
        return View(await db.Settings.FirstAsync());
    }
    [HttpPost("/yonetim/ayarlar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SiteSettings form)
    {
        ViewData["Title"] = "Site Ayarları";
        if (!IsHttpsUrl(form.YouTubeChannelUrl)) ModelState.AddModelError(nameof(form.YouTubeChannelUrl), "HTTPS adresi girin.");
        if (!IsHttpsUrl(form.InstagramUrl)) ModelState.AddModelError(nameof(form.InstagramUrl), "HTTPS adresi girin.");
        if (!IsHttpsUrl(form.TikTokUrl)) ModelState.AddModelError(nameof(form.TikTokUrl), "HTTPS adresi girin.");
        if (!string.IsNullOrWhiteSpace(form.ContactEmail) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(form.ContactEmail))
            ModelState.AddModelError(nameof(form.ContactEmail), "Geçerli e-posta adresi girin.");
        if (!ModelState.IsValid) return View(form);
        var settings = await db.Settings.FirstAsync();
        settings.YouTubeChannelUrl = form.YouTubeChannelUrl?.Trim() ?? "";
        settings.InstagramUrl = form.InstagramUrl?.Trim() ?? "";
        settings.TikTokUrl = form.TikTokUrl?.Trim() ?? "";
        settings.ContactEmail = form.ContactEmail?.Trim() ?? "";
        settings.HeroTitle = form.HeroTitle?.Trim() ?? "";
        settings.HeroDescription = form.HeroDescription?.Trim() ?? "";
        settings.AboutText = form.AboutText?.Trim() ?? "";
        settings.MotorcycleTitle = form.MotorcycleTitle?.Trim() ?? "";
        settings.MotorcycleText = form.MotorcycleText?.Trim() ?? "";
        settings.GuideTitle = form.GuideTitle?.Trim() ?? "";
        settings.GuideText = form.GuideText?.Trim() ?? "";
        var oldCover = settings.HeroImage;
        if (string.IsNullOrWhiteSpace(form.HeroImage)) settings.HeroImage = "";
        await db.SaveChangesAsync();
        if (oldCover != settings.HeroImage) DeleteCoverFile(oldCover);
        TempData["Success"] = "Site ayarları kaydedildi.";
        return RedirectToAction(nameof(Settings));
    }
    [HttpPost("/yonetim/kapak")][ValidateAntiForgeryToken][RequestSizeLimit(7_500_000)]
    public async Task<IActionResult> HeroImage(IFormFile? image)
    {
        if(image is null || image.Length<12 || image.Length>7_000_000) {TempData["Error"]="7 MB altında bir fotoğraf seçin."; return RedirectToAction(nameof(Settings));}
        await using var input=image.OpenReadStream();
        var header=new byte[12];
        await input.ReadExactlyAsync(header);
        var ext=header[0]==255 && header[1]==216 && header[2]==255 ? ".jpg" : header.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})?".png":System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4)=="WEBP"?".webp":null;
        if(ext is null){TempData["Error"]="JPG, PNG veya WebP seçin.";return RedirectToAction(nameof(Settings));}
        var dir=Path.Combine(env.WebRootPath,"uploads","covers"); Directory.CreateDirectory(dir);
        var name=Guid.NewGuid().ToString("N")+ext; input.Position=0;
        await using(var output=System.IO.File.Create(Path.Combine(dir,name))) await input.CopyToAsync(output);
        var settings=await db.Settings.FirstAsync();var oldCover=settings.HeroImage;settings.HeroImage="/uploads/covers/"+name;
        try { await db.SaveChangesAsync(); } catch { DeleteCoverFile(settings.HeroImage); throw; }
        DeleteCoverFile(oldCover);TempData["Success"]="Kapak görseli güncellendi.";return RedirectToAction(nameof(Settings));
    }
    private void DeleteCoverFile(string? path)
    {
        const string prefix = "/uploads/covers/";
        if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.Ordinal) || Path.GetFileName(path) != path[prefix.Length..]) return;
        try { System.IO.File.Delete(Path.Combine(env.WebRootPath, "uploads", "covers", Path.GetFileName(path))); }
        catch (IOException) { TempData["Error"] = "Kapak güncellendi ancak eski dosya temizlenemedi."; }
        catch (UnauthorizedAccessException) { TempData["Error"] = "Kapak güncellendi ancak eski dosya temizlenemedi."; }
    }
    [HttpGet("/yonetim/sifre")]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Şifre Değiştir";
        return View(new ChangePasswordVm());
    }
    [HttpPost("/yonetim/sifre")][EnableRateLimiting("admin-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm vm)
    {
        ViewData["Title"] = "Şifre Değiştir";
        if (!ModelState.IsValid) return View(vm);
        var account = await db.AdminAccounts.SingleAsync();
        var hasher = new PasswordHasher<AdminAccount>();
        var valid = hasher.VerifyHashedPassword(account, account.PasswordHash, vm.CurrentPassword);
        if (valid == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Mevcut şifre doğru değil.");
            return View(vm);
        }
        if (vm.CurrentPassword == vm.NewPassword)
        {
            ModelState.AddModelError(nameof(vm.NewPassword), "Yeni şifre farklı olmalı.");
            return View(vm);
        }
        account.PasswordHash = hasher.HashPassword(account, vm.NewPassword);
        await db.SaveChangesAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Şifren güncellendi. Yeni şifrenle giriş yap.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet("/yonetim/mesajlar")]
    public async Task<IActionResult> Messages()
    {
        ViewData["Title"] = "Gelen Mesajlar";
        return View(await db.Messages.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync());
    }
    [HttpGet("/yonetim/mesajlar/{id:int}")]
    public async Task<IActionResult> Message(int id)
    {
        var message = await db.Messages.FindAsync(id);
        if (message is null) return NotFound();
        message.IsRead = true;
        await db.SaveChangesAsync();
        ViewData["Title"] = "Mesaj Detayı";
        return View(message);
    }
    [HttpPost("/yonetim/mesajlar/{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await db.Messages.FindAsync(id);
        if (message is null) return NotFound();
        db.Messages.Remove(message);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Messages));
    }
}
