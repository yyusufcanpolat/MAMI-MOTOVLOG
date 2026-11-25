using MamiMotovlog.Data;
using MamiMotovlog.Models;
using Microsoft.EntityFrameworkCore;
namespace MamiMotovlog.Services;

// A DB-backed lease avoids concurrent workers sending the same notification.
// SMTP is not transactional: a crash after SMTP accepts mail but before marking it sent can still retry.
public static class ReminderClaim
{
    public static async Task<string?> AcquireAsync(RiderDbContext db, string key, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var owner = Guid.NewGuid().ToString("N");
        var existing = await db.PlatformEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);
        if (existing is not null)
        {
            if (existing.Value == "sent" || existing.UpdatedUtc > now.AddHours(-1)) return null;
            var changed = await db.PlatformEntries.Where(x => x.Key == key && x.Value != "sent" && x.UpdatedUtc <= now.AddHours(-1))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Value, owner).SetProperty(x => x.UpdatedUtc, now), ct);
            return changed == 1 ? owner : null;
        }
        var entry = new PlatformEntry { Key = key, Value = owner, UpdatedUtc = now };
        db.PlatformEntries.Add(entry);
        try { await db.SaveChangesAsync(ct); return owner; }
        catch (DbUpdateException)
        {
            db.Entry(entry).State = EntityState.Detached;
            if (await db.PlatformEntries.AsNoTracking().AnyAsync(x => x.Key == key, ct)) return null;
            throw;
        }
        finally { db.Entry(entry).State = EntityState.Detached; }
    }
    public static async Task CompleteAsync(RiderDbContext db, string key, string owner, CancellationToken ct)
    {
        await db.PlatformEntries.Where(x => x.Key == key && x.Value == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Value, "sent").SetProperty(x => x.UpdatedUtc, DateTime.UtcNow), ct);
    }
    public static async Task ReleaseAsync(RiderDbContext db, string key, string owner, CancellationToken ct)
    {
        await db.PlatformEntries.Where(x => x.Key == key && x.Value == owner).ExecuteDeleteAsync(ct);
    }
}
