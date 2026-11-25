using System.ComponentModel.DataAnnotations;
using MamiMotovlog.Models;

namespace MamiMotovlog.ViewModels;
public class HomeVm
{
    public Video? Video { get; set; }
    public List<Ride> GuideRides { get; set; } = new();
    public Ride? Ride { get; set; }
    public Album? Album { get; set; }
    public SiteSettings Settings { get; set; } = new();
}
public class DetailVideoVm { public Video Video { get; set; } = null!; public string YoutubeId { get; set; } = ""; public Album? LinkedAlbum { get; set; } }
public class DetailRideVm { public Ride Ride { get; set; } = null!; public List<Video> Videos { get; set; } = new(); public List<Album> Albums { get; set; } = new(); }
public class ContactVm
{
    [Required(ErrorMessage = "Adınızı yazın.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Adınız 2–100 karakter olmalı.")]
    [Display(Name = "Ad Soyad")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "E-posta adresi gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi yazın.")]
    [StringLength(180, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Telefon numarası gerekli.")]
    [RegularExpression(@"^05[0-9]{9}$", ErrorMessage = "Telefon 05 ile başlamalı ve 11 rakam olmalı (05XXXXXXXXX).")]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Konu yazın.")]
    [StringLength(140, MinimumLength = 3, ErrorMessage="{0} alanının uzunluğu geçersiz (en fazla {1} karakter).")] public string Subject { get; set; } = "";
    [Required(ErrorMessage = "Mesajınızı yazın.")]
    [StringLength(2500, MinimumLength = 10, ErrorMessage = "Mesajınız 10–2500 karakter olmalı.")]
    public string Message { get; set; } = "";
    public string Website { get; set; } = ""; // Botlara karşı görünmez alan
}
public class LoginVm
{
    [Required(ErrorMessage = "Şifre gerekli.")][DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class ChangePasswordVm
{
    [Required(ErrorMessage = "Mevcut şifreni gir.")]
    [DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Yeni şifreni gir.")]
    [MinLength(12, ErrorMessage = "Yeni şifre en az 12 karakter olmalı.")]
    [DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Required(ErrorMessage = "Şifre tekrarını gir.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler aynı değil.")]
    [DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class ListItemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool Published { get; set; }
}
public class AdminListVm
{
    public string Kind { get; set; } = "";
    public string KindTitle { get; set; } = "";
    public List<ListItemVm> Items { get; set; } = new();
}
public class AdminEditVm
{
    public string Kind { get; set; } = "";
    public int? Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Body { get; set; } = "";
    public string Link { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string Stops { get; set; } = "";
    public int? DistanceKm { get; set; }
    public string MapUrl { get; set; } = "";
    public string Category { get; set; } = "";
    public int? RideId { get; set; }
    public bool Published { get; set; }
    public List<Ride> Rides { get; set; } = new();
    public List<AlbumPhoto> Photos { get; set; } = new();
}
public class DashboardVm
{
    public int Videos { get; set; }
    public int Rides { get; set; }
    public int Albums { get; set; }
    public int Messages { get; set; }
    public List<ContactMessage> RecentMessages { get; set; } = new();
}
