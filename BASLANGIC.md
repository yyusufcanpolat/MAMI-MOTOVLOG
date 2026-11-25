# MAMI MOTOVLOG JOURNAL V5.4

**Son V5.3 paketi üzerinden hazırlanmış V5.4 düzeltme adayı.** Bu ZIP kaynak kodudur; doğrudan Plesk httpdocs içine açılacak hazır publish değildir. Önce yerelde doğrulayın. Canlı SQL veritabanına bu çalışma sırasında bağlantı kurulmamıştır.

## 1. Yerel test (kişisel bilgisayar)

1. ZIP'i tamamen yeni bir klasöre çıkarın. `MamiMotovlog`, `Tests`, `01_TESTLER.cmd` ve diğer CMD dosyaları aynı ana klasörde kalmalı.
2. Öncelikle `00_OTOMATIK_TEST.md` içindeki iki PowerShell komutunu çalıştırın; isterseniz `01_TESTLER.cmd` ile genişletilmiş testleri çalıştırın. .NET 10 SDK, NuGet bağlantısı ve Node.js gerektirir. Derleme veya test hata verirse yayına geçmeyin.
3. İlk yerel yönetici için kişisel PC'de `cd MamiMotovlog` ardından `dotnet user-secrets set "Admin:Password" "KENDI_GUCLU_YEREL_SIFRENIZ"` kullanın. Gerçek canlı şifreyi kullanmayın. Üst klasöre dönüp `02_YEREL_BASLAT.cmd` çalıştırın. Yerel SQLite, canlı SQL'i kullanmaz. Üyelik test e-posta bağlantısı geliştirme konsoluna yazılır.
4. Tarayıcıdan `http://127.0.0.1:5000` ve `/yonetim/giris` açın. `TESTLER_VE_TESLIM.md` adımlarını tamamlayın.

## 2. Canlı V5.1'den V5.4'e geçiş (veri silmeden)

**Önce V5.4 derlemesini ve yerel testleri tamamlayın.** Canlı `mamimoto_mami_v5` veritabanını, mevcut `httpdocs` dosyalarını, gizli Production ayarını, özel yükleme klasörlerini ve anahtarları güvenli yerde yedekleyin. SSMS/Plesk yedeği veya sağlayıcı yedeği alın. Mevcut kullanıcı, rota, ekipman, fotoğraf ve yönetici verileri korunacaktır; bu paket onları sıfırlamaz.

1. `03_YAYIN_HAZIRLA.cmd` çalıştırın. Bu, `YAYIN_V54` klasöründe framework-dependent yayın üretir; gerçek Production şifresini içermez. Derleme geçmezse durun.
2. SSMS'te yalnızca kendi mevcut V5.1 veritabanınızı seçin: `mamimoto_mami_v5`. `SELECT DB_NAME();` ile doğrulayın. `MamiMotovlog/Database/V52_UPDATE_SQLSERVER.sql` dosyasını açın, tamamını çalıştırın. Sonuç: `MAMI V5.2 update OK. Existing data preserved.` Ardından `V54_UPDATE_SQLSERVER.sql` (4 yeni içerik sütunu ve yakıt benzersiz indeksi) ve `V54_CHECK_SQLSERVER.sql` (28 tablo, 195 sütun ve indeks/FK raporu) çalıştırın. Scriptler eklemelidir, tekrar çalıştırılabilir; DROP/DELETE yoktur. Eski çift yakıt kaydı varsa indeks eklenmez, kayıtlar silinmez. **V3 kurulum scriptini tekrar çalıştırmanız gerekmez.**
3. Plesk'te `app_offline.htm` ile bakıma alın. Yayın klasörünün *içeriğini* güvenli geçici klasörden `httpdocs` içine aktarın. Eski klasörleri topluca silmeyin. Özellikle `appsettings.Production.json`, `App_Data`, `wwwroot/uploads`, DataProtection anahtarları ve ilgili hosting ayarlarını koruyun. Yükleme klasörü ve SMTP ayarları eski V5.1 ile aynı olmalı. Mevcut SQL bağlantısı aynı veritabanına yönelmeli. V5.4 için yeni DB/hosting/domain gerekmez.
4. Mevcut `appsettings.Production.json` içinde `Mail:SendReminders` değerini `true` yapın. Diğer SMTP ve SQL değerlerini değiştirmeyin. `Fuel:BackupFeedUrl` yalnızca aynı şemayı sağlayan, gerçekten doğrulanmış HTTPS feed varsa eklenir (varsayılan boş). Parolaları paylaşmayın. `web.config` içinde gerekli IIS ayarlarını koruyun; normal yayında stdout loglarını kapatın, sorun halinde kısa süreli açıp güvenli şekilde inceleyin.
5. `app_offline.htm` kaldırın. Site, giriş, kayıtlı üyeler, eski motosiklet/rota verileri, yönetim paneli ve e-posta akışını kontrol edin. Başarısız olursa V5.1 dosyalarına geri dönün; yeni ek sütun ve tablolar V5.1'e zarar vermemelidir. Gerekirse sağlayıcı desteğiyle yedeği geri yükleyin (yeni kayıtları kaybetmemek için önce durumu değerlendirin).

## 3. Muhammet'in yönetim paneli

`https://mamimotovlog.com/yonetim/giris` — mevcut yönetici parolası SQL'de hash olarak saklanır. Yeni yönetici oluşturmak veya Production şifresini sıfırlamak gerekmez. V5.3 ve sonrası güvenlik damgası nedeniyle mevcut yönetici oturumları bir kez kapanır; aynı şifreyle yeniden giriş yapılır. Menü: üyeler, motosikletler, kayıtlı rotalar, katalog/onaylar, videolar, gezi yazıları, fotoğraf, ekipman, mesaj, site ayarları, işlem geçmişi ve parola.

## 4. Yeni SQL kurulum (sadece boş test DB gerekirse)

İlk kez boş SQL DB kurulacaksa uygulamayı `Rider:Enabled=false` ile başlatıp SiteDbContext tablolarını oluşturun, sonra `V3_INSTALL_SQLSERVER.sql`, `V52_UPDATE_SQLSERVER.sql` ve `V54_UPDATE_SQLSERVER.sql` çalıştırın. Ardından Rider'i etkinleştirin. Bu akış mevcut V5.1 canlı site için kullanılmaz.

## 5. Sınırlar

Bu ortamda .NET SDK bulunmadığı ve NuGet/DNS erişimi olmadığı için C# ve Razor derlemesi, gerçek MSSQL/SMTP ve dış API testi burada yürütülememiştir. Node testleri ve statik kontroller ayrı raporlanmıştır. IIS uygulaması uykuya geçerse bakım e-postası arka plan işçisi çalışmaz; hostingde always-on/uygulama havuzu ayarını teyit edin. Veritabanı tabanlı bir saatlik gönderim kilidi normal eş zamanlı tekrarları engeller. SMTP ile veritabanı atomik bir işlem olmadığından olağanüstü kesintilerde aynı mail yeniden gönderilebilir. Alternatif otomatik fiyat servisi doğrulanmışsa tanımlanır; aksi halde yönetici 24 saat geçerli, kaynağı belli referans fiyat girebilir. Eski fiyat otomatik kullanılmaz.
