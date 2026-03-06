# API ve (isteğe bağlı) web testi. Localhost: -LocalHost
param([switch]$LocalHost)

if ($LocalHost) {
  $base = "http://localhost:5093"
  Write-Host "=== API (localhost:5093) ===" -ForegroundColor Cyan
} else {
  $base = "https://api.baskentenerji.com"
  Write-Host "=== API (api.baskentenerji.com) ===" -ForegroundColor Cyan
}

@("/health", "/", "/api/v1/Exchange", "/api/v1/Exchange/currency", "/api/v1/Exchange/dashboard", "/api/v1/Exchange/vaults") | ForEach-Object {
  try {
    $r = Invoke-WebRequest -Uri ($base + $_) -UseBasicParsing -TimeoutSec 15
    Write-Host "  $_ -> $($r.StatusCode)" -ForegroundColor Green
  } catch {
    $code = $_.Exception.Response.StatusCode.value__
    if (-not $code) { $code = "Hata" }
    Write-Host "  $_ -> $code" -ForegroundColor Red
  }
}

if (-not $LocalHost) {
  Write-Host "`n=== Web (baskentenerji.com) ===" -ForegroundColor Cyan
  try {
    $r = Invoke-WebRequest -Uri "https://baskentenerji.com" -UseBasicParsing -TimeoutSec 15
    Write-Host "  https://baskentenerji.com -> $($r.StatusCode)" -ForegroundColor Green
  } catch {
    $code = $_.Exception.Response.StatusCode.value__
    if (-not $code) { $code = "Hata" }
    Write-Host "  https://baskentenerji.com -> $code" -ForegroundColor Red
  }
}

Write-Host "`nBitti." -ForegroundColor Gray
