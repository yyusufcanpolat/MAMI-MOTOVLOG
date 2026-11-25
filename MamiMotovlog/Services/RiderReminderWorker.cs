using MamiMotovlog.Data;
using MamiMotovlog.Models;
using Microsoft.EntityFrameworkCore;
namespace MamiMotovlog.Services;

public sealed class RiderReminderWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<RiderReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!config.GetValue<bool>("Rider:Enabled") || !config.GetValue<bool>("Mail:SendReminders")) return;
        try {
            await Task.Delay(TimeSpan.FromMinutes(1), token);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            do { await CheckOnce(token); } while(await timer.WaitForNextTickAsync(token));
        } catch(OperationCanceledException) when(token.IsCancellationRequested) { }
    }
    private async Task CheckOnce(CancellationToken token)
    {
        try {
            using var scope=scopes.CreateScope();
            var db=scope.ServiceProvider.GetRequiredService<RiderDbContext>();
            var mail=scope.ServiceProvider.GetRequiredService<RiderMailer>();
            if(!mail.CanSend) return;
            var now=DateTime.UtcNow; var today=now.AddHours(3).Date;
            var users=await db.Users.AsNoTracking().Where(x=>x.EmailConfirmed).ToListAsync(token);
            var members=users.Where(x=>x.LockoutEnd==null || x.LockoutEnd<=DateTimeOffset.UtcNow).ToDictionary(x=>x.Id);
            var disabled=(await db.PlatformEntries.AsNoTracking().Where(x=>x.Key.StartsWith("mail-off:")).Select(x=>x.Key).ToListAsync(token)).ToHashSet();
            bool CanNotify(string id)=>members.ContainsKey(id) && !disabled.Contains("mail-off:"+id) && !string.IsNullOrWhiteSpace(members[id].Email);
            var all=await db.RiderMaintenances.AsNoTracking().Include(x=>x.Bike).ToListAsync(token);
            var latest=all.GroupBy(x=>new {x.BikeId,x.Category}).Select(g=>g.OrderByDescending(x=>x.ServicedOn).ThenByDescending(x=>x.Id).First());
            foreach(var m in latest)
            {
                if(!CanNotify(m.Bike.OwnerId)) continue;
                var days=m.NextDate.HasValue?(m.NextDate.Value.Date-today).Days:(int?)null;
                var km=m.NextKm.HasValue?m.NextKm.Value-m.Bike.OdometerKm:(int?)null;
                var dateStage=days switch { <0=>3, <=1=>2, <=7=>1, _=>0 };
                var kmStage=km switch { <=0=>3, <=100=>2, <=500=>1, _=>0 };
                var stage=Math.Max(dateStage,kmStage); if(stage==0) continue;
                var key=$"maintenance:{m.Id}:stage";
                var stamp=await db.PlatformEntries.AsNoTracking().FirstOrDefaultAsync(x=>x.Key==key,token);
                if(stamp is not null && int.TryParse(stamp.Value,out var previous) && previous>=stage) continue;
                var prefix = $"maintenance:{m.Id}:sent:";
                var completed = await db.PlatformEntries.AsNoTracking().Where(x=>x.Key.StartsWith(prefix) && x.Value=="sent").Select(x=>x.Key).ToListAsync(token);
                if (completed.Any(x=>int.TryParse(x[prefix.Length..],out var sentStage) && sentStage>=stage)) continue;
                var claimKey = prefix + stage;
                var claim = await ReminderClaim.AcquireAsync(db, claimKey, token);
                if (claim is null) continue;
                var member=members[m.Bike.OwnerId];
                var detail=$"Merhaba {member.DisplayName},\n\n{m.Bike.BrandName} {m.Bike.ModelName} için {m.Category} bakımı {(stage==3?"vadesine ulaştı veya gecikti":"yaklaşıyor")}.";
                if(days.HasValue) detail+=$"\nBakım tarihi: {m.NextDate:dd.MM.yyyy} ({(days<0?"geçen gün":"kalan gün")}: {Math.Abs(days.Value)})";
                if(km.HasValue) detail+=$"\nBakım kilometresi: {m.NextKm} km ({(km<0?"aşılan km":"kalan km")}: {Math.Abs(km.Value)})";
                try {
                    await mail.SendAsync(member.Email!,"MAMI MOTOVLOG · Bakım hatırlatması",detail,$"https://mamimotovlog.com/garaj/motor/{m.BikeId}","Bakım kayıtlarımı görüntüle");
                } catch(Exception ex) when(!token.IsCancellationRequested) { logger.LogWarning(ex,"Bakım bildirimi gönderilemedi {Id}",m.Id); await ReminderClaim.ReleaseAsync(db,claimKey,claim,token); continue; }
                await ReminderClaim.CompleteAsync(db,claimKey,claim,token);
            }
            // User-created date reminders: once, not every day.
            var items=await db.RiderReminders.AsNoTracking().Where(x=>x.DueOn<=today.AddDays(7) && x.LastEmailedUtc==null).ToListAsync(token);
            foreach(var r in items)
            {
                if(!CanNotify(r.OwnerId)) continue;
                var claimKey = $"reminder:{r.Id}:sent";
                var claim = await ReminderClaim.AcquireAsync(db,claimKey,token);
                if (claim is null) continue;
                try {
                    await mail.SendAsync(members[r.OwnerId].Email!,"MAMI MOTOVLOG · Hatırlatma",$"{r.Title}\nTarih: {r.DueOn:dd.MM.yyyy}","https://mamimotovlog.com/garaj/hatirlaticilar","Hatırlatmalarımı görüntüle");
                } catch(Exception ex) when(!token.IsCancellationRequested) {logger.LogWarning(ex,"Hatırlatma gönderilemedi {Id}",r.Id); await ReminderClaim.ReleaseAsync(db,claimKey,claim,token); continue;}
                await ReminderClaim.CompleteAsync(db,claimKey,claim,token);
                await db.RiderReminders.Where(x=>x.Id==r.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.LastEmailedUtc,(DateTime?)now),token);
            }
        } catch(Exception ex) when(!token.IsCancellationRequested) {logger.LogError(ex,"Hatırlatma döngüsü başarısız");}
    }
}
