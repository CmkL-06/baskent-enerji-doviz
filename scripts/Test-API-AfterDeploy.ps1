# Deploy sonrasi canli API kontrolu: /health ve istege bagli login
# Kullanim: .\Test-API-AfterDeploy.ps1  veya  .\Test-API-AfterDeploy.ps1 -Login
param([switch]$Login)

$base = "https://api.baskentenerji.com"
$ok = $true

Write-Host "=== Deploy sonrasi API testi ===" -ForegroundColor Cyan
Write-Host "Hedef: $base`n" -ForegroundColor Gray

# 1. Health endpoint
try {
    $r = Invoke-RestMethod -Uri "$base/health" -Method Get -TimeoutSec 15
    Write-Host "  /health -> 200 OK" -ForegroundColor Green
    if ($r.timestamp) { Write-Host "    timestamp: $($r.timestamp)" -ForegroundColor Gray }
} catch {
    Write-Host "  /health -> HATA" -ForegroundColor Red
    Write-Host "    $($_.Exception.Message)" -ForegroundColor Red
    $ok = $false
}

# 2. Istege bagli login (ornek kullanici; sifre dosyada tutulmaz)
if ($Login) {
    $body = @{ Mail = "ihtiyar"; Password = "owner1" } | ConvertTo-Json
    try {
        $r = Invoke-RestMethod -Uri "$base/api/v1/User/login" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 15
        if ($r.token) {
            Write-Host "  /api/v1/User/login -> 200 OK (token alindi)" -ForegroundColor Green
        } else {
            Write-Host "  /api/v1/User/login -> 200 ama token yok" -ForegroundColor Yellow
        }
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "  /api/v1/User/login -> $code" -ForegroundColor Red
        $ok = $false
    }
}

Write-Host ""
if ($ok) { Write-Host "Sonuc: API ayakta." -ForegroundColor Green } else { Write-Host "Sonuc: Bazi kontroller basarisiz." -ForegroundColor Red }
