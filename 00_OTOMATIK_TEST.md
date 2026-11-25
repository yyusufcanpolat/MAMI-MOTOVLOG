# V5.4 otomatik kabul testi (kişisel bilgisayar)

ZIP'i yeni klasöre çıkarın. PowerShell'i MamiMotovlog ve Tests klasörlerinin yan yana bulunduğu ANA klasörde açın. Aşağıdaki bloğu tek seferde çalıştırın; CMD veya güvenlik ayarı değişikliği gerekmez. .NET 10 SDK, NuGet erişimi, JS testleri için Node.js gerekir.

```powershell
dotnet build .\MamiMotovlog\MamiMotovlog.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Derleme başarısız" }
dotnet run --project .\Tests\FuelTests\FuelTests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Yakıt testi başarısız" }
if (Get-Command node -ErrorAction SilentlyContinue) {
    Get-ChildItem .\Tests\WebChecks\*.mjs | ForEach-Object { node $_.FullName; if ($LASTEXITCODE -ne 0) { throw "JavaScript testi başarısız" } }
} else { Write-Warning "Node.js yok; JavaScript testleri atlandı" }
dotnet run --project .\Tests\IntegrationTests\IntegrationTests.csproj -c Release -- .\MamiMotovlog\bin\Release\net10.0\MamiMotovlog.dll
if ($LASTEXITCODE -ne 0) { throw "HTTP kabul testi başarısız" }
```

Gerçek MVC uygulaması rastgele localhost portunda ve geçici SQLite veritabanlarıyla başlatılır; üyelik, e-posta konsol bağlantısı, iki oturum, site ayarları, yakıt, form hatası, rota kaydı, CSRF, kullanıcılar arası erişim ve silme otomatik kontrol edilir. Canlı SMTP, SQL Server, dış harita/fiyat servisi kullanılmaz. Gerçek uygulama ve C# test koşucusu bu ortamda .NET SDK olmadığı için henüz çalıştırılmadı. İlk FAIL satırını saklayın; PASS olmadan canlıya yükseltmeyin.
