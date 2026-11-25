using MamiMotovlog.Models;

namespace MamiMotovlog.Services;

/// <summary>
/// Full-to-full consumption: sum litres bought AFTER a full-tank baseline and
/// THROUGH the next full fill, divided by odometer distance. Partial fills are
/// included. Each closed interval is weighted by actual distance, not averaged
/// arithmetically. A single fill does not prove real consumption.
/// </summary>
public static class FuelConsumptionCalculator
{
    public readonly record struct Result(decimal? LitresPer100Km, int Intervals, int DistanceKm);

    public static Result Calculate(IEnumerable<RiderFuel> records)
    {
        var ordered = records.OrderBy(x => x.AtKm).ThenBy(x => x.FuelledOn).ThenBy(x => x.Id).ToList();
        int? baselineKm = null;
        int previousKm = -1;
        decimal pendingLitres = 0;
        decimal validLitres = 0;
        int validKm = 0;
        int intervals = 0;

        foreach (var fill in ordered)
        {
            if (fill.AtKm < 0 || fill.Litres <= 0 || fill.AtKm < previousKm)
                continue;
            previousKm = fill.AtKm;
            if (!baselineKm.HasValue)
            {
                if (fill.FullTank) baselineKm = fill.AtKm;
                continue;
            }
            if (fill.AtKm <= baselineKm.Value) // duplicate or invalid reading: do not divide by zero
                continue;
            pendingLitres += fill.Litres;
            if (!fill.FullTank) continue;

            var segmentKm = fill.AtKm - baselineKm.Value;
            if (segmentKm > 0 && pendingLitres > 0)
            {
                validLitres += pendingLitres;
                validKm += segmentKm;
                intervals++;
            }
            baselineKm = fill.AtKm;
            pendingLitres = 0;
        }
        return validKm > 0
            ? new Result(Math.Round(validLitres * 100m / validKm, 2), intervals, validKm)
            : new Result(null, 0, 0);
    }
}
