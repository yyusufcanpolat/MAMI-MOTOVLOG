using MamiMotovlog.Data;
using MamiMotovlog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace MamiMotovlog.Services;

public sealed class RiderDeletion(RiderDbContext db, UserManager<RiderMember> users, RiderFileStore files, ILogger<RiderDeletion> logger)
{
    private void Clean(IEnumerable<string> names)
    {
        foreach (var name in names) try { files.Delete(name); } catch (IOException ex) { logger.LogWarning(ex,"Silinen kaydın dosyası kaldırılamadı"); } catch (UnauthorizedAccessException ex) { logger.LogWarning(ex,"Dosya silme izni yok"); }
    }
    public async Task DeleteBikeAsync(RiderBike bike)
    {
        var docs = await db.RiderMaintenances.Where(x=>x.BikeId==bike.Id).Select(x=>x.AttachmentFileName).ToListAsync();
        var maintIds = await db.RiderMaintenances.Where(x=>x.BikeId==bike.Id).Select(x=>x.Id).ToListAsync();
        foreach(var mid in maintIds)
        {
            var prefix = $"maintenance:{mid}:";
            db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key.StartsWith(prefix)).ToListAsync());
        }
        var trips = await db.RiderTrips.Where(x=>x.BikeId==bike.Id).ToListAsync();
        foreach(var t in trips) t.BikeId=null;
        var reminders = await db.RiderReminders.Where(x=>x.BikeId==bike.Id).ToListAsync();
        foreach(var r in reminders) { var key=$"reminder:{r.Id}:sent"; db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key==key).ToListAsync()); }
        db.RiderReminders.RemoveRange(reminders);
        db.RiderBikes.Remove(bike);
        await db.SaveChangesAsync();
        Clean(docs);
    }
    public async Task DeleteMemberAsync(RiderMember member)
    {
        var docs = new List<string>();
        var id=member.Id;
        // SQL retry strategy must own explicit transaction. No automatic retry of SMTP/files.
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            docs.Clear();
            await using var tx=await db.Database.BeginTransactionAsync();
            var current=await users.FindByIdAsync(id);
            if(current is null) { await tx.CommitAsync(); return; }
            var bikes=await db.RiderBikes.Where(x=>x.OwnerId==id).ToListAsync();
            var bikeIds=bikes.Select(x=>x.Id).ToList();
            var maintenance=await db.RiderMaintenances.Where(x=>bikeIds.Contains(x.BikeId)).ToListAsync();
            var gear=await db.RiderEquipments.Where(x=>x.OwnerId==id).ToListAsync();
            docs.AddRange(maintenance.Select(x=>x.AttachmentFileName)); docs.AddRange(gear.Select(x=>x.PhotoFileName));
            foreach(var m in maintenance) { var prefix=$"maintenance:{m.Id}:"; db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key.StartsWith(prefix)).ToListAsync()); }
            db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key=="mail-off:"+id).ToListAsync());
            db.RiderFavorites.RemoveRange(await db.RiderFavorites.Where(x=>x.OwnerId==id).ToListAsync());
            db.RiderTrips.RemoveRange(await db.RiderTrips.Where(x=>x.OwnerId==id).ToListAsync());
            var reminders = await db.RiderReminders.Where(x=>x.OwnerId==id).ToListAsync();
            foreach(var r in reminders) { var key=$"reminder:{r.Id}:sent"; db.PlatformEntries.RemoveRange(await db.PlatformEntries.Where(x=>x.Key==key).ToListAsync()); }
            db.RiderReminders.RemoveRange(reminders);
            db.RiderEquipments.RemoveRange(gear);
            db.RiderBikes.RemoveRange(bikes);
            await db.SaveChangesAsync();
            var result=await users.DeleteAsync(current);
            if(!result.Succeeded) throw new InvalidOperationException("Üyelik silinemedi.");
            await tx.CommitAsync();
        });
        Clean(docs);
    }
}
