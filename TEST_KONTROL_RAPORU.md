# V5.4 kaynak inceleme ve test sonucu

Temel alınan tek ZIP: MAMI_MOTOVLOG_V5_3_KAPSAMLI_DUZELTME.zip. V5.1 canlı ortamına ve gerçek SQL Server'a bağlanılmadı, hiçbir canlı veri değiştirilmedi. V5.3 raporu tarihsel karşılaştırma amacıyla ayrıdır.

## Uygulanan değişiklikler

| Alan | Değişiklik |
|---|---|
| Rota kayıt JS | CSRF başlığı açıkça gönderilir; hatalı/başka siteye yönlendirme ve beklenmeyen kayıt URL'si reddedilir; düğme tekrar etkinleşir. |
| Rota servisi | Eksik koordinat ve ardışık aynı nokta doğrulaması; hava ve yakıt ayrı hız sınırı. |
| Sunucu hata yanıtı | Üretimde JSON isteyen istemciye tutarlı hata yanıtı. |
| Yakıt | Aynı motosiklet ve kilometre için benzersiz indeks; eş zamanlı kilometre güncellemesinde EF concurrency kontrolü ve form mesajı. |
| Bakım / kilometre | Eş zamanlı veri değişiminde kullanıcıya form hatası. Türkiye tarih standardı. |
| Bakım e-postaları | PlatformEntries üzerinde bir saatlik atomik gönderim rezervasyonu, başarılı gönderimde sent durumu; silinen hatırlatmaların rezervasyonlarının temizliği. |
| Yönetim | Motosiklet tanıtım ve yol rehberi başlık/metin düzenleme; varsayılan içerik boş bırakıldığında korunur. |
| Albüm | Bir grup fotoğrafın hatalı olması halinde kısmi DB kaydı yapılmaz; başarısız yüklemede yeni dosyalar temizlenmeye çalışılır. |
| Gizlilik | Taslak rota ilişkilerinin yayınlanan albüm/video detaylarında gizlenmesi; admin audit kaydında kişisel URL parametreleri yerine işlem şablonu. |
| Benzin kaynağı | HTTP yanıt boyutu ve süre sınırı; mevcut manuel, kaynak gösterilen 24 saatlik referans korunur. Doğrulanmış ikinci otomatik kaynak mevcut değil. |
| JS / önbellek | Leaflet yüklenmezse yakıt kontrolünde başlatılmamış değişken hatası önlenir; service worker sürümü değiştirilir. |
| SQL Server | V54 eklemeli SQL yükseltmesi: dört yeni sütun, yakıt benzersiz indeksi (eski çift kayıtlar silinmez); 28 tablo/195 sütun için genişletilmiş salt okunur ön kontrol ve FK/index raporu. |
| SQLite | Aynı dört içerik sütunu ve benzersiz indeks için yerel yükseltme; RiderEquipment maliyet hassasiyeti SQL ile hizalandı. |
| Dağıtım / test | Gizli dosyalar hariç, PowerShell ile toplu test, genişletilmiş gerçek HTTP test senaryoları. |

## Gerçekten çalıştırılan kontroller

- Node.js: 6 test dosyası başarılı (map, picker, journal-v5, v52, v53, v54). Bunların bazıları statik kaynak sözleşmesi kontrolüdür; bazıları JS davranışını mock DOM/Leaflet ile çalıştırır.
- Chromium: sentetik HTML üzerinde dosya seçimi, silme onayı/iptal, mobil navigasyon, 390px genişlik kontrolü başarılı. Gerçek Razor sayfaları değildir.
- SQLite: Python sqlite3 ile kaynakta bulunan gerçek dört ALTER TABLE ve CREATE UNIQUE INDEX ifadesi geçici örnek veritabanında çalıştırıldı; önceki veri korundu, aynı indeks tekrar kurulabildi, çift kayıt reddedildi. Tam EF şeması ve C# çalıştırılmadı.
- wwwroot altındaki JavaScript dosyaları Node syntax kontrolünden geçti.

## Açık kalan doğrulamalar / sınırlar

- Bu ortamda .NET SDK yok, SDK indirme DNS hatasıyla mümkün olmadı. V5.4 C#/Razor derlemesi, C# birim testi ve gerçek HTTP entegrasyon testi çalıştırılmadı. Kaynak paketi yayın adayıdır, hatasız olduğu garantilenemez. Kullanıcının bildirdiği ancak açıklamadığı 24 hatanın tamamının giderildiği doğrulanamaz.
- Gerçek MSSQL, SMTP, canlı rota/hava/yakıt sağlayıcısı, hosting always-on ve tüm gerçek mobil ekranlar test edilmedi. V54 SQL kontrolü kolon varlığını kapsar; tüm veri tipleri, kısıtların doğruluğu ve bütün gerçek kayıtları doğrulamaz. Eski çift yakıt kaydı varsa indeks atlanır ve raporlanır.
- Hatırlatma e-postası için SMTP ve Mail:SendReminders=true, ayrıca hosting uygulamasının çalışması gerekir. SMTP kabulü ile DB işareti arasındaki olağanüstü kesinti yine tekrar riski yaratır. Kilometre kullanıcının son girdiği bilgidir.
- Alternatif otomatik yakıt fiyatı kaynağı doğrulanmadığı için URL uydurulmadı; güncel olmayan fiyat otomatik kullanılmaz.
- CMS mevcut ana sayfa, hakkında, sosyal bağlantı, motosiklet ve rehber metinleri ile içerik CRUD'unu kapsar; tüm tasarım/görseller için sınırsız bir site oluşturucu değildir.
- Teslim: gerçek müşteri brief kanıtı, erişilebilir GitHub repo, yayınlanan URL ve 5–7 dakikalık sunum öğrenci tarafından tamamlanmalıdır. Mevcut V5.1 canlı testleri V5.4 onayı değildir.
