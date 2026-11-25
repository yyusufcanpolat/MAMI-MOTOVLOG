# V5.3 — Kod inceleme ve test raporu

Kaynak: `MAMI_MOTOVLOG_V5_2_DERLEME_DUZELTMESI.zip` (kullanıcının başarıyla derlediği V5.2). Başka eski ZIP ile birleştirilmedi. Canlı veritabanı veya canlı site değiştirilmedi. Bu sürüm bir düzeltme adayıdır; kullanıcının bildirdiği 24 hatanın tamamını bildiğimiz veya kapattığımız iddia edilmez.

## Kodda uygulanan düzeltmeler

| Alan | Tespit / yapılan değişiklik |
|---|---|
| 1. Rota HTTP 400 | Varsayılan kimlik admin çereziydi; anonim planlayıcıda üye principal'ı View'a taşınmıyordu. İstek yoluna göre admin/üye kimliği ayrıldı; CSRF korunarak GET/POST aynı kimliğe bağlandı. Gerçek HTTP testi gerekli. |
| 2. Topluluk favorileri | Anonim GET ile üyeye özel POST arasındaki kimlik uyuşmazlığı aynı yöntemle düzeltildi. |
| 3. Rota detayları | Üye kimliği kamuya açık rota detayında da doğru seçiliyor; özel günlük ve tamamlanma tarihi ziyaretçiden saklanıyor. |
| 4. HTTP 429 | Sınıf düzeyindeki üyelik limiti kaldırıldı. Yalnızca hassas POST işlemlerinde ayrı politikalar. |
| 5. İstek limiti | Yönetici giriş/şifre ve üye giriş/kayıt/mail/reset/hesap silme limitleri ayrı. Retry-After ve Türkçe hata mesajı. |
| 6. HTTP hata yanıtları | Boş 400, 401, 403, 404, 405, 413 ve diğer hatalara açıklayıcı sayfa/JSON. |
| 7. Rota AJAX | Geçersiz oturum 401 JSON; eksik CSRF/400 ve diğer sunucu hataları için açıklayıcı metin; hata sonrası tekrar deneme. |
| 8. Rota formu | ModelState hataları JSON'da ayrıntılı; geçersiz koordinat girişi reddediliyor. |
| 9. Bakım tutarı | Boş bırakılabilir; veritabanında 0 olarak tutulur. |
| 10. Şifre onayı | Kayıt sırasında zorunlu şifre tekrarı. |
| 11. Doğrulama | Boş ve aşırı uzun doğrulama kodu reddedilir, yeniden gönderimde e-posta boşlukları kırpılır. |
| 12. Hatırlatma | Motor seçilmemişse 404 yerine anlaşılır form uyarısı. |
| 13. Ekipman | Gelecekteki satın alma tarihi engelleniyor. |
| 14. Bakım maili | Hatırlatmalar untracked okunarak hatadan sonra ChangeTracker temizlenmesinin diğer kayıtları bozma riski azaltıldı. |
| 15. Dosya yükleme | Tam başlık okuma, başarısız dosya kopyalama/kayıt sonrası temizlik. |
| 16. Özel bakım belgeleri | İndirme yanıtı; tarayıcıda aynı origin içinde doğrudan PDF açma zorunluluğu yok. |
| 17. Admin şifresi | Şifre değişikliğinde mevcut eski yönetici oturumları da geçersiz; bir kez yeniden giriş gerekir. |
| 18. Admin sıfırlama | Yerel testte üretilen sıfırlama URL'si localhost'a yöneliyor. |
| 19. Admin moderasyon | Kullanıcı paylaşım istemeden rotayı onaylama engelleniyor. |
| 20. Admin işlem geçmişi | Hata mesajıyla yönlendirilen işlemler başarılı gibi kaydedilmiyor. |
| 21. İçerik seed | Yönetici tarafından silinmiş/değiştirilmiş örnek rehberler her açılışta tekrar oluşturulmuyor. |
| 22. SQLite yükseltmesi | Eski yerel veritabanında eksik Phone sütunu sabit, eklemeli SQL ile eklenir. |
| 23. Katalog | Gecikmiş marka/model yanıtlarının seçimi bozması ve elle değiştirilen alanların eski variant ID ile üzerine yazılması azaltıldı. |
| 24. Rota ve hava | Haritada noktalar değiştikten sonra eski ağ yanıtının yeni seçime uygulanması engelleniyor; hesaplanmadan kayıt engeli. |
| 25. Rota kısa mesafe | Pozitif çok kısa mesafenin 0.0'a yuvarlanarak kaydı geçersiz yapması engelleniyor. |
| 26. Ondalık alanlar | İki basamaklı tüketim/depo değerlerinin tarayıcı step doğrulamasına takılması giderildi. |
| 27. PWA | Önbellek sürümü değişti, versiyonlu URL'ler eski önbellekten sunulmuyor. |
| 28. Kapak dosyası | Kapak değiştirme/sıfırlamada eski dosya temizliği ve DB hatasında yeni dosya temizliği. |
| 29. Yakıt yedek referansı | Bozuk yönetici JSON kaydı fiyat endpoint'ini çökertmiyor; güncelliği doğrulanmamış veri fiyat olarak uygulanmıyor. |
| 30. Güvenlik | Yerel ve canlı DataProtection uygulama adları ayrıldı; prod uygulama adı korundu. |

## Çalıştırılmış kontroller

- Beş Node.js test dosyası başarıyla çalıştı: test-map, test-picker, test-journal-v5, test-v52 (statik sözleşme), test-v53 (statik sözleşme ve AJAX yanıt davranışı).
- wwwroot altındaki JavaScript dosyalarına `node --check` uygulandı; başarılı.
- Chromium fixture testi: dosya adı, özel silme onay/vazgeçme, zorunlu alan işareti, 390px genişlikte taşma kontrolü ve mobil menü başarılı. Bu gerçek MVC sayfası testi değildir.
- C# / Razor kaynakları ve EF model/SQL scriptleri gözden geçirildi; V5.3 yeni SQL tablo/sütun gerektirmez. Salt okunur V53_CHECK_SQLSERVER.sql şema kontrolü eklendi. SQL Server üzerinde çalıştırılmadı.

## Otomatik HTTP kabul testi (yeni, henüz çalıştırılmadı)

`Tests/IntegrationTests`: derlenmiş gerçek uygulamayı geçici SQLite üzerinde başlatıp üyelik/doğrulama, 16 GET, aynı tarayıcıda admin+üye, opsiyonel form alanları, yakıt hesabı, gider, ekipman, rota multipart POST, CSRF reddi, kullanıcı izolasyonu, topluluk, parola yenileme ve silme işlemlerini sınar. Kişisel veya canlı DB'ye erişmez. Test fixture geometrisi gerçek yol tarifi değildir. Başlatma komutları `00_OTOMATIK_TEST.md` içindedir. İlk hata test koşusunu durdurur.

## Açık sınırlar

Bu çalışma ortamında .NET SDK, NuGet ağı, SQL Server ve gerçek SMTP erişimi yok. C# ve Razor derlemesi, yeni HTTP koşucusunun kendisi, EF/SQLite çalışma zamanı ve canlı davranış doğrulanamadı. Canlı V5.1 korunmalı; derleme ve otomatik testler geçmeden yükseltme yapılmamalı. Harici benzin kaynağı taze veri vermezse güvenli manuel giriş ve yönetici referansı devam eder; yeni doğrulanmış otomatik kaynak bulunmuş değildir. Hatırlatma e-postaları hosting uygulaması aktif olduğu sürece çalışır; çoklu süreç/olağanüstü SMTP kesintilerinde tam bir kez gönderim garantisi yoktur. Canlı kullanıcı veritabanı ve yüklenen dosyalar pakete dahil değildir. Hocanın yönergesi incelendi; GitHub erişilebilirliği, müşteri brief kanıtı ve canlı kabul testi öğrenci tarafından tamamlanmalıdır.
