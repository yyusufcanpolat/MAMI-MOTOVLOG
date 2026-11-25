using MamiMotovlog.Models;
using MamiMotovlog.Services;

static void Check(bool ok, string title)
{
    if (!ok) throw new Exception("TEST FAIL: " + title);
    Console.WriteLine("PASS " + title);
}
static RiderFuel Fill(int km, decimal litre, bool full, int id) =>
    new() { Id = id, AtKm = km, Litres = litre, FullTank = full, FuelledOn = new DateTime(2026, 11, 1) };

Check(FuelConsumptionCalculator.Calculate(Array.Empty<RiderFuel>()).LitresPer100Km is null, "Boş veri -> ölçüm yok");
Check(FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 10, true, 1) }).LitresPer100Km is null, "Tek tam depo -> ölçüm yok");
Check(FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 10, true, 1), Fill(1200, 8, true, 2) }).LitresPer100Km == 4m,
      "İki tam depo (8 L / 200 km) -> 4 L/100");
Check(FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 10, true, 1), Fill(1100, 3, false, 2), Fill(1200, 7, true, 3) }).LitresPer100Km == 5m,
      "Ara kısmi dolum dahil -> 5 L/100");
var weighted = FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 11, true, 1), Fill(1200, 10, true, 2), Fill(1500, 9, true, 3) });
Check(weighted.LitresPer100Km == 3.8m && weighted.Intervals == 2 && weighted.DistanceKm == 500,
      "Birden fazla aralık ağırlıklı (19 L / 500 km) -> 3,8 L/100");
Check(FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 10, true, 1), Fill(1000, 5, true, 2) }).LitresPer100Km is null,
      "Aynı kilometreye iki kayıt -> sıfıra bölme yok");
Check(FuelConsumptionCalculator.Calculate(new[] { Fill(1000, 10, false, 1), Fill(1200, 8, true, 2) }).LitresPer100Km is null,
      "Başlangıç tam dolum değil -> ölçüm yok");
Console.WriteLine("Fuel calculator tests completed.");
