using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MamiMotovlog.Models;

public class RiderMember : IdentityUser
{
    [MaxLength(75)] public string DisplayName { get; set; } = "";
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}
public class BikeBrand
{
    public int Id { get; set; }
    [MaxLength(75)] public string Name { get; set; } = "";
    public ICollection<BikeModel> Models { get; set; } = new List<BikeModel>();
}
public class BikeModel
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public BikeBrand Brand { get; set; } = null!;
    [MaxLength(100)] public string Name { get; set; } = "";
    public ICollection<BikeVariant> Variants { get; set; } = new List<BikeVariant>();
}
public class BikeVariant
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public BikeModel Model { get; set; } = null!;
    public int Year { get; set; }
    [MaxLength(85)] public string Edition { get; set; } = "Standart";
    public int? EngineCc { get; set; }
    [Range(0,1000)] public decimal? PowerHp { get; set; }
    [Range(0,1000)] public decimal? TorqueNm { get; set; }
    [Range(0,150)] public decimal? TankLitres { get; set; }
    // Unknown manufacturer data must remain NULL, never fictitious.
}
public class RiderBike
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = "";
    public RiderMember Owner { get; set; } = null!;
    public int? VariantId { get; set; }
    public BikeVariant? Variant { get; set; }
    [MaxLength(90)] public string BrandName { get; set; } = "";
    [MaxLength(110)] public string ModelName { get; set; } = "";
    public int ModelYear { get; set; }
    [MaxLength(100)] public string Edition { get; set; } = "";
    [Range(0,2000000)] public int OdometerKm { get; set; }
    [Range(0,100)] public decimal? AverageFuelL100 { get; set; }
    [Range(0,150)] public decimal? TankLitres { get; set; }
    [MaxLength(1000)] public string Notes { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public class RiderMaintenance
{
    public int Id { get; set; }
    public int BikeId { get; set; }
    public RiderBike Bike { get; set; } = null!;
    [MaxLength(90)] public string Category { get; set; } = "";
    [MaxLength(300)] public string Description { get; set; } = "";
    public DateTime ServicedOn { get; set; }
    [Range(0,2000000)] public int AtKm { get; set; }
    [Range(0,9999999)] public decimal CostTry { get; set; }
    public DateTime? NextDate { get; set; }
    public int? NextKm { get; set; }
    [MaxLength(90)] public string AttachmentFileName { get; set; } = "";
}
public class RiderFuel
{
    public int Id { get; set; }
    public int BikeId { get; set; }
    public RiderBike Bike { get; set; } = null!;
    public DateTime FuelledOn { get; set; }
    [Range(0,2000000)] public int AtKm { get; set; }
    [Range(0.01,150)] public decimal Litres { get; set; }
    [Range(0,999999)] public decimal TotalTry { get; set; }
    public bool FullTank { get; set; }
}
public class RiderExpense
{
    public int Id { get; set; }
    public int BikeId { get; set; }
    public RiderBike Bike { get; set; } = null!;
    [MaxLength(40)] public string Category { get; set; } = "Diğer";
    [MaxLength(220)] public string Description { get; set; } = "";
    public DateTime SpentOn { get; set; }
    [Range(0,9999999)] public decimal AmountTry { get; set; }
}
public class RiderEquipment
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = "";
    [MaxLength(50)] public string Category { get; set; } = "";
    [MaxLength(120)] public string Brand { get; set; } = "";
    [MaxLength(120)] public string Model { get; set; } = "";
    public DateTime? PurchasedOn { get; set; }
    public DateTime? WarrantyEndsOn { get; set; }
    public decimal? CostTry { get; set; }
    [MaxLength(90)] public string PhotoFileName { get; set; } = "";
}
public class RiderTrip
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = "";
    public int? BikeId { get; set; }
    public RiderBike? Bike { get; set; }
    [MaxLength(140)] public string Title { get; set; } = "";
    [MaxLength(120)] public string StartLabel { get; set; } = "";
    [MaxLength(120)] public string EndLabel { get; set; } = "";
    public double StartLat { get; set; }
    public double StartLon { get; set; }
    public double EndLat { get; set; }
    public double EndLon { get; set; }
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOn { get; set; }
    public decimal? ActualSpendTry { get; set; }
    [MaxLength(2000)] public string Journal { get; set; } = "";
    public string GeoJson { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsApproved { get; set; }
}
public class RiderReminder
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = "";
    public int BikeId { get; set; }
    [MaxLength(65)] public string Kind { get; set; } = "";
    [MaxLength(180)] public string Title { get; set; } = "";
    public DateTime DueOn { get; set; }
    public DateTime? LastEmailedUtc { get; set; }
}

public class RiderFavorite
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = "";
    public int TripId { get; set; }
    public RiderTrip Trip { get; set; } = null!;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class PlatformEntry
{
    [Key, MaxLength(200)] public string Key { get; set; } = "";
    [MaxLength(2000)] public string Value { get; set; } = "";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
public class AdminAudit
{
    public int Id { get; set; }
    [MaxLength(250)] public string Operation { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
