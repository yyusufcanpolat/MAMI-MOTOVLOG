# V5.4 — Kabul testleri ve ders teslimi

Önce `00_OTOMATIK_TEST.md` üzerinden otomatik HTTP test sonucu PASS olmalı; sonra `02_YEREL_BASLAT.cmd`. V5.1 canlı testleri ile V5.4 aday testleri birbirine karıştırılmamalıdır.

## V5.4 yerel / canlı kabul kontrolü

| Senaryo | Beklenen sonuç |
|---|---|
| Marka/model geçerli, versiyon/notlar boş | Motosiklet kaydolur, HTTP 405 olmaz |
| Marka/model tamamen boş | Türkçe uyarı; kayıt yok; diğer alanlar korunur |
| Bakım açıklaması, sonraki tarih/km boş | Geçerli zorunlu alanlarla kaydolur |
| Yakıt: 1000 km, 10 L, 500 TL tam; 1200 km, 8 L, 400 TL tam | Gerçek tüketim 4,00 L/100 km, harcama 900 TL; garaj, motor kartı ve rota planlayıcı aynı ölçümü kullanır |
| Dolum türü yanlış | Yakıt geçmişindeki Tam depo/Kısmi düğmesi ile düzelt; hesap yenilensin |
| Rota hesapla, başlık gir, başlangıç/varış adı boş, kaydet | Rota garaja kaydolur; hatada hesaplanmış rota ekranda kalır |
| 5 ara durak, alternatif ve saat/dakika | Yol sağlayıcısı desteklediğinde rota değişir; GPX ve motorin yok |
| Benzin kaynağı eski | Otomatik fiyat temizlenir; elle giriş mümkün; panelde 24 saatlik doğrulanmış referans çalışır |
| Üye kaydı ve şifre sıfırlama | HTML e-postada tıklanabilir başlık; düz metin alternatifi var; gerçek SMTP ile canlı test |
| Yağ bakımı 7 gün / 500 km kala | Mail:SendReminders=true ve uygulama açıkken saatlik kontrol; 1 gün / 100 km ve gecikme aşaması; aynı aşama tekrar edilmez (normal çalışma) |
| Bakım yenilendi veya kullanıcı bildirimleri kapattı | Eski bakım uyarıları / e-posta durur |
| Yönetici /yonetim/rider/uyeler, motosikletler, rotalar | Gerçek listeler, arama, sayfalama ve silme; üye şifreleri görünmez |
| Üye engelle / engeli kaldır | Engellenen üyenin giriş ve aktif oturumu engellenir; kaldırınca tekrar giriş |
| Hesap silme | Bağlı garaj, yakıt, bakım, gider, ekipman, favori ve rota silinir; diğer hesaplara dokunulmaz |
| Motor silme | Bağlı yakıt/bakım/gider ve hatırlatmalar silinir; sürüşler korunur |
| Katalog CRUD ve CSV | Hatalı satırda uyarı; bağlı kayıt silinmez; silinen seed yeniden oluşmaz |
| Yönetici site başlık, açıklama, kapak, Hakkımda, sosyal bağlantılar | Ana sayfa/ilgili sayfada değişiklik; boş bırakıldığında varsayılan içerik |
| Dosya yükleme | Özel dosya alanı, dosya adı, tür/boyut kontrolü; yalnız kendi özel dosyanı açabilirsin |
| Silme | Site içi onay penceresi; vazgeçince hiçbir istek yok; onayda POST + CSRF |
| A/B hesap izolasyonu | B hesabı A'nın motor, belge, fotoğraf, rota ID'sini açamaz veya değiştiremez |
| Mobil | 375–430px ekran, yatay taşma olmamalı; admin tablo kendi alanında kayar |
| İçerik CRUD / iletişim | Video, rota, albüm, ekipman ekle-listele-düzenle-sil; mesaj panelde görünür |

**Bu senaryolar kabul ölçütleridir, tamamı geçmiş test sonucu değildir.** Gerçek MSSQL ve SMTP üzerinden canlı test, yayın sonrası zorunludur.

## BİLP2135 yönergesi — teslim listesi

- Gerçek müşteri Muhammet; orijinal yazışma veya görüşme kanıtı ayrıca eklenmeli. Özet dosya kanıt yerine geçmez; kişisel yazışmaları izinsiz public repo'ya koyma.
- ASP.NET Core MVC + SQL Server, dinamik backend ve en az bir CRUD akışı mevcut kaynakta bulunur. Razor form doğrulaması, responsive arayüz, oturum ve rol ayrımı mevcut.
- Canlı URL: `https://mamimotovlog.com`. V5.1 kullanıcı tarafından canlı test edildi; V5.4 yükseltmesi henüz yapılmadı.
- Kaynak GitHub'a yüklenmeli ve değerlendiren kişinin erişimine açık olmalı. GitHub adresi gerçek repository oluşturulduktan sonra teslim edilmeli. Hazır olmayan link uydurulmadı.
- `appsettings.Production.json`, `App_Data`, `wwwroot/uploads`, log, publish, parola ve anahtarlar repo dışı kalmalı. `git status` ve staged diff ile kontrol et. Örnek config gerçek parola içermez.
- Teslim: 25 Kasım 2026. Sunum: 2 Aralık 2026, 5–7 dakika. `MamiMotovlog/Documentation/SUNUM_PLANI.md` teknik anlatım. Öğrencinin kodu açıklaması ve AI kullanımını belirtmesi yönergede zorunludur.
- Müşteri geri bildirimi varsa gerçek haliyle eklenebilir; yapay veya uydurma kanıt üretme.
