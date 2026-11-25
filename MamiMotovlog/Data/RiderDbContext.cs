using MamiMotovlog.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Data;

// Independent context: original V2 SiteDbContext and its data are unchanged.
public class RiderDbContext(DbContextOptions<RiderDbContext> options) : IdentityDbContext<RiderMember>(options)
{
    public DbSet<BikeBrand> BikeBrands => Set<BikeBrand>();
    public DbSet<BikeModel> BikeModels => Set<BikeModel>();
    public DbSet<BikeVariant> BikeVariants => Set<BikeVariant>();
    public DbSet<RiderBike> RiderBikes => Set<RiderBike>();
    public DbSet<RiderMaintenance> RiderMaintenances => Set<RiderMaintenance>();
    public DbSet<RiderFuel> RiderFuels => Set<RiderFuel>();
    public DbSet<RiderExpense> RiderExpenses => Set<RiderExpense>();
    public DbSet<RiderEquipment> RiderEquipments => Set<RiderEquipment>();
    public DbSet<RiderTrip> RiderTrips => Set<RiderTrip>();
    public DbSet<RiderReminder> RiderReminders => Set<RiderReminder>();
    public DbSet<RiderFavorite> RiderFavorites => Set<RiderFavorite>();
    public DbSet<PlatformEntry> PlatformEntries => Set<PlatformEntry>();
    public DbSet<AdminAudit> AdminAudits => Set<AdminAudit>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<RiderMember>().Property(x => x.DisplayName).HasMaxLength(75);
        b.Entity<BikeBrand>().HasIndex(x => x.Name).IsUnique();
        b.Entity<BikeModel>().HasIndex(x => new { x.BrandId, x.Name }).IsUnique();
        b.Entity<BikeVariant>().HasIndex(x => new { x.ModelId, x.Year, x.Edition }).IsUnique();
        b.Entity<RiderBike>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RiderBike>().HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<RiderMaintenance>().HasOne(x => x.Bike).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RiderFuel>().HasOne(x => x.Bike).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RiderExpense>().HasOne(x => x.Bike).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RiderTrip>().HasOne(x => x.Bike).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<RiderEquipment>().HasIndex(x => x.OwnerId);
        b.Entity<RiderTrip>().HasIndex(x => x.OwnerId);
        b.Entity<RiderReminder>().HasIndex(x => x.OwnerId);
        b.Entity<RiderFavorite>().HasOne(x=>x.Trip).WithMany().HasForeignKey(x=>x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RiderFavorite>().HasIndex(x=>new{x.OwnerId,x.TripId}).IsUnique();
        b.Entity<RiderFuel>().HasIndex(x => new { x.BikeId, x.AtKm }).IsUnique();
        b.Entity<RiderBike>().Property(x => x.OdometerKm).IsConcurrencyToken();
        b.Entity<RiderEquipment>().Property(x => x.CostTry).HasPrecision(12,2);
        b.Entity<RiderFuel>().Property(x => x.Litres).HasPrecision(10,2);
        b.Entity<RiderFuel>().Property(x => x.TotalTry).HasPrecision(12,2);
        b.Entity<RiderExpense>().Property(x => x.AmountTry).HasPrecision(12,2);
        b.Entity<RiderMaintenance>().Property(x => x.CostTry).HasPrecision(12,2);
        b.Entity<RiderEquipment>().Property(x => x.CostTry).HasPrecision(12,2);
        b.Entity<RiderBike>().Property(x => x.AverageFuelL100).HasPrecision(8,2);
        b.Entity<RiderBike>().Property(x => x.TankLitres).HasPrecision(8,2);
        b.Entity<BikeVariant>().Property(x => x.PowerHp).HasPrecision(8,2);
        b.Entity<BikeVariant>().Property(x => x.TorqueNm).HasPrecision(8,2);
        b.Entity<BikeVariant>().Property(x => x.TankLitres).HasPrecision(8,2);
        b.Entity<RiderTrip>().Property(x => x.DistanceKm).HasPrecision(11,2);
        b.Entity<RiderTrip>().Property(x => x.ActualSpendTry).HasPrecision(12,2);
    }
}
