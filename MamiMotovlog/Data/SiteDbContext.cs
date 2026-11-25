using MamiMotovlog.Models;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Data;

public class SiteDbContext(DbContextOptions<SiteDbContext> options) : DbContext(options)
{
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();
    public DbSet<Gear> Gear => Set<Gear>();
    public DbSet<SiteSettings> Settings => Set<SiteSettings>();
    public DbSet<ContactMessage> Messages => Set<ContactMessage>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Video>().HasOne(x => x.Ride).WithMany().HasForeignKey(x => x.RideId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Album>().HasOne(x => x.Ride).WithMany().HasForeignKey(x => x.RideId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<AlbumPhoto>().HasOne(x => x.Album).WithMany(x => x.Photos).HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<AdminAccount>().HasIndex(x => x.Key).IsUnique();
    }
}
