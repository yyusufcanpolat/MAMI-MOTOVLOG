# Test the exact FULL-geometry route request used by the web app, no credentials.
$ErrorActionPreference = 'Stop'
$path = '/route/v1/driving/28.9497,41.0138;29.6133,41.1757?overview=full&geometries=geojson&steps=false'
$providers = @('https://routing.openstreetmap.de/routed-car','https://router.project-osrm.org')
foreach ($provider in $providers) {
    Write-Host "ROTA TEST: $provider"
    try {
        $response = Invoke-RestMethod -Uri ($provider + $path) -Method Get -Headers @{ 'User-Agent'='MamiMotovlog/4.0 (+https://mamimotovlog.com)';'Accept'='application/json' } -TimeoutSec 30
        if ($response.code -ne 'Ok' -or -not $response.routes -or $response.routes[0].geometry.type -ne 'LineString') {
            Write-Host 'HATA: Servis uygun GeoJSON yol verisi dondurmedi.' -ForegroundColor Red
            continue
        }
        $route = $response.routes[0]
        Write-Host ('PASS: ' + [Math]::Round($route.distance/1000, 1) + ' km, GeoJSON coordinates: ' + $route.geometry.coordinates.Count) -ForegroundColor Green
    } catch { Write-Host ('HATA: ' + $_.Exception.Message) -ForegroundColor Yellow }
}
Write-Host 'Not: Bu test servis ulasiminı olcer; ASP.NET uygulamasinda ayri test gerekir.'
