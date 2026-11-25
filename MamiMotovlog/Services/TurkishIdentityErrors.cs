using Microsoft.AspNetCore.Identity;
namespace MamiMotovlog.Services;
public sealed class TurkishIdentityErrors : IdentityErrorDescriber
{
    private static IdentityError E(string code,string text)=>new(){Code=code,Description=text};
    public override IdentityError DefaultError()=>E(nameof(DefaultError),"İşlem tamamlanamadı.");
    public override IdentityError InvalidToken()=>E(nameof(InvalidToken),"Bağlantı geçersiz veya süresi dolmuş.");
    public override IdentityError DuplicateEmail(string email)=>E(nameof(DuplicateEmail),"Bu e-posta kullanılıyor.");
    public override IdentityError DuplicateUserName(string userName)=>E(nameof(DuplicateUserName),"Bu e-posta kullanılıyor.");
    public override IdentityError InvalidEmail(string? email)=>E(nameof(InvalidEmail),"Geçerli bir e-posta adresi girin.");
    public override IdentityError PasswordTooShort(int length)=>E(nameof(PasswordTooShort),$"Şifre en az {length} karakter olmalıdır.");
    public override IdentityError PasswordRequiresNonAlphanumeric()=>E(nameof(PasswordRequiresNonAlphanumeric),"Şifre özel karakter içermelidir.");
    public override IdentityError PasswordRequiresDigit()=>E(nameof(PasswordRequiresDigit),"Şifre rakam içermelidir.");
    public override IdentityError PasswordRequiresLower()=>E(nameof(PasswordRequiresLower),"Şifre küçük harf içermelidir.");
    public override IdentityError PasswordRequiresUpper()=>E(nameof(PasswordRequiresUpper),"Şifre büyük harf içermelidir.");
}
