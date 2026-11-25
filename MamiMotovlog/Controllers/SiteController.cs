using MamiMotovlog.Data;
using MamiMotovlog.Models;
using MamiMotovlog.Services;
using MamiMotovlog.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

public class SiteController(SiteDbContext db) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var vm = new HomeVm
        {
            GuideRides = await db.Rides.AsNoTracking().Where(x => x.IsPublished && x.Title.StartsWith("Rota rehberi:")).OrderBy(x => x.Title).Take(5).ToListAsync(),
            Video = await db.Videos.AsNoTracking().Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(),
            Ride = await db.Rides.AsNoTracking().Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(),
            Album = await db.Albums.AsNoTracking().Include(x => x.Photos).Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(),
            Settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync() ?? new SiteSettings()
        };
        ViewData["Title"] = "Ana Sayfa";
        return View(vm);
    }
    [HttpGet("/rehber")]
    public IActionResult Guide() { ViewData["Title"] = "Yol Rehberi"; return View(); }

    [HttpGet("/hakkimda")]
    public IActionResult About() { ViewData["Title"] = "Hakkımda"; return View(); }

    [HttpGet("/motosikletim")]
    public async Task<IActionResult> Motorcycle()
    {
        ViewData["Title"] = "CFMOTO 450SR";
        ViewBag.Videos = await db.Videos.AsNoTracking().Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).Take(3).ToListAsync();
        return View();
    }
    [HttpGet("/ekipmanlarim")]
    public async Task<IActionResult> Gear()
    {
        ViewData["Title"] = "Ekipmanlarım";
        return View(await db.Gear.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync());
    }
    [HttpGet("/videolar")]
    public async Task<IActionResult> Videos()
    {
        ViewData["Title"] = "Motovlog Videoları";
        ViewBag.YouTubeChannelUrl = (await db.Settings.AsNoTracking().FirstOrDefaultAsync())?.YouTubeChannelUrl ?? "https://www.youtube.com/@mami500rvm";
        return View(await db.Videos.AsNoTracking().Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).ToListAsync());
    }
    [HttpGet("/videolar/{id:int}")]
    public async Task<IActionResult> Video(int id)
    {
        var video = await db.Videos.AsNoTracking().Include(x => x.Ride).FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
        if (video is null) return NotFound();
        if (video.Ride is { IsPublished: false }) video.Ride = null;
        var youtubeId = YouTubeParser.GetVideoId(video.YouTubeUrl);
        if (youtubeId is null) return NotFound();
        var album = video.RideId is null ? null : await db.Albums.AsNoTracking().Where(x => x.RideId == video.RideId && x.IsPublished).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        ViewData["Title"] = video.Title;
        return View(new DetailVideoVm { Video = video, YoutubeId = youtubeId, LinkedAlbum = album });
    }
    [HttpGet("/rotalar")]
    public async Task<IActionResult> Rides()
    {
        ViewData["Title"] = "Gezi Rotalarım";
        return View(await db.Rides.AsNoTracking().Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).ToListAsync());
    }
    [HttpGet("/rotalar/{id:int}")]
    public async Task<IActionResult> Ride(int id)
    {
        var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
        if (ride is null) return NotFound();
        ViewData["Title"] = ride.Title;
        return View(new DetailRideVm
        {
            Ride = ride,
            Videos = await db.Videos.AsNoTracking().Where(x => x.RideId == id && x.IsPublished).OrderByDescending(x => x.CreatedAt).ToListAsync(),
            Albums = await db.Albums.AsNoTracking().Include(x => x.Photos).Where(x => x.RideId == id && x.IsPublished).OrderByDescending(x => x.CreatedAt).ToListAsync()
        });
    }
    [HttpGet("/galeri")]
    public async Task<IActionResult> Gallery()
    {
        ViewData["Title"] = "Fotoğraf Galerisi";
        return View(await db.Albums.AsNoTracking().Include(x => x.Photos).Where(x => x.IsPublished).OrderByDescending(x => x.CreatedAt).ToListAsync());
    }
    [HttpGet("/galeri/{id:int}")]
    public async Task<IActionResult> Album(int id)
    {
        var album = await db.Albums.AsNoTracking().Include(x => x.Photos).Include(x => x.Ride).FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
        if (album is null) return NotFound();
        if (album.Ride is { IsPublished: false }) album.Ride = null;
        ViewData["Title"] = album.Title;
        return View(album);
    }
    [HttpGet("/iletisim")]
    public IActionResult Contact() { ViewData["Title"] = "İletişim"; return View(new ContactVm()); }

    [HttpPost("/iletisim")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("contact")]
    public async Task<IActionResult> Contact(ContactVm vm)
    {
        ViewData["Title"] = "İletişim";
        if (!string.IsNullOrWhiteSpace(vm.Website)) return RedirectToAction(nameof(Thanks));
        if (!ModelState.IsValid) return View(vm);
        db.Messages.Add(new ContactMessage
        {
            Name = vm.Name.Trim(), Email = vm.Email.Trim(), Phone = (vm.Phone ?? "").Trim(), Subject = vm.Subject.Trim(), Message = vm.Message.Trim()
        });
        await db.SaveChangesAsync();
        TempData["ContactSent"] = "true";
        return RedirectToAction(nameof(Thanks));
    }
    [HttpGet("/tesekkurler")]
    public IActionResult Thanks()
    {
        if (TempData["ContactSent"] as string != "true") return RedirectToAction(nameof(Contact));
        ViewData["Title"] = "Mesajın Ulaştı";
        return View();
    }
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 1800)]
    public async Task<IActionResult> Sitemap()
    {
        var paths = new List<string> { "", "/hakkimda", "/motosikletim", "/ekipmanlarim", "/videolar", "/rotalar", "/galeri", "/iletisim", "/rehber" };
        paths.AddRange((await db.Videos.AsNoTracking().Where(x => x.IsPublished).Select(x => x.Id).ToListAsync()).Select(id => "/videolar/" + id));
        paths.AddRange((await db.Rides.AsNoTracking().Where(x => x.IsPublished).Select(x => x.Id).ToListAsync()).Select(id => "/rotalar/" + id));
        paths.AddRange((await db.Albums.AsNoTracking().Where(x => x.IsPublished).Select(x => x.Id).ToListAsync()).Select(id => "/galeri/" + id));
        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"
            + string.Join("", paths.Select(x => "<url><loc>https://mamimotovlog.com" + x + "</loc></url>")) + "</urlset>";
        return Content(xml, "application/xml; charset=utf-8");
    }

    [Route("/hata")]
    [AllowAnonymous]
    public IActionResult Error() { ViewData["Title"] = "Bir Hata Oluştu"; return View(); }
}
