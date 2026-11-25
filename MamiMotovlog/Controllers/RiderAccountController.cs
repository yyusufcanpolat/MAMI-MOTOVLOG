using System.Text;
using MamiMotovlog.Models;
using MamiMotovlog.Services;
using MamiMotovlog.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Controllers;

[Route("uye")]
public class RiderAccountController(UserManager<RiderMember> users, SignInManager<RiderMember> signIn,
    RiderMailer mail, MamiMotovlog.Data.RiderDbContext riderDb, RiderDeletion deletion, IConfiguration config, IWebHostEnvironment env, ILogger<RiderAccountController> logger) : Controller
{
    [HttpGet("kayit")]
    public IActionResult Register() => View(new RiderRegisterVm());

    [HttpPost("kayit")][EnableRateLimiting("register")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RiderRegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!mail.CanSend && !(env.IsDevelopment() && config.GetValue<bool>("Mail:LocalConsoleOnly")))
        {
            ModelState.AddModelError("", "Üyelik için e-posta servisi henüz yapılandırılmadı.");
            return View(vm);
        }
        var user = new RiderMember { UserName = vm.Email.Trim().ToLowerInvariant(), Email = vm.Email.Trim().ToLowerInvariant(), DisplayName = vm.Name.Trim() };
        var result = await users.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Code == "DuplicateUserName" ? "Bu e-posta kullanılıyor." : error.Description);
            return View(vm);
        }
        try
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = Url.Action(nameof(Verify), "RiderAccount", new { id = user.Id, code }, env.IsDevelopment() ? Request.Scheme : "https", env.IsDevelopment() ? Request.Host.Value : "mamimotovlog.com")!;
            await mail.SendAsync(user.Email!, "MAMI MOTOVLOG · E-posta doğrulama", $"Merhaba {user.DisplayName},\n\nHesabını doğrulamak için aşağıdaki bağlantıyı kullan. Bu işlemi sen başlatmadıysan mesajı yok sayabilirsin.", url, "E-posta Adresinizi Doğrulamak İçin Tıklayınız");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yeni kullanıcıya doğrulama e-postası gönderilemedi");
            // Do not leave a login-enabled user behind; the email is not verified in any case.
            ModelState.AddModelError("", "Hesap oluşturuldu ancak doğrulama e-postası gönderilemedi. E-posta servisi kontrol edilmeli.");
            return View(vm);
        }
        TempData["RiderInfo"] = "Doğrulama bağlantısını e-posta adresine gönderdik. Giriş yapmadan önce onayla.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("dogrula")]
    public async Task<IActionResult> Verify(string id, string code)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(code) || code.Length > 4096) return BadRequest("Doğrulama bağlantısı geçersiz.");
        var user = await users.FindByIdAsync(id);
        if (user is null || string.IsNullOrEmpty(code)) return BadRequest("Doğrulama bağlantısı geçersiz.");
        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await users.ConfirmEmailAsync(user, token);
            TempData["RiderInfo"] = result.Succeeded ? "E-posta doğrulandı. Giriş yapabilirsin." : "Bağlantı geçersiz veya süresi dolmuş.";
        }
        catch (FormatException) { TempData["RiderInfo"] = "Bağlantı geçersiz."; }
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("giris")]
    public IActionResult Login() => View(new RiderLoginVm());

    [HttpPost("giris")][EnableRateLimiting("login")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(RiderLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var member = await users.FindByEmailAsync(vm.Email.Trim());
        if (member is null) { ModelState.AddModelError("", "E-posta veya şifre hatalı."); return View(vm); }
        var result = await signIn.PasswordSignInAsync(member, vm.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded) return Redirect("/garaj");
        ModelState.AddModelError("", result.IsNotAllowed ? "Lütfen e-posta adresini doğrula." : result.IsLockedOut ? "Çok fazla deneme. Daha sonra tekrar dene." : "E-posta veya şifre hatalı.");
        return View(vm);
    }

    [HttpPost("cikis")][Authorize(AuthenticationSchemes = "Identity.Application")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return Redirect("/"); }

    [HttpGet("profil")][Authorize(AuthenticationSchemes = "Identity.Application")]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return NotFound();
        ViewBag.ReminderEmails = !await riderDb.PlatformEntries.AnyAsync(x => x.Key == "mail-off:" + user.Id);
        return View(user);
    }

    [HttpPost("bildirimler")][Authorize(AuthenticationSchemes = "Identity.Application")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Notifications(bool enabled)
    {
        if (!ModelState.IsValid) { TempData["RiderInfo"] = "Bildirim tercihi geçersiz."; return RedirectToAction(nameof(Profile)); }
        var user = await users.GetUserAsync(User);
        if (user is null) return NotFound();
        var key = "mail-off:" + user.Id;
        var entry = await riderDb.PlatformEntries.FindAsync(key);
        if (enabled && entry is not null) riderDb.PlatformEntries.Remove(entry);
        if (!enabled && entry is null) riderDb.PlatformEntries.Add(new PlatformEntry { Key = key, Value = "1" });
        await riderDb.SaveChangesAsync();
        TempData["RiderInfo"] = "Bakım e-posta tercihin kaydedildi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("dogrulama-tekrar")][EnableRateLimiting("mail")][ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerification(string email)
    {
        // Intentionally identical response for unknown accounts (avoid account enumeration).
        var member = await users.FindByEmailAsync((email ?? "").Trim());
        if (member is not null && !member.EmailConfirmed && (mail.CanSend || (env.IsDevelopment() && config.GetValue<bool>("Mail:LocalConsoleOnly"))))
        {
            try
            {
                var token = await users.GenerateEmailConfirmationTokenAsync(member);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var url = Url.Action(nameof(Verify), "RiderAccount", new { id = member.Id, code }, env.IsDevelopment() ? Request.Scheme : "https", env.IsDevelopment() ? Request.Host.Value : "mamimotovlog.com")!;
                await mail.SendAsync(member.Email!, "MAMI MOTOVLOG · E-posta doğrulama", "Hesabını doğrulamak için aşağıdaki bağlantıyı kullan.", url, "E-posta Adresinizi Doğrulamak İçin Tıklayınız");
            }
            catch (Exception ex) { logger.LogWarning(ex, "Doğrulama tekrar e-postası gönderilemedi"); }
        }
        TempData["RiderInfo"] = "Kayıtlı ve doğrulanmamış bir hesap varsa doğrulama bağlantısı gönderildi.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost("hesabi-sil")][EnableRateLimiting("account-delete")][Authorize(AuthenticationSchemes = "Identity.Application")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string password)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return NotFound();
        if (string.IsNullOrEmpty(password) || !await users.CheckPasswordAsync(user, password))
        {
            TempData["RiderInfo"] = "Şifre doğrulanamadı. Hesap silinmedi.";
            return RedirectToAction(nameof(Profile));
        }
        await deletion.DeleteMemberAsync(user);
        await signIn.SignOutAsync();
        TempData["RiderInfo"] = "Hesabın ve garaj verilerin silindi.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("sifremi-unuttum")]
    public IActionResult Forgot() => View();

    [HttpPost("sifremi-unuttum")][EnableRateLimiting("mail")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Forgot(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await users.FindByEmailAsync(email.Trim());
            if (user is not null && await users.IsEmailConfirmedAsync(user) && (mail.CanSend || (env.IsDevelopment() && config.GetValue<bool>("Mail:LocalConsoleOnly"))))
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var url = Url.Action(nameof(Reset), "RiderAccount", new { email = user.Email, code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)) }, env.IsDevelopment() ? Request.Scheme : "https", env.IsDevelopment() ? Request.Host.Value : "mamimotovlog.com")!;
                try { await mail.SendAsync(user.Email!, "MAMI MOTOVLOG · Şifre sıfırlama", "Hesabın için bir şifre sıfırlama talebi aldık. İşlemi sen başlatmadıysan bu mesajı yok say.", url, "Şifrenizi Sıfırlamak İçin Lütfen Tıklayınız"); }
                catch (Exception ex) { logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi"); }
            }
        }
        TempData["RiderInfo"] = "Bu e-posta için doğrulanmış bir hesap varsa sıfırlama bağlantısı gönderildi.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("sifre-yenile")]
    public IActionResult Reset(string email, string code) => View(new RiderResetVm { Email = email ?? "", Code = code ?? "" });

    [HttpPost("sifre-yenile")][EnableRateLimiting("reset")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(RiderResetVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await users.FindByEmailAsync(vm.Email);
        if (user is null) { ModelState.AddModelError("", "İşlem gerçekleştirilemedi."); return View(vm); }
        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(vm.Code));
            var result = await users.ResetPasswordAsync(user, token, vm.Password);
            if (result.Succeeded) { TempData["RiderInfo"] = "Şifren güncellendi."; return RedirectToAction(nameof(Login)); }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        catch (FormatException) { ModelState.AddModelError("", "Bağlantı geçersiz."); }
        return View(vm);
    }
}
