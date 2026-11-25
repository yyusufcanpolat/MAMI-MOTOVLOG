# MAMI MOTOVLOG — Journal V5.4

Gerçek müşteri Muhammet için ASP.NET Core 10 MVC / Razor, EF Core, SQL Server 2019 ve yerel SQLite ile geliştirilmiş motosiklet içerik ve kişisel garaj platformu. Canlı adres: https://mamimotovlog.com (V5.4 dağıtımı ayrıca doğrulanmalıdır).

Üst klasördeki `BASLANGIC.md` ve `TESTLER_VE_TESLIM.md` yetkili güncelleme rehberidir. Kaynakta gerçek SQL/SMTP parolaları yoktur. Canlı Production ayarını ve özel yüklemeleri GitHub'a koymayın. `Database/V52_UPDATE_SQLSERVER.sql` mevcut V5.1 verisini silmeden şemayı genişletir. Yerel başlatma ve derleme test komutları üst klasördedir.

## Mimari
- `Controllers`: HTTP GET/POST, yetki, doğrulama, CRUD; yönetici ve üye cookie şemaları ayrıdır.
- `Models` / `Data`: ilişkili içerik ve Rider tabloları, Identity üyeleri, EF Core bağlamları.
- `Views` / `wwwroot`: Razor, responsive CSS, JavaScript, açık harita ve hava servisi.
- `Services`: tam-depo yakıt tüketimi, SMTP HTML e-postası, bakım hatırlatma, özel dosya, güvenli silme ve işlem geçmişi.
- `Database`: V3 temel SQL şeması, V5.2 ve V5.4 eklemeli SQL yükseltmeleri.
- `Tests`: önceki JS davranış testleri, V5.2 kaynak sözleşmesi kontrolü, C# yakıt birim testi.

## Geliştirme notu
Müşterinin orijinal brief kanıtı bu kaynak paketinin yerine geçmez. GitHub erişilebilirliği ve canlı V5.4 yayını teslimden önce öğrenci tarafından tamamlanmalıdır. Örnek içerik gerçek sürüş olarak sunulmaz. GPX kaldırılmıştır. Katalog doğrulanmamış model teknik değerlerini uydurmaz. Yakıt fiyatı istasyon garantisi değildir.
