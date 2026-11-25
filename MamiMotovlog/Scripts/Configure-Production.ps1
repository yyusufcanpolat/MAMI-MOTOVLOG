# Windows PowerShell / PowerShell 7
# Creates an appsettings.Production.json ONLY in the local publish output.
param([string]$PublishDirectory = ".\publish")
$ErrorActionPreference = "Stop"
$target = [System.IO.Path]::GetFullPath($PublishDirectory)
if (-not (Test-Path (Join-Path $target "MamiMotovlog.dll"))) {
  throw "Publish klasorunde MamiMotovlog.dll bulunamadi. Once dotnet publish -c Release -o publish calistirin."
}
function Get-SecretString([string]$Label) {
  $secure = Read-Host $Label -AsSecureString
  $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
  try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
  finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}
$server = Read-Host "Plesk MSSQL 2019 sunucu (ornek: 89.252.181.138\MSSQLSERVER2019)"
$database = Read-Host "Plesk veritabani tam adi"
$username = Read-Host "Plesk SQL kullanici adi"
$dbPassword = Get-SecretString "SQL kullanici sifresi"
$adminPassword = Get-SecretString "Panel icin en az 12 karakterlik YENI guclu sifre"
if ([string]::IsNullOrWhiteSpace($server) -or [string]::IsNullOrWhiteSpace($database) -or [string]::IsNullOrWhiteSpace($username)) { throw "Sunucu, veritabani ve SQL kullanici adi bos olamaz." }
if ($adminPassword.Length -lt 12) { throw "Yonetici sifresi en az 12 karakter olmali." }
# Standart DbConnectionStringBuilder ile güvenli kaçış (özellikle ; içeren şifreler).
$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder.Add('Data Source', [string]$server)
$builder.Add('Initial Catalog', [string]$database)
$builder.Add('User ID', [string]$username)
$builder.Add('Password', [string]$dbPassword)
$builder.Add('Integrated Security', 'False')
# SQL sunucusunun TLS politikası hosting firmasıyla doğrulanmalıdır.
# Mevcut canlı appsettings.Production.json dosyanızı ASLA bu betikle üzerine yazmayın.
$builder.Add('Encrypt', 'True')
$builder.Add('TrustServerCertificate', 'False')
$builder.Add('Connect Timeout', '15')
$configuration = @{
    Database = @{ Provider = "SqlServer" }
    ConnectionStrings = @{ SqlServer = $builder.ConnectionString }
    Admin = @{ Password = $adminPassword }
}
$file = Join-Path $target "appsettings.Production.json"
$configuration | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $file -Encoding utf8
Write-Host "Uretildi: $file"
Write-Host "Bu dosyayi GitHub'a gondermeyin veya ekran goruntusu olarak paylasmayin. Yalnizca sunucuya yukleyin."
