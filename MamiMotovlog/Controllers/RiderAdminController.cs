using System.Globalization;
using MamiMotovlog.Data;
using MamiMotovlog.Models;
using MamiMotovlog.Services;
using MamiMotovlog.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Route("yonetim/rider")]
public class RiderAdminController(RiderDbContext db, UserManager<RiderMember> users, RiderDeletion deletion, RiderMailer mail, IWebHostEnvironment env, ILogger<RiderAdminController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Members = await db.Users.CountAsync();
        ViewBag.Bikes = await db.RiderBikes.CountAsync();
        ViewBag.Routes = await db.RiderTrips.CountAsync();
        ViewBag.Pending = await db.RiderTrips.CountAsync(x => x.IsPublic && !x.IsApproved);
        ViewBag.Brands = await db.BikeBrands.AsNoTracking().Include(x=>x.Models).ThenInclude(x=>x.Variants).OrderBy(x=>x.Name).ToListAsync();
        ViewBag.PendingRoutes = await db.RiderTrips.AsNoTracking().Where(x=>x.IsPublic && !x.IsApproved)
            .OrderByDescending(x=>x.CreatedUtc).Take(30).ToListAsync();
        ViewBag.FuelEntries = await db.PlatformEntries.AsNoTracking().Where(x=>x.Key.StartsWith("fuel:")).ToListAsync();
        return View();
    }
    [HttpPost("marka")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBrand(string name)
    {
        name = (name ?? "").Trim();
        if (!ModelState.IsValid || name.Length is < 2 or > 75) return Fail("Marka 2–75 karakter olmalı.");
        if (!await db.BikeBrands.AnyAsync(x => x.Name == name)) { db.BikeBrands.Add(new BikeBrand { Name = name }); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("model")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModel(int brandId, string name)
    {
        name = (name ?? "").Trim();
        if (!ModelState.IsValid || name.Length is < 2 or > 100 || !await db.BikeBrands.AnyAsync(x => x.Id == brandId)) return Fail("Model geçersiz.");
        if (!await db.BikeModels.AnyAsync(x => x.BrandId == brandId && x.Name == name))
        { db.BikeModels.Add(new BikeModel { BrandId = brandId, Name = name }); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("versiyon")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(int modelId, int year, string edition, int? engineCc, decimal? powerHp, decimal? torqueNm, decimal? tankLitres)
    {
        edition = (edition ?? "").Trim();
        if (!ModelState.IsValid || year is < 1950 or > 2035 || edition.Length is < 2 or > 85 || !await db.BikeModels.AnyAsync(x => x.Id == modelId)
            || engineCc is < 1 or > 2500 || powerHp is < 0 or > 1000 || torqueNm is < 0 or > 1000 || tankLitres is < 0.5m or > 150m)
            return Fail("Versiyon bilgileri geçersiz.");
        if (await db.BikeVariants.AnyAsync(x => x.ModelId == modelId && x.Year == year && x.Edition == edition))
            return Fail("Bu yıl ve versiyon zaten kayıtlı.");
        db.BikeVariants.Add(new BikeVariant { ModelId = modelId, Year = year, Edition = edition,
            EngineCc = engineCc, PowerHp = powerHp, TorqueNm = torqueNm, TankLitres = tankLitres });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("katalog-csv")][ValidateAntiForgeryToken][RequestSizeLimit(250_000)]
    public async Task<IActionResult> ImportCatalog(IFormFile file)
    {
        if (file is null || file.Length is < 15 or > 200_000) return Fail("CSV dosyası 200 KB altında olmalı.");
        using var reader = new StreamReader(file.OpenReadStream());
        var header = await reader.ReadLineAsync();
        if (header != "Brand;Model;Year;Edition;EngineCc;PowerHp;TorqueNm;TankLitres")
            return Fail("CSV sütunları şablonla aynı olmalı.");
        var rows=new List<(string Brand,string Model,int Year,string Edition,int? Cc,decimal? Hp,decimal? Torque,decimal? Tank)>();
        for(var lineNo=2;;lineNo++)
        {
            var line=await reader.ReadLineAsync(); if(line is null) break;
            if(lineNo>251) return Fail("Tek seferde en fazla 250 satır yükleyebilirsin.");
            if(string.IsNullOrWhiteSpace(line)) continue;
            var c=line.Split(';'); if(c.Length!=8) return Fail($"Satır {lineNo}: 8 sütun bekleniyor.");
            var brand=c[0].Trim();var model=c[1].Trim();var edition=c[3].Trim();
            if(brand.Length is <2 or >75 || model.Length is <2 or >100 || edition.Length is <2 or >85 || !int.TryParse(c[2],out var year) || year is <1950 or >2035) return Fail($"Satır {lineNo}: ad veya yıl geçersiz.");
            int? cc=int.TryParse(c[4],out var n)?n:null;
            decimal? D(string v)=>decimal.TryParse(v,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var d)?d:null;
            var hp=D(c[5]);var tq=D(c[6]);var tank=D(c[7]);
            if((!string.IsNullOrWhiteSpace(c[4])&&!cc.HasValue)||(!string.IsNullOrWhiteSpace(c[5])&&!hp.HasValue)||(!string.IsNullOrWhiteSpace(c[6])&&!tq.HasValue)||(!string.IsNullOrWhiteSpace(c[7])&&!tank.HasValue)||cc is <1 or >2500||hp is <0 or >1000||tq is <0 or >1000||tank is <0.5m or >150m) return Fail($"Satır {lineNo}: teknik değer geçersiz.");
            rows.Add((brand,model,year,edition,cc,hp,tq,tank));
        }
        var added=0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>{
            db.ChangeTracker.Clear();added=0;
            await using var tx=await db.Database.BeginTransactionAsync();
            foreach(var row in rows)
            {
                var brand=await db.BikeBrands.FirstOrDefaultAsync(x=>x.Name==row.Brand);
                if(brand is null){brand=new BikeBrand{Name=row.Brand};db.BikeBrands.Add(brand);await db.SaveChangesAsync();}
                var model=await db.BikeModels.FirstOrDefaultAsync(x=>x.BrandId==brand.Id&&x.Name==row.Model);
                if(model is null){model=new BikeModel{BrandId=brand.Id,Name=row.Model};db.BikeModels.Add(model);await db.SaveChangesAsync();}
                if(await db.BikeVariants.AnyAsync(x=>x.ModelId==model.Id&&x.Year==row.Year&&x.Edition==row.Edition))continue;
                db.BikeVariants.Add(new BikeVariant{ModelId=model.Id,Year=row.Year,Edition=row.Edition,EngineCc=row.Cc,PowerHp=row.Hp,TorqueNm=row.Torque,TankLitres=row.Tank});await db.SaveChangesAsync();added++;
            }
            await tx.CommitAsync();
        });
        TempData["RiderInfo"]=$"{added} yeni versiyon eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("rota-onay/{id:int}")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, bool approved)
    {
        var trip = await db.RiderTrips.FirstOrDefaultAsync(x => x.Id == id);
        if (trip is null) return NotFound();
        if (approved && !trip.IsPublic) return Fail("Yalnızca kullanıcının paylaşım isteği onaylanabilir.");
        trip.IsApproved = approved; if (!approved) trip.IsPublic = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Fail(string message,string section="")
    { TempData["Error"]=message; return string.IsNullOrEmpty(section)?RedirectToAction(nameof(Index)):Redirect($"/yonetim/rider/{section}"); }
    private IActionResult Done(string message,string section="")
    { TempData["Success"]=message; return string.IsNullOrEmpty(section)?RedirectToAction(nameof(Index)):Redirect($"/yonetim/rider/{section}"); }

    [HttpGet("{section}")]
    public async Task<IActionResult> Records(string section,string? q,int page=1)
    {
        if(section is not ("uyeler" or "motosikletler" or "rotalar" or "islemler")) return NotFound();
        q=(q??"").Trim(); if(q.Length>100) q=q[..100]; page=Math.Clamp(page,1,100000); const int size=30;
        ViewData["Title"]=section switch {"uyeler"=>"Kullanıcı yönetimi","motosikletler"=>"Kayıtlı motosikletler","rotalar"=>"Kayıtlı rotalar",_=>"İşlem geçmişi"};
        if(section=="uyeler"){
            var query=db.Users.AsNoTracking().Where(x=>x.DisplayName.Contains(q)||(x.Email!=null&&x.Email.Contains(q)));
            return View(new AdminPlatformVm{Section=section,Query=q,Page=page,Total=await query.CountAsync(),Members=await query.OrderByDescending(x=>x.JoinedUtc).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync()});
        }
        if(section=="motosikletler"){
            var query=db.RiderBikes.AsNoTracking().Include(x=>x.Owner).Where(x=>x.BrandName.Contains(q)||x.ModelName.Contains(q)||x.Owner.DisplayName.Contains(q)||(x.Owner.Email!=null&&x.Owner.Email.Contains(q)));
            return View(new AdminPlatformVm{Section=section,Query=q,Page=page,Total=await query.CountAsync(),Bikes=await query.OrderByDescending(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync()});
        }
        if(section=="rotalar"){
            var query=db.RiderTrips.AsNoTracking().Where(x=>x.Title.Contains(q)||x.StartLabel.Contains(q)||x.EndLabel.Contains(q));
            // Do not load members' private route geometry or journal in the administration list.
            var items=await query.OrderByDescending(x=>x.Id).Skip((page-1)*size).Take(size).Select(x=>new RiderTrip{Id=x.Id,OwnerId=x.OwnerId,Title=x.Title,DistanceKm=x.DistanceKm,CreatedUtc=x.CreatedUtc,IsPublic=x.IsPublic,IsApproved=x.IsApproved}).ToListAsync();
            var ids=items.Select(x=>x.OwnerId).Distinct().ToList();
            var names=await db.Users.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.DisplayName);
            return View(new AdminPlatformVm{Section=section,Query=q,Page=page,Total=await query.CountAsync(),Trips=items,Names=names});
        }
        var audit=db.AdminAudits.AsNoTracking().Where(x=>x.Operation.Contains(q));
        return View(new AdminPlatformVm{Section=section,Query=q,Page=page,Total=await audit.CountAsync(),Audits=await audit.OrderByDescending(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync()});
    }
    [HttpPost("uye/{id}/durum")][ValidateAntiForgeryToken]
    public async Task<IActionResult> MemberStatus(string id,bool blocked)
    {
        var member=await users.FindByIdAsync(id);if(member is null)return NotFound();
        member.LockoutEnabled=true;member.LockoutEnd=blocked?new DateTimeOffset(2099,1,1,0,0,0,TimeSpan.Zero):null;member.AccessFailedCount=0;
        var result=await users.UpdateAsync(member);if(!result.Succeeded)return Fail("Hesap durumu değiştirilemedi.","uyeler");
        await users.UpdateSecurityStampAsync(member);
        return Done(blocked?"Üye engellendi.":"Üye engeli kaldırıldı.","uyeler");
    }
    [HttpPost("uye/{id}/sifirla")][ValidateAntiForgeryToken]
    public async Task<IActionResult> MemberReset(string id)
    {
        var member=await users.FindByIdAsync(id);if(member is null)return NotFound();
        if(!member.EmailConfirmed||string.IsNullOrWhiteSpace(member.Email))return Fail("Önce e-posta doğrulanmalıdır.","uyeler");
        try {
            var token=await users.GeneratePasswordResetTokenAsync(member);
            var url=Url.Action("Reset","RiderAccount",new{email=member.Email,code=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))},env.IsDevelopment()?Request.Scheme:"https",env.IsDevelopment()?Request.Host.Value:"mamimotovlog.com")!;
            await mail.SendAsync(member.Email,"MAMI MOTOVLOG · Şifre sıfırlama","Yönetici hesabınız için şifre sıfırlama bağlantısı gönderdi. Şifrenizi yalnızca siz belirleyebilirsiniz.",url,"Şifrenizi Sıfırlamak İçin Lütfen Tıklayınız");
        }catch(Exception ex){logger.LogWarning(ex,"Yönetici sıfırlama maili gönderilemedi");return Fail("E-posta gönderilemedi.","uyeler");}
        return Done("Üyenin e-posta adresine sıfırlama bağlantısı gönderildi.","uyeler");
    }
    [HttpPost("uye/{id}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> MemberDelete(string id)
    {
        var member=await users.FindByIdAsync(id);if(member is null)return NotFound();
        await deletion.DeleteMemberAsync(member);return Done("Üye ve bağlı kayıtları silindi.","uyeler");
    }
    [HttpPost("motor/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> BikeDelete(int id)
    {
        var bike=await db.RiderBikes.FindAsync(id);if(bike is null)return NotFound();
        await deletion.DeleteBikeAsync(bike);return Done("Motosiklet ve bağlı kayıtlar silindi. Sürüş arşivi korundu.","motosikletler");
    }
    [HttpPost("rota/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> RouteDelete(int id)
    {
        var trip=await db.RiderTrips.FindAsync(id);if(trip is null)return NotFound();
        db.RiderTrips.Remove(trip);await db.SaveChangesAsync();return Done("Rota silindi.","rotalar");
    }
    [HttpPost("rota/{id:int}/yayini-kaldir")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        var trip=await db.RiderTrips.FindAsync(id);if(trip is null)return NotFound();
        trip.IsPublic=false;trip.IsApproved=false;await db.SaveChangesAsync();return Done("Rota topluluktan kaldırıldı.","rotalar");
    }
    [HttpPost("katalog/{kind}/{id:int}/duzenle")][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCatalog(string kind,int id,string? name,int year,string? edition,int? engineCc,decimal? powerHp,decimal? torqueNm,decimal? tankLitres)
    {
        name=(name??"").Trim();edition=(edition??"").Trim();
        if(!ModelState.IsValid)return Fail("Katalog alanlarını kontrol edin.");
        switch(kind){
            case "marka":
                var brand=await db.BikeBrands.FindAsync(id);if(brand is null)return NotFound();
                if(name.Length is <2 or >75 || await db.BikeBrands.AnyAsync(x=>x.Id!=id&&x.Name==name))return Fail("Marka adı geçersiz veya mevcut.");brand.Name=name;break;
            case "model":
                var model=await db.BikeModels.FindAsync(id);if(model is null)return NotFound();
                if(name.Length is <2 or >100||await db.BikeModels.AnyAsync(x=>x.Id!=id&&x.BrandId==model.BrandId&&x.Name==name))return Fail("Model adı geçersiz veya mevcut.");model.Name=name;break;
            case "versiyon":
                var v=await db.BikeVariants.FindAsync(id);if(v is null)return NotFound();
                if(year is <1950 or >2035||edition.Length is <2 or >85||engineCc is <1 or >2500||powerHp is <0 or >1000||torqueNm is <0 or >1000||tankLitres is <0.5m or >150m||await db.BikeVariants.AnyAsync(x=>x.Id!=id&&x.ModelId==v.ModelId&&x.Year==year&&x.Edition==edition))return Fail("Versiyon bilgileri geçersiz veya mevcut.");
                v.Year=year;v.Edition=edition;v.EngineCc=engineCc;v.PowerHp=powerHp;v.TorqueNm=torqueNm;v.TankLitres=tankLitres;break;
            default:return NotFound();
        }
        await db.SaveChangesAsync();return Done("Katalog kaydı güncellendi.");
    }
    [HttpPost("katalog/{kind}/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCatalog(string kind,int id)
    {
        switch(kind){
            case "marka":var brand=await db.BikeBrands.FindAsync(id);if(brand is null)return NotFound();if(await db.BikeModels.AnyAsync(x=>x.BrandId==id))return Fail("Önce markanın modellerini kaldırın.");db.BikeBrands.Remove(brand);break;
            case "model":var model=await db.BikeModels.FindAsync(id);if(model is null)return NotFound();if(await db.BikeVariants.AnyAsync(x=>x.ModelId==id))return Fail("Önce modelin versiyonlarını kaldırın.");db.BikeModels.Remove(model);break;
            case "versiyon":var v=await db.BikeVariants.FindAsync(id);if(v is null)return NotFound();if(await db.RiderBikes.AnyAsync(x=>x.VariantId==id))return Fail("Bu versiyonu kullanan motosikletler var. Kayıtları korumak için silinmedi.");db.BikeVariants.Remove(v);break;
            default:return NotFound();
        }
        await db.SaveChangesAsync();return Done("Katalog kaydı silindi.");
    }
    [HttpPost("yakit-fiyati")][ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFuel(string side,decimal price,string? source)
    {
        source=(source??"").Trim();
        if(!ModelState.IsValid||side is not ("anadolu" or "avrupa")||price is <1 or >1000||source.Length is <3 or >150)return Fail("Yaka, litre fiyatı ve doğrulama kaynağı geçerli olmalı.");
        var key="fuel:"+side;var entry=await db.PlatformEntries.FindAsync(key);
        if(entry is null){entry=new PlatformEntry{Key=key};db.PlatformEntries.Add(entry);}
        entry.Value=System.Text.Json.JsonSerializer.Serialize(new ManualFuelReference(price,source));entry.UpdatedUtc=DateTime.UtcNow;
        await db.SaveChangesAsync();return Done("Yönetici tarafından doğrulanan referans fiyat 24 saat kullanılabilir.");
    }
}
