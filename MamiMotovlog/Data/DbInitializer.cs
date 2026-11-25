using MamiMotovlog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MamiMotovlog.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider provider, IConfiguration config, ILogger logger)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SiteDbContext>();
        await db.Database.EnsureCreatedAsync(); // İlk kurulum; mevcut şemayı değiştirmez.
        // Canlı veritabanında Messages tablosuna yeni Phone alanını veri kaybetmeden ekle.
        // Tek başına EnsureCreated yeni sütunları mevcut tablolara eklemez.
        if (db.Database.IsSqlServer())
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.Messages', N'U') IS NOT NULL
                  AND COL_LENGTH(N'dbo.Messages', N'Phone') IS NULL
                BEGIN
                    ALTER TABLE dbo.Messages ADD Phone nvarchar(11) NOT NULL
                        CONSTRAINT DF_Messages_Phone DEFAULT N'';
                END
                """);
        }
        if (db.Database.IsSqlite())
        {
            // SQLite has no ADD COLUMN IF NOT EXISTS. Use fixed SQL statements.
            var messageColumns = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Messages')").ToListAsync();
            if (!messageColumns.Contains("Phone"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Messages ADD COLUMN Phone TEXT NOT NULL DEFAULT ''");
            var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Settings')").ToListAsync();
            if (!names.Contains("HeroTitle"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN HeroTitle TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("HeroDescription"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN HeroDescription TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("HeroImage"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN HeroImage TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("AboutText"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN AboutText TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("MotorcycleTitle"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN MotorcycleTitle TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("MotorcycleText"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN MotorcycleText TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("GuideTitle"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN GuideTitle TEXT NOT NULL DEFAULT ''");
            if (!names.Contains("GuideText"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Settings ADD COLUMN GuideText TEXT NOT NULL DEFAULT ''");
        }
        if (db.Database.IsSqlServer() && await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN COL_LENGTH(N'dbo.Settings',N'MotorcycleTitle') IS NOT NULL AND COL_LENGTH(N'dbo.Settings',N'MotorcycleText') IS NOT NULL AND COL_LENGTH(N'dbo.Settings',N'GuideTitle') IS NOT NULL AND COL_LENGTH(N'dbo.Settings',N'GuideText') IS NOT NULL THEN 1 ELSE 0 END AS [Value]").SingleAsync() != 1)
            throw new InvalidOperationException("V5.4 içerik sütunları eksik. Önce Database/V52_UPDATE_SQLSERVER.sql ve V54_UPDATE_SQLSERVER.sql dosyalarını uygulayın.");
        var isFirstRun = !await db.Settings.AnyAsync();
        if (isFirstRun)
        {
            db.Settings.Add(new SiteSettings
            {
                InstagramUrl = "https://www.instagram.com/mami500rvm/",
                YouTubeChannelUrl = "https://www.youtube.com/@mami500rvm"
            });
        }
        if (isFirstRun && !await db.Gear.AnyAsync())
        {
            db.Gear.AddRange(
                new Gear { Category = "KASK", Name = "AGV K1 S", SortOrder = 1, IsPublished = true,
                    Summary = "Sportif tasarım. Geniş görüş alanı. ECE 22.06 onaylı bir yol arkadaşı.",
                    Review = "AGV K1 S, kullandığım kasklardan biri. Arka spoyleri ve keskin hatlarıyla 450SR'nin sportif görünümüne yakışıyor. Geniş vizörü, ayarlanabilir hava girişleri ve ECE 22.06 onayı dikkat çekiyor. Benim için görünüş kadar görüş alanı, kafaya oturuş ve rüzgâr sesi de önemli. Otoyol hızlarında ses konforu kişiye ve sürüş koşullarına göre değişebilir.",
                    ExternalUrl = "https://www.agv.com/im/en/k1-s-agv-e2206--2118394017087.html" },
                new Gear { Category = "KAMERA", Name = "DJI Osmo Action 5 Pro", SortOrder = 2, IsPublished = true,
                    Summary = "Sürüşün ritmini görüntüye taşıyan aksiyon kameram.",
                    Review = "Motovloglarımı DJI Osmo Action 5 Pro ile çekiyorum. Virajın yaklaşmasını, yolun açılmasını ve manzaranın ortaya çıkmasını izleyici de hissetsin istiyorum. RockSteady 3.0 görüntü sabitleme, sürüşteki sarsıntıları azaltmaya yardımcı oluyor. Kamera, 4K çözünürlükte 120 fps'ye kadar kayıt sunuyor.",
                    ExternalUrl = "https://www.dji.com/osmo-action-5-pro/specs" }
            );
        }
        // Bu içerik gerçek bir sürüşün kaydı değil, okul sunumu ve müşteri değerlendirmesi için taslaktır.
        // Gerçek gezi bilgisi Muhammet tarafından teyit edildiğinde panelden düzenlenebilir veya silinebilir.
        if (isFirstRun && !await db.Rides.AnyAsync())
        {
            db.Rides.Add(new Ride
            {
                Title = "Örnek rota: İstanbul'dan Şile ve Ağva'ya",
                Summary = "Örnek gezi planı • İstanbul'un kalabalığından Karadeniz kıyılarına uzanan bir motosiklet rotası.",
                StartPoint = "İstanbul (Anadolu Yakası)",
                EndPoint = "Ağva / İstanbul",
                Stops = "Şile merkez, Şile sahili, Ağva",
                // Başlangıç noktasına ve seçilen yola göre mesafe değişir; doğrulanmadan sayı verilmez.
                DistanceKm = null,
                MapUrl = "https://www.google.com/maps/dir/?api=1&origin=Istanbul%2C%20Turkey&destination=Agva%2C%20Istanbul%2C%20Turkey&waypoints=Sile%2C%20Istanbul%2C%20Turkey&travelmode=driving",
                Story = "BU YAZI BİR ÖRNEK ROTA TASLAĞIDIR. Muhammet'in bu sürüşü gerçekten yaptığı iddia edilmez. Gerçek yolculuk bilgileri ve o geziye ait fotoğraflar geldiğinde güncellenecektir.\n\n"
                    + "YOL FİKRİ\nİstanbul Anadolu Yakası'ndan yola çıkıp Şile yoluna bağlanan, oradan sahil havasını takip ederek Ağva'ya uzanan bir gün rotası. Şehir geride kaldıkça çevre daha yeşil bir hâl alır.\n\n"
                    + "ÖNERİLEN DURAKLAR\nİlk mola Şile merkez ve sahil çevresi. Kısa bir dinlenmenin ardından Ağva yönüne devam edilerek Göksu Nehri çevresinde gezi tamamlanabilir.\n\n"
                    + "MOTOSİKLET NOTLARI\nCFMOTO 450SR ile böyle bir rotanın en keyifli tarafı, şehir dışındaki manzara değişimini hissetmek ve güvenli mola noktalarında fotoğraf çekebilmek. Yol ve hava koşulları sürüşten önce kontrol edilmelidir.\n\n"
                    + "MESAFE / HARİTA\nKesin kilometre başlangıç adresine ve tercih edilen yollara göre değişir. Harita bağlantısı, örnek güzergâhı açar; gerçek GPS sürüş kaydı değildir.",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        if (isFirstRun && !await db.Albums.AnyAsync())
        {
            var album = new Album { Title = "Yolun içinden", Description = "Motosiklet sürüşlerimden ve yol üzerindeki duraklardan seçtiğim ilk kareler.", IsPublished = true, CreatedAt = DateTime.UtcNow };
            string[] photos = ["sunset-450sr.webp", "muhammet-450sr.webp", "rider-450sr.webp", "city-ride.webp", "two-riders.webp", "three-bikes.webp"];
            string[] captions = ["Gün batımında 450SR", "Motosikletin yanında bir an", "Yoldan sürüş karesi", "Hareket hâlinde", "İki motosiklet, ortak bir yol", "Birlikte çıkılan sürüş"];
            for (var i = 0; i < photos.Length; i++)
                album.Photos.Add(new AlbumPhoto { ImagePath = "/images/" + photos[i], Caption = captions[i], SortOrder = i });
            db.Albums.Add(album);
        }
        // Yalnızca ilk kurulumda örnek rehberleri ekle; yönetici silerse tekrar oluşturma.
        // Bunlar gerçek sürüş iddiası taşımayan, yönetici tarafından değiştirilebilir rehber yazılarıdır.
        var routeGuides = new List<Ride>
        {
        new Ride { Title = @"Rota rehberi: Şile’den Ağva’ya: Karadeniz’e açılan gün", Summary = @"Şehirden uzaklaşmak için haftalarca izin almaya gerek yok. Şile, Ağva ve Göksu çevresinde molalı bir Karadeniz günü planı.", StartPoint = @"İstanbul Anadolu Yakası", EndPoint = @"Ağva", Stops = @"Şile merkez • Şile Feneri çevresi • Ağva • Göksu Nehri", Story = @"ÖNERİ ROTA / EDITORYAL REHBER — Bu rota Muhammet'in yaptığı doğrulanmış bir sürüş olarak sunulmaz.

BİR GÜNLÜK KAÇIŞIN ANATOMİSİ
Bu rota sabah erkenden şehrin trafik yoğunluğu artmadan hareket etmeyi sevenler için. Şile yolunda ilk dikkat edilmesi gereken şey manzara değil; değişebilen hava, yoğun hafta sonu trafiği ve bağlantı yollarındaki yüzey farklılıkları. Rota boyunca 'şu virajı bir daha alayım' telaşına girmeden sakin ilerlemek günü güzelleştirir.

ŞİLE'DE İLK MOLA
Şile merkez, deniz havasını almak ve kısa bir yürüyüş yapmak için doğal bir durak. Şile Feneri çevresi uygun koşullarda fotoğraf için güzel bir referans noktasıdır; ancak motosikleti yayaların geçişini engellemeyecek, izinli bir yerde bırakmak gerekir. Yazın kalabalık ve park yoğunluğunu hesaba katmak iyi olur.

AĞVA'YA UZANAN KISIM
Ağva yönüne geçerken yolu yalnızca kilometre olarak düşünmeyin. Orman dokusu ve kıyı coğrafyası değişir; kasaba girişlerinde hız ve dikkat ihtiyacı artar. Göksu Nehri çevresinde öğle molası, ardından kısa yürüyüş yapılabilir. Dönüşü hava karardıktan sonraya bırakmamak özellikle sonbaharda rahatlık sağlar.

YOLA ÇIKMADAN
Yağış ihtimalini, trafik yoğunluğunu, akaryakıt durumunu ve dönüş saatini kontrol edin. Sahil ve orman kesimlerinde rüzgâr, ıslak yaprak, çamur ve görüş mesafesi değişebilir. Bu plan gerçek bir sürüş günlüğü değil; harita üzerinden kişiselleştirebileceğiniz editoryal bir rota önerisidir.", IsPublished = true, DistanceKm = null, MapUrl = "https://www.google.com/maps/search/?api=1&query=A%C4%9Fva", CreatedAt = DateTime.UtcNow.AddDays(-1) },
        new Ride { Title = @"Rota rehberi: Kilyos ve Rumeli Feneri: Şehrin kuzey kıyısı", Summary = @"Karadeniz rüzgârı, eski balıkçı yerleşimleri ve İstanbul’un kuzeyinde kısa molalarla ilerleyen bir hafta sonu fikri.", StartPoint = @"İstanbul", EndPoint = @"Rumeli Feneri", Stops = @"Sarıyer • Kilyos • Garipçe • Rumeli Feneri", Story = @"ÖNERİ ROTA / EDITORYAL REHBER — Bu rota Muhammet'in yaptığı doğrulanmış bir sürüş olarak sunulmaz.

ROTANIN RUHU
Bazen uzaklara gitmeden de yeni bir yol duygusu yakalanır. İstanbul'un kuzeyinde kıyıya yaklaştıkça rüzgârın sesi, ağaçların rengi ve denize açılan boşluklar değişir. Bu öneri özellikle şehir hayatından kısa süreliğine uzaklaşmak isteyenler için düşünüldü.

KİLYOS'TA DURMAK
Kilyos çevresinde denizi görebileceğiniz alanlar bulabilirsiniz. Sahil kesimlerinde hafta sonları yoğunluk artar; motosikleti güvenli yere park edip yürüyerek keşfetmek daha sakindir. Yeme-içme molası için sabit bir mekâna bağlı kalmadan, güncel saatleri kontrol ederek ilerlemek iyi bir plan.

GARİPÇE VE FENER
Garipçe'nin dar sokaklarına yaklaşırken yerleşim alanında olduğunuzu unutmayın. Rumeli Feneri yönünde Karadeniz manzarası günün öne çıkan bölümü olabilir. Deniz kenarında fotoğraf çekerken yol kenarında ani duruşlar yapmak yerine uygun cep ve park alanlarını tercih edin.

SÜRÜŞ PLANLAMASI
Rüzgâr kıyı yollarında yan taraftan hissedilebilir. Yol yüzeyinde kum, nem ve çakıl görülebilecek kesimlere karşı hızınızı koşullara göre ayarlayın. Bu yazı yaşanmış gezi anlatısı değil, ziyaret noktalarını bir araya getiren bir rota taslağıdır.", IsPublished = true, DistanceKm = null, MapUrl = "https://www.google.com/maps/search/?api=1&query=Rumeli%20Feneri", CreatedAt = DateTime.UtcNow.AddDays(-2) },
        new Ride { Title = @"Rota rehberi: Sapanca, Maşukiye, Kartepe: Gölden ormana", Summary = @"Göl kıyısında yavaş bir başlangıç, Maşukiye’de mola ve Kartepe yönünde yükselen orman manzaraları.", StartPoint = @"Sapanca", EndPoint = @"Kartepe", Stops = @"Sapanca Gölü çevresi • Maşukiye • Kartepe etekleri", Story = @"ÖNERİ ROTA / EDITORYAL REHBER — Bu rota Muhammet'in yaptığı doğrulanmış bir sürüş olarak sunulmaz.

GÖLLE BAŞLAYAN GÜN
Sapanca'yı motosikletle gezmek isteyenler için en keyifli yaklaşım tüm durakları tek nefeste bitirmeye çalışmamak. Göl kıyısında kısa bir yürüyüş, kahve ve rota kontrolü güne iyi bir tempo verir. Park alanlarını ve yürüyüş bölgelerini önceden incelemek zaman kazandırır.

MAŞUKİYE'DE MOLA
Maşukiye, ağaçların ve su sesinin eşlik ettiği dinlenme alanlarıyla bilinir. Popüler günlerde kalabalık artacağı için durak seçimini esnek tutun. Güne erken başlamanız, hem yolun hem de dinlenme alanlarının daha rahat hissedilmesini sağlayabilir.

KARTEPE YÖNÜNDE
Yükseldikçe hava ve yol koşulları değişebilir. Mevsime bağlı sis, düşük sıcaklık, yağmur ve kış koşulları rotanın sürüşe uygunluğunu etkiler. Yukarı çıkmayı zorunluluk olarak görmeyin; koşullar uygun değilse orman eteklerinde günü tamamlamak da güzel bir seçimdir.

KÜÇÜK BİR KONTROL LİSTESİ
Frenler, lastikler, zincir bakımı ve koruyucu ekipmanı yola çıkmadan kontrol edin. Özellikle irtifa değişimi olan rotalarda birkaç katmanlı giyinmek konforu artırır. Buradaki duraklar sürüş önerisidir, doğrulanmış kişisel deneyim iddiası taşımaz.", IsPublished = true, DistanceKm = null, MapUrl = "https://www.google.com/maps/search/?api=1&query=Kartepe", CreatedAt = DateTime.UtcNow.AddDays(-3) },
        new Ride { Title = @"Rota rehberi: Bolu’nun iki yüzü: Abant ve Gölcük", Summary = @"Göl manzarasını, orman yollarını ve Bolu’nun sakin molalarını bir araya getiren doğa odaklı bir tur taslağı.", StartPoint = @"Bolu merkez", EndPoint = @"Gölcük Tabiat Parkı", Stops = @"Abant Gölü çevresi • Bolu merkez • Gölcük Tabiat Parkı", Story = @"ÖNERİ ROTA / EDITORYAL REHBER — Bu rota Muhammet'in yaptığı doğrulanmış bir sürüş olarak sunulmaz.

BOLU'DA YAVAŞLAMAK
Bu rota hızdan çok mola fikrini sevenlere hitap ediyor. Abant ve Gölcük ayrı karakterlere sahip iki doğa durağı; günün planını yaparken park giriş koşulları, hava durumu ve güncel yol durumunu birlikte düşünmek iyi olur. Tek bir günde her noktayı görme zorunluluğu yok.

ABANT BÖLÜMÜ
Abant çevresinde göl manzarası, orman ve yürüyüş alanları öne çıkar. Hava serin olduğunda üst katman ve yağmurluk taşımak faydalı olabilir. Göl çevresinde ziyaretçi yoğunluğuna, park kurallarına ve yaya önceliğine dikkat edin.

BOLU MERKEZDE ARA
Göl geçişleri arasına şehir merkezinde kısa bir öğle molası koymak iyi bir fikir. Motosikletle seyahat ederken öğle yemeği ve su ihtiyacını son ana bırakmayın. Yorgunluk, günün son etabında karar verme kalitesini etkileyebilir.

GÖLCÜK'TE BİTİRMEK
Gölcük çevresi özellikle fotoğraf ve yürüyüş için düşünülebilir. Gün batımına yakın saatleri seçiyorsanız dönüş yolunun karanlığa kalma ihtimalini hesaba katın. Yağış sonrasında nemli yüzeyler ve mevsimsel buzlanma önemli risklerdir. Bu metin editoryal rota planıdır; Muhammet'in bu güzergâhı sürdüğü anlamına gelmez.", IsPublished = true, DistanceKm = null, MapUrl = "https://www.google.com/maps/search/?api=1&query=G%C3%B6lc%C3%BCk%20Tabiat%20Park%C4%B1", CreatedAt = DateTime.UtcNow.AddDays(-4) },
        new Ride { Title = @"Rota rehberi: Assos ve Küçükkuyu: Ege’ye inen yollar", Summary = @"Tarih, zeytinlikler ve deniz: Assos çevresinden Küçükkuyu yönüne ayrıntılı, ağırdan alınacak bir kıyı rotası.", StartPoint = @"Ayvacık / Çanakkale", EndPoint = @"Küçükkuyu", Stops = @"Assos / Behramkale • Sokakağzı çevresi • Küçükkuyu", Story = @"ÖNERİ ROTA / EDITORYAL REHBER — Bu rota Muhammet'in yaptığı doğrulanmış bir sürüş olarak sunulmaz.

YOLA TARİH KARIŞINCA
Assos çevresinde motosikletle gezmek, yalnızca kıyı fotoğrafı çekmekten daha fazlasını sunar. Behramkale'nin tarihî dokusu, taş sokakları ve tepeye çıkan bölümleri yürüyerek keşfetmek için zaman ayırmayı hak eder. Dar ve kalabalık yollarda motosikleti uygun bir alana bırakmak önemli.

ASSOS'UN ÇEVRESİNDE
Antik kent ziyareti düşünüyorsanız giriş saatlerini ve güncel ziyaret koşullarını resmî kaynaklardan kontrol edin. Mevsim dışında deniz kıyısı daha sessiz olabilir; yaz aylarında ise araç ve ziyaretçi sayısı artabilir. Birkaç kilometrelik sürüşten sonra planlı molaya zaman bırakın.

KÜÇÜKKUYU'YA DEVAM
Küçükkuyu yönünde zeytinliklerin ve kıyı yerleşimlerinin ritmi değişir. Sahil hattında araç çıkışlarına, kavşaklara ve yerleşim içi hız sınırlarına dikkat etmek gerekir. Manzaraya odaklanmak yerine motosikleti güvenle durdurup fotoğraf çekin.

BU ROTA KİMİN İÇİN?
Acele etmeyi sevmeyen, motorundan inip yürüyüş yapmak isteyen ve tek bir manzara için bir süre durabilen sürücüler için. Kış ve sonbaharda yağış, ilkbaharda değişken rüzgâr; yazın ise sıcaklık ve trafik planı etkiler. Bu bir öneri rotadır, kişisel anı değildir.", IsPublished = true, DistanceKm = null, MapUrl = "https://www.google.com/maps/search/?api=1&query=K%C3%BC%C3%A7%C3%BCkkuyu", CreatedAt = DateTime.UtcNow.AddDays(-5) },
        };
        if (isFirstRun)
        foreach (var guide in routeGuides)
        {
            if (!await db.Rides.AnyAsync(r => r.Title == guide.Title)) db.Rides.Add(guide);
        }
        await db.SaveChangesAsync();
        var adminPassword = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword)) adminPassword = Environment.GetEnvironmentVariable("MAMI_ADMIN_PASSWORD");
        if (await db.AdminAccounts.AnyAsync()) return;
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("İlk yönetici oluşturulmadı. Admin:Password veya MAMI_ADMIN_PASSWORD ayarlayın.");
            return;
        }
        if (adminPassword.Length < 12)
            throw new InvalidOperationException("İlk yönetici şifresi en az 12 karakter olmalıdır.");
        var account = new AdminAccount { Key = "owner" };
        account.PasswordHash = new PasswordHasher<AdminAccount>().HashPassword(account, adminPassword);
        db.AdminAccounts.Add(account);
        await db.SaveChangesAsync();
        logger.LogInformation("Tek yönetici hesabı güvenli şifre özeti ile oluşturuldu.");
    }
}
