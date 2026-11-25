using System.Security.Claims;
using MamiMotovlog.Data;
using MamiMotovlog.Models;
using MamiMotovlog.ViewModels;
using MamiMotovlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

[Authorize(AuthenticationSchemes = "Identity.Application")]
public class RiderController(RiderDbContext db, RiderFileStore files, RiderDeletion deletion, ILogger<RiderController> logger) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    [HttpGet("/garaj")]
    public async Task<IActionResult> Dashboard()
    {
        var bikes = await db.RiderBikes.AsNoTracking().Where(x => x.OwnerId == UserId).OrderByDescending(x => x.CreatedUtc).ToListAsync();
        var ids = bikes.Select(x => x.Id).ToList();
        var fuels = await db.RiderFuels.AsNoTracking().Where(x => ids.Contains(x.BikeId)).ToListAsync();
        var maint = await db.RiderMaintenances.AsNoTracking().Where(x => ids.Contains(x.BikeId)).ToListAsync();
        var other = await db.RiderExpenses.AsNoTracking().Where(x => ids.Contains(x.BikeId)).ToListAsync();
        var allTrips = await db.RiderTrips.AsNoTracking().Where(x => x.OwnerId == UserId).ToListAsync();
        var trips = allTrips.OrderByDescending(x=>x.CreatedUtc).Take(8).ToList();
        var reminders = await db.RiderReminders.AsNoTracking().Where(x => x.OwnerId == UserId && x.DueOn <= DateTime.UtcNow.AddDays(45)).OrderBy(x => x.DueOn).ToListAsync();
        var maintenanceReminders = maint.GroupBy(x => new { x.BikeId, x.Category }).Select(x => x.OrderByDescending(y => y.ServicedOn).ThenByDescending(y => y.Id).First()).Where(x => (x.NextDate.HasValue && x.NextDate <= MamiMotovlog.Services.TurkeyClock.Today.AddDays(30)) ||
                                                    (x.NextKm.HasValue && bikes.Any(b => b.Id == x.BikeId && b.OdometerKm + 500 >= x.NextKm.Value)))
            ;
        foreach (var item in maintenanceReminders)
            reminders.Add(new RiderReminder { Title = $"{bikes.First(x => x.Id == item.BikeId).BrandName} {bikes.First(x => x.Id == item.BikeId).ModelName} · {item.Category}",
                DueOn = item.NextDate ?? MamiMotovlog.Services.TurkeyClock.Today, Kind = "Bakım" });
        var name = User.FindFirstValue("rider_name") ?? User.Identity?.Name ?? "Sürücü";
        return View(new RiderDashboardVm {
            DisplayName = name, Bikes = bikes, MeasuredFuel = bikes.ToDictionary(b => b.Id, b => FuelConsumptionCalculator.Calculate(fuels.Where(f => f.BikeId == b.Id)).LitresPer100Km), Trips = trips, CompletedTripCount = allTrips.Count(x=>x.CompletedOn.HasValue), Reminders = reminders.OrderBy(x=>x.DueOn).Take(10).ToList(),
            FuelSpent = fuels.Sum(x => x.TotalTry), MaintenanceSpent = maint.Sum(x => x.CostTry), OtherSpent = other.Sum(x => x.AmountTry),
            TotalKm = allTrips.Where(x => x.CompletedOn.HasValue).Sum(x => x.DistanceKm)
        });
    }

    [HttpGet("/garaj/yil-ozeti")]
    public async Task<IActionResult> YearSummary(int? year)
    {
        var selected = year ?? MamiMotovlog.Services.TurkeyClock.Today.Year;
        if (selected is < 2000 or > 2100) return BadRequest();
        var bikes = await db.RiderBikes.AsNoTracking().Where(x=>x.OwnerId==UserId).Select(x=>x.Id).ToListAsync();
        var trips = await db.RiderTrips.AsNoTracking().Where(x=>x.OwnerId==UserId && x.CompletedOn.HasValue && x.CompletedOn.Value.Year==selected).ToListAsync();
        var fuels = await db.RiderFuels.AsNoTracking().Where(x=>bikes.Contains(x.BikeId)&&x.FuelledOn.Year==selected).ToListAsync();
        var maint = await db.RiderMaintenances.AsNoTracking().Where(x=>bikes.Contains(x.BikeId)&&x.ServicedOn.Year==selected).ToListAsync();
        var expenses = await db.RiderExpenses.AsNoTracking().Where(x=>bikes.Contains(x.BikeId)&&x.SpentOn.Year==selected).ToListAsync();
        var monthly = new decimal[12];
        foreach(var t in trips) monthly[t.CompletedOn!.Value.Month-1]+=t.DistanceKm;
        return View(new RiderYearVm { Year=selected, CompletedTrips=trips.Count, CompletedDistanceKm=trips.Sum(x=>x.DistanceKm),
            FuelTry=fuels.Sum(x=>x.TotalTry), MaintenanceTry=maint.Sum(x=>x.CostTry), ExpensesTry=expenses.Sum(x=>x.AmountTry),
            MonthlyDistancesKm=monthly });
    }

    [AllowAnonymous]
    [HttpGet("/garaj/katalog/markalar")]
    public async Task<IActionResult> Brands() => Json(await db.BikeBrands.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync());

    [AllowAnonymous]
    [HttpGet("/garaj/katalog/modeller/{brandId:int}")]
    public async Task<IActionResult> Models(int brandId) => Json(await db.BikeModels.AsNoTracking().Where(x => x.BrandId == brandId)
        .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync());

    [AllowAnonymous]
    [HttpGet("/garaj/katalog/versiyonlar/{modelId:int}")]
    public async Task<IActionResult> Variants(int modelId) => Json(await db.BikeVariants.AsNoTracking().Where(x => x.ModelId == modelId)
        .OrderByDescending(x => x.Year).Select(x => new { x.Id, x.Year, x.Edition, x.EngineCc, x.PowerHp, x.TorqueNm, x.TankLitres }).ToListAsync());

    [HttpGet("/garaj/motor/ekle")]
    public IActionResult AddBike() => View(new RiderBikeVm());

    [HttpPost("/garaj/motor/ekle")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBike(RiderBikeVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var bike = new RiderBike {
            OwnerId = UserId, BrandName = vm.BrandName.Trim(), ModelName = vm.ModelName.Trim(), ModelYear = vm.ModelYear,
            Edition = (vm.Edition ?? "").Trim(), OdometerKm = vm.OdometerKm, AverageFuelL100 = vm.AverageFuelL100,
            TankLitres = vm.TankLitres, Notes = (vm.Notes ?? "").Trim()
        };
        if (vm.VariantId.HasValue)
        {
            var variant = await db.BikeVariants.Include(x => x.Model).ThenInclude(x => x.Brand).FirstOrDefaultAsync(x => x.Id == vm.VariantId);
            if (variant is null) { ModelState.AddModelError("", "Seçilen versiyon geçersiz."); return View(vm); }
            bike.VariantId = variant.Id; bike.BrandName = variant.Model.Brand.Name;
            bike.ModelName = variant.Model.Name; bike.ModelYear = variant.Year; bike.Edition = variant.Edition;
            bike.TankLitres ??= variant.TankLitres;
        }
        db.RiderBikes.Add(bike);
        await db.SaveChangesAsync();
        TempData["RiderInfo"] = "Motosiklet garajına eklendi.";
        return RedirectToAction(nameof(Bike), new { id = bike.Id });
    }

    [HttpGet("/garaj/motor/{id:int}")]
    public async Task<IActionResult> Bike(int id)
    {
        var bike = await db.RiderBikes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        var fuels = await db.RiderFuels.AsNoTracking().Where(x => x.BikeId == id).OrderByDescending(x=>x.AtKm).ToListAsync();
        var maint = await db.RiderMaintenances.AsNoTracking().Where(x => x.BikeId == id).OrderByDescending(x=>x.ServicedOn).ToListAsync();
        var exp = await db.RiderExpenses.AsNoTracking().Where(x => x.BikeId == id).OrderByDescending(x=>x.SpentOn).ToListAsync();
        var fuelResult = FuelConsumptionCalculator.Calculate(fuels);
        var measured = fuelResult.LitresPer100Km.HasValue;
        // An owner-provided value is shown explicitly as an estimate; never as measured consumption.
        var shownConsumption = fuelResult.LitresPer100Km ?? bike.AverageFuelL100;
        return View(new RiderBikeDetailsVm {
            Bike = bike, Fuels = fuels, Maintenances = maint, Expenses = exp,
            ConsumptionL100 = shownConsumption, ConsumptionMeasured = measured,
            ConsumptionIntervalCount = fuelResult.Intervals
        });
    }

    [HttpGet("/garaj/motor/{id:int}/bakim-raporu")]
    public async Task<IActionResult> MaintenanceReport(int id)
    {
        var bike = await db.RiderBikes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        ViewBag.Bike = bike;
        return View(await db.RiderMaintenances.AsNoTracking().Where(x => x.BikeId == id).OrderBy(x => x.ServicedOn).ToListAsync());
    }

    [HttpGet("/garaj/motor/{id:int}/duzenle")]
    public async Task<IActionResult> EditBike(int id)
    {
        var bike = await db.RiderBikes.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        ViewData["BikeId"] = id;
        return View(new RiderBikeVm { VariantId = bike.VariantId, BrandName = bike.BrandName, ModelName = bike.ModelName,
            ModelYear = bike.ModelYear, Edition = bike.Edition, OdometerKm = bike.OdometerKm,
            AverageFuelL100 = bike.AverageFuelL100, TankLitres = bike.TankLitres, Notes = bike.Notes });
    }

    [HttpPost("/garaj/motor/{id:int}/duzenle")][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBike(int id, RiderBikeVm vm)
    {
        var bike = await db.RiderBikes.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        if (!ModelState.IsValid) { ViewData["BikeId"] = id; return View(vm); }
        if (vm.OdometerKm < bike.OdometerKm) ModelState.AddModelError(nameof(vm.OdometerKm), "Kilometre geçmiş kayıttan küçük olamaz.");
        if (!ModelState.IsValid) { ViewData["BikeId"] = id; return View(vm); }
        if(bike.BrandName!=vm.BrandName.Trim() || bike.ModelName!=vm.ModelName.Trim() || bike.ModelYear!=vm.ModelYear || bike.Edition!=(vm.Edition??"").Trim()) bike.VariantId=null;
        bike.BrandName = vm.BrandName.Trim(); bike.ModelName = vm.ModelName.Trim(); bike.ModelYear = vm.ModelYear;
        bike.Edition = (vm.Edition ?? "").Trim(); bike.Notes = (vm.Notes ?? "").Trim(); bike.TankLitres = vm.TankLitres;
        bike.AverageFuelL100 = vm.AverageFuelL100;
        bike.OdometerKm = vm.OdometerKm;
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ViewData["BikeId"] = id; ModelState.AddModelError("", "Kilometre başka bir işlemde değişti. Güncel kaydı açıp tekrar deneyin."); return View(vm); }
        return RedirectToAction(nameof(Bike), new { id });
    }

    [HttpPost("/garaj/motor/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBike(int id)
    {
        var bike = await db.RiderBikes.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        await deletion.DeleteBikeAsync(bike);
        TempData["RiderInfo"] = "Motosiklet ve bağlı kayıtlar silindi.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet("/garaj/motor/{id:int}/yakit")]
    public async Task<IActionResult> AddFuel(int id)
    {
        var bike = await db.RiderBikes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        ViewData["BikeName"] = bike.BrandName + " " + bike.ModelName;
        return View(new RiderFuelVm { BikeId = id, OdometerKm = bike.OdometerKm, FullTank = true });
    }

    [HttpPost("/garaj/yakit")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFuel(RiderFuelVm vm)
    {
        var bike = await db.RiderBikes.FirstOrDefaultAsync(x => x.Id == vm.BikeId && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        if (vm.Date.Date > MamiMotovlog.Services.TurkeyClock.Today || vm.Date.Year < 1990) ModelState.AddModelError(nameof(vm.Date), "Geçerli bir tarih gir.");
        if (vm.OdometerKm < bike.OdometerKm) ModelState.AddModelError(nameof(vm.OdometerKm), "Kilometre mevcut değerden düşük olamaz.");
        if (await db.RiderFuels.AnyAsync(x => x.BikeId == bike.Id && x.AtKm == vm.OdometerKm))
            ModelState.AddModelError(nameof(vm.OdometerKm), "Bu kilometrede yakıt kaydı zaten var. Çift kayıt yerine mevcut kaydı kontrol et.");
        if (!ModelState.IsValid) { ViewData["BikeName"] = bike.BrandName + " " + bike.ModelName; return View("AddFuel", vm); }
        db.RiderFuels.Add(new RiderFuel { BikeId = bike.Id, FuelledOn = vm.Date.Date, AtKm = vm.OdometerKm,
            Litres = vm.Litres, TotalTry = vm.AmountTry, FullTank = vm.FullTank });
        bike.OdometerKm = Math.Max(bike.OdometerKm, vm.OdometerKm);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Yakıt kaydı çakışması veya veri hatası"); ModelState.AddModelError("", "Kayıt eklenemedi. Kilometreyi ve mevcut yakıt kayıtlarını kontrol edip yeniden deneyin."); ViewData["BikeName"] = bike.BrandName + " " + bike.ModelName; return View("AddFuel", vm); }
        return RedirectToAction(nameof(Bike), new { id = bike.Id });
    }

    [HttpPost("/garaj/yakit/{id:int}/dolum")][ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFuelFill(int id, bool fullTank)
    {
        var fuel = await db.RiderFuels.FirstOrDefaultAsync(f => f.Id == id && f.Bike.OwnerId == UserId);
        if (fuel is null) return NotFound();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Bike), new { id = fuel.BikeId });
        fuel.FullTank = fullTank;
        await db.SaveChangesAsync();
        TempData["RiderInfo"] = "Dolum türü ve ölçülen tüketim güncellendi.";
        return RedirectToAction(nameof(Bike), new { id = fuel.BikeId });
    }

    [HttpGet("/garaj/motor/{id:int}/bakim")]
    public async Task<IActionResult> AddMaintenance(int id)
    {
        var bike = await db.RiderBikes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        return View(new RiderMaintenanceVm { BikeId = id, OdometerKm = bike.OdometerKm });
    }

    [HttpPost("/garaj/bakim")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaintenance(RiderMaintenanceVm vm)
    {
        var bike = await db.RiderBikes.FirstOrDefaultAsync(x => x.Id == vm.BikeId && x.OwnerId == UserId);
        if (bike is null) return NotFound();
        if (vm.Date.Date > MamiMotovlog.Services.TurkeyClock.Today || vm.Date.Year < 1990) ModelState.AddModelError(nameof(vm.Date), "Geçerli bir tarih gir.");
        if (vm.NextDate.HasValue && vm.NextDate.Value.Date <= vm.Date.Date) ModelState.AddModelError(nameof(vm.NextDate), "Sonraki bakım tarihi daha ileri olmalı.");
        if (vm.NextKm.HasValue && vm.NextKm.Value <= vm.OdometerKm) ModelState.AddModelError(nameof(vm.NextKm), "Sonraki bakım kilometresi daha yüksek olmalı.");
        if (!ModelState.IsValid) return View(vm);
        db.RiderMaintenances.Add(new RiderMaintenance { BikeId = bike.Id, Category = vm.Category.Trim(), Description = (vm.Description ?? "").Trim(),
            ServicedOn = vm.Date.Date, AtKm = vm.OdometerKm, CostTry = vm.CostTry ?? 0m, NextDate = vm.NextDate?.Date, NextKm = vm.NextKm });
        bike.OdometerKm = Math.Max(bike.OdometerKm, vm.OdometerKm);
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Motosiklet kilometresi başka bir işlemde güncellendi. Kaydı kontrol edip tekrar deneyin."); return View(vm); }
        return RedirectToAction(nameof(Bike), new { id = bike.Id });
    }

    [HttpGet("/garaj/motor/{id:int}/gider")]
    public async Task<IActionResult> AddExpense(int id) => await OwnedBike(id) ? View(new RiderExpenseVm { BikeId = id }) : NotFound();

    [HttpPost("/garaj/gider")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(RiderExpenseVm vm)
    {
        if (!await OwnedBike(vm.BikeId)) return NotFound();
        if (vm.Date.Date > MamiMotovlog.Services.TurkeyClock.Today || vm.Date.Year < 1990) ModelState.AddModelError(nameof(vm.Date), "Geçerli bir tarih gir.");
        if (!ModelState.IsValid) return View(vm);
        db.RiderExpenses.Add(new RiderExpense { BikeId = vm.BikeId, SpentOn = vm.Date.Date,
            Category = vm.Category.Trim(), Description = (vm.Description ?? "").Trim(), AmountTry = vm.AmountTry });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Bike), new { id = vm.BikeId });
    }

    [HttpPost("/garaj/kayit-sil/{type}/{id:int}")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecord(string type, int id)
    {
        int bikeId;
        string? removedFile = null;
        switch (type)
        {
            case "yakit":
                var fuel = await db.RiderFuels.FirstOrDefaultAsync(x => x.Id == id && x.Bike.OwnerId == UserId);
                if (fuel is null) return NotFound();
                bikeId = fuel.BikeId; db.RiderFuels.Remove(fuel); break;
            case "bakim":
                var maint = await db.RiderMaintenances.FirstOrDefaultAsync(x => x.Id == id && x.Bike.OwnerId == UserId);
                if (maint is null) return NotFound();
                bikeId = maint.BikeId; removedFile=maint.AttachmentFileName;
                var prefix=$"maintenance:{maint.Id}:";
                db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key.StartsWith(prefix)).ToListAsync());
                db.RiderMaintenances.Remove(maint); break;
            case "gider":
                var expense = await db.RiderExpenses.FirstOrDefaultAsync(x => x.Id == id && x.Bike.OwnerId == UserId);
                if (expense is null) return NotFound();
                bikeId = expense.BikeId; db.RiderExpenses.Remove(expense); break;
            default: return NotFound();
        }
        await db.SaveChangesAsync();
        files.Delete(removedFile);
        TempData["RiderInfo"]="Kayıt silindi.";
        return RedirectToAction(nameof(Bike), new { id = bikeId });
    }

    [HttpGet("/garaj/ekipmanlar")]
    public async Task<IActionResult> Equipment() => View(await db.RiderEquipments.AsNoTracking().Where(x => x.OwnerId == UserId).OrderBy(x => x.Category).ToListAsync());

    [HttpGet("/garaj/ekipman-ekle")]
    public IActionResult AddEquipment() => View(new RiderGearVm());

    [HttpPost("/garaj/ekipman-ekle")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEquipment(RiderGearVm vm)
    {
        if (vm.PurchasedOn.HasValue && vm.PurchasedOn.Value.Date > MamiMotovlog.Services.TurkeyClock.Today) ModelState.AddModelError(nameof(vm.PurchasedOn), "Alış tarihi gelecekte olamaz.");
        if (vm.WarrantyEndsOn < vm.PurchasedOn) ModelState.AddModelError(nameof(vm.WarrantyEndsOn), "Garanti bitişi alış tarihinden önce olamaz.");
        if (!ModelState.IsValid) return View(vm);
        db.RiderEquipments.Add(new RiderEquipment { OwnerId = UserId, Category = vm.Category.Trim(), Brand = vm.Brand.Trim(),
            Model = vm.Model.Trim(), CostTry = vm.CostTry, PurchasedOn = vm.PurchasedOn?.Date, WarrantyEndsOn = vm.WarrantyEndsOn?.Date });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Equipment));
    }

    [HttpPost("/garaj/ekipman-sil/{id:int}")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEquipment(int id)
    {
        var item = await db.RiderEquipments.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (item is null) return NotFound();
        db.RiderEquipments.Remove(item); await db.SaveChangesAsync(); files.Delete(item.PhotoFileName);
        return RedirectToAction(nameof(Equipment));
    }

    [HttpPost("/garaj/ekipmanlar/{id:int}/fotograf")][ValidateAntiForgeryToken][RequestSizeLimit(6_500_000)]
    public async Task<IActionResult> UploadGearPhoto(int id, IFormFile image, CancellationToken ct)
    {
        var gear = await db.RiderEquipments.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId, ct);
        if (gear is null) return NotFound();
        string filename;
        try { filename = await files.SaveAsync(image, allowPdf: false, ct); }
        catch (ArgumentException ex) { TempData["RiderInfo"] = ex.Message; return RedirectToAction(nameof(Equipment)); }
        catch (IOException ex) { logger.LogError(ex, "Private upload directory is not writable"); return StatusCode(503,"Dosya alanı yazılabilir değil; hosting izinleri kontrol edilmeli."); }
        var old = gear.PhotoFileName;
        gear.PhotoFileName = filename;
        try { await db.SaveChangesAsync(ct); } catch { files.Delete(filename); throw; }
        files.Delete(old);
        return RedirectToAction(nameof(Equipment));
    }

    [HttpGet("/garaj/ekipmanlar/{id:int}/fotograf")]
    public async Task<IActionResult> GearPhoto(int id)
    {
        var gear = await db.RiderEquipments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (gear is null || string.IsNullOrWhiteSpace(gear.PhotoFileName)) return NotFound();
        try { return File(files.Open(gear.PhotoFileName), RiderFileStore.Mime(gear.PhotoFileName)); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpPost("/garaj/bakim/{id:int}/belge")][ValidateAntiForgeryToken][RequestSizeLimit(6_500_000)]
    public async Task<IActionResult> UploadMaintenanceDocument(int id, IFormFile document, CancellationToken ct)
    {
        var record = await db.RiderMaintenances.Include(x => x.Bike).FirstOrDefaultAsync(x => x.Id == id && x.Bike.OwnerId == UserId, ct);
        if (record is null) return NotFound();
        string filename;
        try { filename = await files.SaveAsync(document, allowPdf: true, ct); }
        catch (ArgumentException ex) { TempData["RiderInfo"] = ex.Message; return RedirectToAction(nameof(Bike), new { id = record.BikeId }); }
        catch (IOException ex) { logger.LogError(ex, "Private upload directory is not writable"); return StatusCode(503,"Dosya alanı yazılabilir değil; hosting izinleri kontrol edilmeli."); }
        var old = record.AttachmentFileName; record.AttachmentFileName = filename;
        try { await db.SaveChangesAsync(ct); } catch { files.Delete(filename); throw; }
        files.Delete(old);
        return RedirectToAction(nameof(Bike), new { id = record.BikeId });
    }

    [HttpGet("/garaj/bakim/{id:int}/belge")]
    public async Task<IActionResult> MaintenanceDocument(int id)
    {
        var item = await db.RiderMaintenances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Bike.OwnerId == UserId);
        if (item is null || string.IsNullOrWhiteSpace(item.AttachmentFileName)) return NotFound();
        try { return File(files.Open(item.AttachmentFileName), RiderFileStore.Mime(item.AttachmentFileName), "bakim-belgesi" + Path.GetExtension(item.AttachmentFileName)); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpGet("/garaj/hatirlaticilar")]
    public async Task<IActionResult> Reminders() => View(await db.RiderReminders.AsNoTracking().Where(x => x.OwnerId == UserId).OrderBy(x=>x.DueOn).ToListAsync());

    [HttpPost("/garaj/hatirlaticilar")][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReminder(int bikeId, string kind, string title, DateTime date)
    {
        if (!await OwnedBike(bikeId)) { TempData["RiderInfo"] = "Garajından geçerli bir motosiklet seç."; return RedirectToAction(nameof(Reminders)); }
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(title) || title.Length > 180 || string.IsNullOrWhiteSpace(kind) || kind.Length > 65 || date.Year < 2000 || date.Year > 2100)
            { TempData["RiderInfo"] = "Hatırlatma bilgilerini kontrol edin."; return RedirectToAction(nameof(Reminders)); }
        db.RiderReminders.Add(new RiderReminder { OwnerId = UserId, BikeId = bikeId, Kind = kind, Title = title.Trim(), DueOn = date.Date });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Reminders));
    }

    [HttpPost("/garaj/hatirlaticilar/{id:int}/sil")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReminder(int id)
    {
        var item = await db.RiderReminders.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (item is null) return NotFound();
        var key=$"reminder:{item.Id}:sent";
        db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key==key).ToListAsync());
        db.RiderReminders.Remove(item); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Reminders));
    }

    private Task<bool> OwnedBike(int id) => db.RiderBikes.AsNoTracking().AnyAsync(x => x.Id == id && x.OwnerId == UserId);
}
