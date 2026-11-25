using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MamiMotovlog.Models;

namespace MamiMotovlog.ViewModels;

public class RiderRegisterVm
{
    [Required(ErrorMessage="Ad gerekli.")][StringLength(75,MinimumLength=2, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Name { get; set; } = "";
    [Required(ErrorMessage="E-posta gerekli.")][EmailAddress(ErrorMessage="Geçerli e-posta adresi girin.")][StringLength(180, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Email { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(100,MinimumLength=10, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")][DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required(ErrorMessage="Şifre tekrarını girin.")][Compare(nameof(Password),ErrorMessage="Şifreler eşleşmiyor.")][DataType(DataType.Password)] public string Confirm { get; set; } = "";
}
public class RiderLoginVm
{
    [Required(ErrorMessage="Bu alan zorunludur.")][EmailAddress(ErrorMessage="Geçerli e-posta adresi girin.")] public string Email { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class RiderResetVm
{
    [Required(ErrorMessage="Bu alan zorunludur.")][EmailAddress(ErrorMessage="Geçerli e-posta adresi girin.")] public string Email { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")] public string Code { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(100,MinimumLength=10, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")][DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class RiderBikeVm
{
    public int? VariantId { get; set; }
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(90, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string BrandName { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(110, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string ModelName { get; set; } = "";
    [Range(1950,2035, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public int ModelYear { get; set; } = MamiMotovlog.Services.TurkeyClock.Today.Year;
    [StringLength(100, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? Edition { get; set; } = "";
    [Range(0,2000000, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public int OdometerKm { get; set; }
    [Range(0.1,50, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] public decimal? AverageFuelL100 { get; set; }
    [Range(0.5,150, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] public decimal? TankLitres { get; set; }
    [StringLength(1000, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? Notes { get; set; } = "";
}
public class RiderFuelVm
{
    [Required(ErrorMessage="Bu alan zorunludur.")] public int BikeId { get; set; }
    [DataType(DataType.Date)] [BindRequired] public DateTime Date { get; set; } = MamiMotovlog.Services.TurkeyClock.Today;
    [Range(0,2000000, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public int OdometerKm { get; set; }
    [Range(0.01,150, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public decimal Litres { get; set; }
    [Range(0.01,999999, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public decimal AmountTry { get; set; }
    [BindRequired] public bool FullTank { get; set; }
}
public class RiderMaintenanceVm
{
    public int BikeId { get; set; }
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(90, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Category { get; set; } = "";
    [StringLength(300, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? Description { get; set; } = "";
    [DataType(DataType.Date)] [BindRequired] public DateTime Date { get; set; } = MamiMotovlog.Services.TurkeyClock.Today;
    [Range(0,2000000, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public int OdometerKm { get; set; }
    [Range(0,9999999, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] public decimal? CostTry { get; set; }
    [DataType(DataType.Date)] public DateTime? NextDate { get; set; }
    [Range(1,2000000, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] public int? NextKm { get; set; }
}
public class RiderExpenseVm
{
    public int BikeId { get; set; }
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(40, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Category { get; set; } = "Diğer";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(220, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? Description { get; set; } = "";
    [DataType(DataType.Date)] [BindRequired] public DateTime Date { get; set; } = MamiMotovlog.Services.TurkeyClock.Today;
    [Range(0.01,9999999, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public decimal AmountTry { get; set; }
}
public class RiderGearVm
{
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(50, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Category { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(120, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Brand { get; set; } = "";
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(120, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Model { get; set; } = "";
    [DataType(DataType.Date)] public DateTime? PurchasedOn { get; set; }
    [DataType(DataType.Date)] public DateTime? WarrantyEndsOn { get; set; }
    [Range(0,999999, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] public decimal? CostTry { get; set; }
}
public class RiderTripVm
{
    [Required(ErrorMessage="Bu alan zorunludur.")][StringLength(140, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Title { get; set; } = "";
    [StringLength(120, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? StartLabel { get; set; } = "";
    [StringLength(120, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string? EndLabel { get; set; } = "";
    [Range(-90,90, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public double StartLat { get; set; }
    [Range(-180,180, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public double StartLon { get; set; }
    [Range(-90,90, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public double EndLat { get; set; }
    [Range(-180,180, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public double EndLon { get; set; }
    [Range(0.1,20000, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public decimal DistanceKm { get; set; }
    [Range(1,99999, ErrorMessage="{0} alanı {1} ile {2} arasında olmalıdır.")] [BindRequired] public int DurationMinutes { get; set; }
    public int? BikeId { get; set; }
    public string GeoJson { get; set; } = "";
}
public class RiderDashboardVm
{
    public required string DisplayName { get; init; }
    public List<RiderBike> Bikes { get; init; } = [];
    public Dictionary<int, decimal?> MeasuredFuel { get; init; } = [];
    public List<RiderTrip> Trips { get; init; } = [];
    public int CompletedTripCount { get; init; }
    public List<RiderReminder> Reminders { get; init; } = [];
    public decimal FuelSpent { get; init; }
    public decimal MaintenanceSpent { get; init; }
    public decimal OtherSpent { get; init; }
    public decimal TotalKm { get; init; }
}
public class RiderBikeDetailsVm
{
    public RiderBike Bike { get; init; } = null!;
    public List<RiderFuel> Fuels { get; init; } = [];
    public List<RiderMaintenance> Maintenances { get; init; } = [];
    public List<RiderExpense> Expenses { get; init; } = [];
    public decimal? ConsumptionL100 { get; init; }
    public bool ConsumptionMeasured { get; init; }
    public int ConsumptionIntervalCount { get; init; }
}
public class RiderYearVm
{
    public int Year { get; init; }
    public int CompletedTrips { get; init; }
    public decimal CompletedDistanceKm { get; init; }
    public decimal FuelTry { get; init; }
    public decimal MaintenanceTry { get; init; }
    public decimal ExpensesTry { get; init; }
    public decimal[] MonthlyDistancesKm { get; init; } = new decimal[12];
}
