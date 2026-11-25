# Fonksiyon Test Planı — MAMI MOTOVLOG

Bu senaryolar **yapılacak canlı/yerel testlerin listesi**dir. Bu ortamda .NET 10 SDK ve SQL Server çalıştırılamadığı için sonuçlar otomatik olarak "başarılı" kabul edilmez. Test sırasında her satırın durumunu siz doldurun.

| # | Senaryo | Beklenen sonuç | Sonuç |
|---|---|---|---|
| 01 | Ana sayfayı masaüstü Chrome'da aç | Gerçek hero resmi ve güncel 3 içerik kartı görünür | □ |
| 02 | 390 px mobil görünüm | Menü açılır, fotoğraflar taşmaz | □ |
| 03 | `/yonetim/giris` yanlış şifre | Şifre hatalı, login olamaz | □ |
| 04 | 5 kez yanlış şifre | 15 dakika kilitlenir | □ |
| 05 | Doğru şifre | Yönetim paneli açılır | □ |
| 06 | Yeni gezi yazısı (yayında) | `/rotalar` listesinde görünür | □ |
| 07 | Gezi yazısı güncelle | Yeni bilgi web sitesinde görünür | □ |
| 08 | Gezi yazısı sil | Listeden kalkar, ilişkiler tutarlı kalır | □ |
| 09 | Geçerli YouTube video linki ekle | Video kartı + video sayfası oluşur | □ |
| 10 | Geçersiz video linki ekle | Form doğrulama hatası gösterir | □ |
| 11 | Yeni albüm oluştur ve JPG yükle | Yeni albüm / fotoğraf görünür | □ |
| 12 | SVG/PHP dosyasını görsel diye yükle | Sunucu kabul etmez | □ |
| 13 | Albümde fotoğraf büyüt/ok tuşları/ESC | Büyütme açılır, geçiş ve kapanma çalışır | □ |
| 14 | İletişim boş form gönder | Alan hataları görünür | □ |
| 15 | Geçerli mesaj gönder | Mesaj panelde görüntülenir ve okundu yapılır | □ |
| 16 | Şifre değiştirme | Mevcut şifre gerekir; sonra yeni şifreyle giriş | □ |
| 17 | Yetkisiz `/yonetim` isteği | Login sayfasına yönlendirilir | □ |
| 18 | `/sitemap.xml` | XML URL listesi oluşur, admin dahil edilmez | □ |
| 19 | Plesk siteyi yeniden başlat | DB kayıtları ve fotoğraf dosyaları korunur | □ |
| 20 | `https://mamimotovlog.com` | Geçerli SSL, Plesk welcome sayfası yerine site | □ |

**Kullanılan test verilerini** demo sonunda silebilirsiniz. Canlı siteyi test etmeden başarılı test raporu düzenlemeyin.
