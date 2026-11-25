@echo off
setlocal
cd /d "%~dp0" || exit /b 1
where dotnet >nul 2>&1 || (echo .NET SDK bulunamadi. & exit /b 1)
if exist "%~dp0YAYIN_V54" (echo YAYIN_V54 zaten var. Once adini degistirin veya yeni klasore cikartin. & exit /b 2)
dotnet publish "%~dp0MamiMotovlog\MamiMotovlog.csproj" -c Release -o "%~dp0YAYIN_V54" || exit /b 3
if exist "%~dp0YAYIN_V54\appsettings.Production.json" del /q "%~dp0YAYIN_V54\appsettings.Production.json"
if exist "%~dp0YAYIN_V54\appsettings.Development.json" del /q "%~dp0YAYIN_V54\appsettings.Development.json"
if exist "%~dp0YAYIN_V54\App_Data" (echo Ozel App_Data bulundu. YAYIN_V54 klasorunu paylasmayin. & exit /b 4)
if exist "%~dp0YAYIN_V54\wwwroot\uploads" (echo Ozel uploads bulundu. YAYIN_V54 klasorunu paylasmayin. & exit /b 4)
echo YAYIN_V54 hazir. SQL guncelleme ve Plesk koruma adimlari icin BASLANGIC.md dosyasini okuyun.
echo Mevcut canli Production ayarini ASLA uzerine yazmayin.
exit /b 0
