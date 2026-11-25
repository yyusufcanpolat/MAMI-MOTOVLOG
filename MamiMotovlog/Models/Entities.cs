using System.ComponentModel.DataAnnotations;

namespace MamiMotovlog.Models;

public class Video
{
    public int Id { get; set; }
    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(550)] public string Summary { get; set; } = "";
    [MaxLength(300)] public string YouTubeUrl { get; set; } = "";
    public int? RideId { get; set; }
    public Ride? Ride { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
}
public class Ride
{
    public int Id { get; set; }
    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(550)] public string Summary { get; set; } = "";
    public string Story { get; set; } = "";
    [MaxLength(130)] public string StartPoint { get; set; } = "";
    [MaxLength(130)] public string EndPoint { get; set; } = "";
    [MaxLength(350)] public string Stops { get; set; } = "";
    public int? DistanceKm { get; set; }
    [MaxLength(500)] public string MapUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
}
public class Album
{
    public int Id { get; set; }
    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(550)] public string Description { get; set; } = "";
    public int? RideId { get; set; }
    public Ride? Ride { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
    public List<AlbumPhoto> Photos { get; set; } = new();
}
public class AlbumPhoto
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    [MaxLength(260)] public string ImagePath { get; set; } = "";
    [MaxLength(180)] public string Caption { get; set; } = "";
    public int SortOrder { get; set; }
}
public class Gear
{
    public int Id { get; set; }
    [MaxLength(40)] public string Category { get; set; } = "";
    [MaxLength(130)] public string Name { get; set; } = "";
    [MaxLength(550)] public string Summary { get; set; } = "";
    public string Review { get; set; } = "";
    [MaxLength(500)] public string ExternalUrl { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
public class SiteSettings
{
    public int Id { get; set; }
    [MaxLength(200)] public string YouTubeChannelUrl { get; set; } = "";
    [MaxLength(200)] public string InstagramUrl { get; set; } = "";
    [MaxLength(200)] public string TikTokUrl { get; set; } = "";
    [MaxLength(180)] public string ContactEmail { get; set; } = "";
    [MaxLength(100)] public string HeroTitle { get; set; } = "";
    [MaxLength(550)] public string HeroDescription { get; set; } = "";
    [MaxLength(260)] public string HeroImage { get; set; } = "";
    [MaxLength(2000)] public string AboutText { get; set; } = "";
    [MaxLength(100)] public string MotorcycleTitle { get; set; } = "";
    [MaxLength(4000)] public string MotorcycleText { get; set; } = "";
    [MaxLength(100)] public string GuideTitle { get; set; } = "";
    [MaxLength(8000)] public string GuideText { get; set; } = "";
}
public class ContactMessage
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(11)] public string Phone { get; set; } = "";
    [MaxLength(140)] public string Subject { get; set; } = "";
    [MaxLength(2500)] public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
public class AdminAccount
{
    public int Id { get; set; }
    [MaxLength(48)] public string Key { get; set; } = "owner";
    public string PasswordHash { get; set; } = "";
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
