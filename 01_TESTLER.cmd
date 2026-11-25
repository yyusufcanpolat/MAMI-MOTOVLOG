@echo off
setlocal
cd /d "%~dp0" || exit /b 1
set "ROOT=%~dp0"
set "APP=%ROOT%MamiMotovlog"
set "TESTS=%ROOT%Tests"
echo === MAMI MOTOVLOG JOURNAL V5.4 / TEST ===
for %%F in ("%APP%\MamiMotovlog.csproj" "%TESTS%\FuelTests\FuelTests.csproj" "%TESTS%\FuelTests\Program.cs" "%TESTS%\WebChecks\test-map.mjs" "%TESTS%\WebChecks\test-picker.mjs" "%TESTS%\WebChecks\test-journal-v5.mjs") do (
    if not exist "%%~F" (
        echo EKSIK DOSYA: %%~F
        echo ZIP dosyasini tamamen yeni bir klasore cikartin. Eski dosyalari kullanmayin.
        exit /b 2
    )
)
where dotnet >nul 2>&1 || (echo .NET SDK bulunamadi. & exit /b 3)
set "NODE="
if exist "%ProgramFiles%\nodejs\node.exe" set "NODE=%ProgramFiles%\nodejs\node.exe"
if not defined NODE for /f "delims=" %%N in ('where node.exe 2^>nul') do if not defined NODE set "NODE=%%N"
if not defined NODE (echo Node.js bulunamadi. & exit /b 3)
pushd "%APP%" || exit /b 1
dotnet restore "%APP%\MamiMotovlog.csproj" || goto :failed
dotnet build "%APP%\MamiMotovlog.csproj" -c Release --no-restore || goto :failed
dotnet run --project "%TESTS%\FuelTests\FuelTests.csproj" -c Release || goto :failed
for /r "%APP%\wwwroot" %%J in (*.js) do (
    "%NODE%" --check "%%~fJ" || goto :failed
)
for %%T in (test-map.mjs test-picker.mjs test-journal-v5.mjs test-v52.mjs test-v53.mjs test-v54.mjs) do (
    "%NODE%" "%TESTS%\WebChecks\%%T" || goto :failed
)
popd
dotnet run --project "%TESTS%\IntegrationTests\IntegrationTests.csproj" -c Release -- "%APP%\bin\Release\net10.0\MamiMotovlog.dll" || exit /b 1
echo PASS: .NET derleme, yakit ve JavaScript testleri.
echo NOT: Gercek SQL, SMTP, harici API ve mobil tarayici testleri ayrica gerekli.
exit /b 0
:failed
popd
echo HATA: Test veya derleme basarisiz. Yukaridaki ilk hatayi inceleyin.
exit /b 1
