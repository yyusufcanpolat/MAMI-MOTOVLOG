# BİLP2135 — Sunum Planı (5–7 dakika)

**Öğrenci:** Yusuf Can Polat · 25BİLP0030  
**Gerçek müşteri:** Muhammet  
**Proje:** MAMI MOTOVLOG · mamimotovlog.com  
**Teslim:** 25 Kasım 2026 · **Sunum:** 2 Aralık 2026

**0:00–0:50 — İhtiyaç / gerçek müşteri**  
Muhammet'in e-posta brief'ini kanıt olarak aç. İsteklerini özetle: YouTube, rota, fotoğraf albümleri, CFMOTO 450SR, ekipman ve kendi yönetim paneli.

**0:50–1:40 — Tasarım**  
Gerçek müşteri fotoğrafları, 450SR esinli çizgi amblem, yeşil-gri tonlu sade editoryal görsel dil ve responsive yapıyı anlat. Mobil menüyü göster.

**1:40–2:30 — Site turu**  
Ana sayfa, Hakkımda, 450SR, ekipman, galeri. Albümde fotoğraf büyütme. Örnek rotanın **örnek** olarak işaretlendiğini belirt.

**2:30–4:30 — Teknik demo**  
`/yonetim/giris` şifreyle giriş. Rota ekle → listele → düzenle → sil. Videoda geçerli YouTube URL doğrulama; fotoğraf yükleme. Controller / Model / View ve EF Core / MSSQL ilişkisini kendi cümlelerinle açıkla.

**4:30–5:25 — Form / doğrulama**  
İletişim formunu boş gönderince doğrulama, ardından geçerli test mesajı → panelde görüntüle. İletişim formunda e-posta ve telefon zorunlu; SMTP üyelik akışında kullanılır.

**5:25–6:20 — Yayın / değerlendirme**  
Gerçek çalışan HTTPS URL'yi ve GitHub kaynak kodunu göster. Önceden sorunsuz kontrol edilmişse göster; çalışmıyorsa olmuş gibi anlatma. AI kullanımını (kod taslağı/üslup/tasarım/inceleme) açıkça belirt, çünkü yönergede bunu açıklamak şart.

**6:20–7:00 — Katma değer**  
Müşteri içeriklerini, kullanıcıları, kayıtlı motosikletleri ve katalog kayıtlarını panelden yönetebilir (V5.4 kabul testinden sonra). Video, rota ve albüm bağlantıları dinamik, fotoğraflar büyütülebilir, iletişim mesajları SQL veritabanında toplanır.

## Sorulara kısa cevaplar

- **MVC:** Model veri yapısı, View Razor arayüzü, Controller istek/iş akışıdır.
- **CRUD:** Create / Read / Update / Delete; örneğin gezi içeriği ekle, listele, düzenle, sil.
- **SQL Server neden?:** Video, gezi, albüm, fotoğraf ve mesajlar kalıcı ve ilişkili saklanır.
- **Şifre?:** Tek yönetici, Identity PasswordHasher ve cookie auth; gizli bilgi GitHub'da tutulmaz.
- **İlk video neden boş?:** Kanal gerçek, ancak müşteriden doğrulanmış video URL'si gelmedi. Üçüncü taraf videosunu müşteriye atfetmiyoruz.
- **Örnek rota?:** Sunum/test için örnek metin; gerçek sürüş iddiası yok.
