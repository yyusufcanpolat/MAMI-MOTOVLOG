using MamiMotovlog.Models;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Data;

public static class RiderSeed
{
    // Model catalogue seeds are only name references. Manufacturer specifications remain empty until verified.
    // Model adları başlangıç kataloğudur: her ülkede ve model yılında satılma garantisi yoktur.
    // Yakıt bilgisi ayrıca doğrulanmış bir kaynağa dayanmadıkça boş bırakılır.
    private static readonly Dictionary<string, string[]> Catalog = new()
    {
        ["CFMOTO"] = ["250SR", "300SR", "450SR", "450NK", "450MT", "675SR-R", "700MT", "800MT"],
        ["Honda"] = ["CBR125R", "CBR250R", "CBR500R", "CBR650R", "CB650R", "CB500F", "CB500X", "CB750 Hornet", "CRF300L", "Africa Twin", "NC750X", "PCX125", "Forza 250", "ADV350"],
        ["Yamaha"] = ["R125", "R25", "R3", "R7", "R1", "MT-25", "MT-03", "MT-07", "MT-09", "MT-10", "Tracer 7", "Tracer 9", "Tenere 700", "XMAX 250", "NMAX 125"],
        ["Kawasaki"] = ["Ninja 250", "Ninja 400", "Ninja 500", "Ninja 650", "Ninja ZX-6R", "Ninja ZX-10R", "Z400", "Z500", "Z650", "Z900", "Versys 650"],
        ["BMW"] = ["G 310 R", "G 310 GS", "F 900 R", "F 900 GS", "S 1000 RR", "S 1000 R", "R 1250 GS", "R 1300 GS", "R nineT"],
        ["KTM"] = ["125 Duke", "250 Duke", "390 Duke", "RC 390", "790 Duke", "890 Adventure", "1290 Super Duke R"],
        ["Suzuki"] = ["GSX-R125", "GSX-R600", "GSX-R1000", "GSX-S750", "GSX-S1000", "V-Strom 250", "V-Strom 650", "V-Strom 800", "Hayabusa"],
        ["Ducati"] = ["Monster", "Panigale V2", "Panigale V4", "Supersport 950", "Multistrada V2", "Multistrada V4", "DesertX"],
        ["Bajaj"] = ["Pulsar NS125", "Pulsar NS200", "Pulsar RS200", "Pulsar N250", "Dominar 250", "Dominar 400"],
        ["Triumph"] = ["Speed 400", "Scrambler 400 X", "Street Triple 765", "Tiger 900", "Tiger 1200", "Bonneville T120"],
        ["Royal Enfield"] = ["Hunter 350", "Meteor 350", "Classic 350", "Himalayan 450", "Interceptor 650", "Continental GT 650", "Super Meteor 650"],
        ["Husqvarna"] = ["Svartpilen 250", "Svartpilen 401", "Vitpilen 401", "Norden 901"],
        ["Aprilia"] = ["RS 125", "RS 457", "RS 660", "Tuono 660", "Tuareg 660", "SR GT 200"],
        ["Benelli"] = ["TRK 251", "TRK 502", "TRK 702", "Leoncino 250", "Leoncino 500", "502C", "BN 125"],
        ["Harley-Davidson"] = ["Sportster S", "Nightster", "Iron 883", "Street Bob", "Fat Boy", "Pan America 1250"],
        ["TVS"] = ["Apache RTR 200 4V", "Apache RR 310", "Jupiter 125", "Ntorq 125"],
        ["Hero"] = ["Xpulse 200 4V", "Hunk 160R", "Splendor Plus"],
        ["Voge"] = ["300R", "500DS", "525DSX", "650DSX", "900DSX"],
        ["QJMotor"] = ["SRK 125 S", "SRK 400", "SRK 550", "SRT 700", "SRT 800"],
        ["Zontes"] = ["125U", "350R", "350T", "350X"],
        ["Kymco"] = ["Agility 125", "Downtown 250i", "AK 550", "Xciting 400"],
        ["SYM"] = ["Jet X 125", "Joyride 300", "Maxsym 400"],
        ["Vespa"] = ["Primavera 125", "Sprint 150", "GTS 300"],
        ["Piaggio"] = ["Liberty 125", "Medley 150", "Beverly 300", "MP3 400"],
        ["Moto Guzzi"] = ["V7 Stone", "V85 TT", "Stelvio"],
        ["MV Agusta"] = ["Brutale 800", "F3 800", "Turismo Veloce"],
        ["Indian"] = ["Scout", "Chief", "FTR", "Challenger"],
        ["Mondial"] = ["Drift L 125", "Wing 50", "X-Treme Max 200"],
        ["RKS"] = ["RZ 125", "Titanic 150", "R250"],
        ["Kuba"] = ["Bluebird 50", "Superlight 200", "RX9"],
        ["Peugeot"] = ["Django 125", "Pulsion 125"],
    };

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration cfg, ILogger log)
    {
        if (!cfg.GetValue<bool>("Rider:Enabled")) return;
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiderDbContext>();
        if (db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS PlatformEntries (Key TEXT NOT NULL PRIMARY KEY, Value TEXT NOT NULL, UpdatedUtc TEXT NOT NULL)");
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS AdminAudits (Id INTEGER PRIMARY KEY AUTOINCREMENT, Operation TEXT NOT NULL, CreatedUtc TEXT NOT NULL)");
            if (await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM RiderFuels GROUP BY BikeId, AtKm HAVING COUNT(*) > 1) THEN 1 ELSE 0 END AS Value").SingleAsync() == 0)
                await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_RiderFuels_BikeId_AtKm ON RiderFuels(BikeId, AtKm)");
            else log.LogWarning("Eski yakıt kayıtlarında aynı kilometrede birden fazla dolum var; benzersiz indeks eklenmedi. Kayıtlar korunuyor.");
        }
        else if (db.Database.IsSqlServer())
        {
            // Never modify an existing live database automatically. Run V3_INSTALL_SQLSERVER.sql first.
            if (await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.RiderBikes',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.AspNetUsers',N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]").SingleAsync() != 1)
                throw new InvalidOperationException("V3 SQL tabloları eksik. Database/V3_INSTALL_SQLSERVER.sql dosyasını uygulayın veya Rider:Enabled=false yapın.");
        }
        if (db.Database.IsSqlServer() && await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.PlatformEntries',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.AdminAudits',N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]").SingleAsync() != 1)
            throw new InvalidOperationException("V5.2 ek tablolar eksik. Database/V52_UPDATE_SQLSERVER.sql dosyasını uygulayın.");
        var seeded = await db.PlatformEntries.FindAsync("catalog:seeded");
        if (seeded is not null) return;
        var existing = await db.BikeBrands.AnyAsync();
        if (!existing)
        {
            foreach (var (brandName, models) in Catalog)
            {
                var brand = new BikeBrand { Name = brandName };
                foreach (var modelName in models) brand.Models.Add(new BikeModel { Name = modelName });
                db.BikeBrands.Add(brand);
            }
            await db.SaveChangesAsync();
            log.LogInformation("V3 motosiklet kataloğu için {Count} marka kaydedildi.", Catalog.Count);
        }
        // Verified 2024 CFMOTO 450SR owner's manual; do not fabricate the other variants.
        var verifiedModel = await db.BikeModels.Include(m => m.Brand)
            .FirstOrDefaultAsync(m => m.Name == "450SR" && m.Brand.Name == "CFMOTO");
        if (!existing && verifiedModel is not null &&
            !await db.BikeVariants.AnyAsync(v => v.ModelId == verifiedModel.Id && v.Year == 2024 && v.Edition == "Standart"))
        {
            db.BikeVariants.Add(new BikeVariant { ModelId = verifiedModel.Id, Year = 2024,
                Edition = "Standart", EngineCc = 449, TankLitres = 14m });
            await db.SaveChangesAsync();
        }
        db.PlatformEntries.Add(new PlatformEntry { Key="catalog:seeded", Value="1" });
        await db.SaveChangesAsync();
    }
}
