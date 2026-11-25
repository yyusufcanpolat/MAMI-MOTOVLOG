@echo off
setlocal
cd /d "%~dp0MamiMotovlog" || exit /b 1
if not exist "MamiMotovlog.csproj" (echo Proje dosyasi eksik. & exit /b 2)
if not exist "appsettings.Development.json" (
    copy /y "appsettings.Development.example.json" "appsettings.Development.json" >nul || exit /b 2
)
set "ASPNETCORE_ENVIRONMENT=Development"
set "DOTNET_ENVIRONMENT=Development"
set "Database__Provider=Sqlite"
set "ConnectionStrings__Sqlite=Data Source=App_Data/mami.db"
set "ConnectionStrings__SqlServer="
set "Rider__Enabled=true"
set "Mail__LocalConsoleOnly=true"
set "Mail__SendReminders=false"
set "ASPNETCORE_URLS=http://127.0.0.1:5000"
set "Routing__OsrmBaseUrl=https://routing.openstreetmap.de/routed-car"
set "Routing__BackupOsrmBaseUrl=https://router.project-osrm.org"
set "Weather__Enabled=true"
echo Yerel SQLite test ortami: http://127.0.0.1:5000
echo Canli SQL veya Plesk kullanilmaz. Durdurmak icin Ctrl+C.
dotnet run --project .\MamiMotovlog.csproj --no-launch-profile
exit /b %ERRORLEVEL%
