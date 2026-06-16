# API KURTARMA + YENIDEN DEPLOY
# AppPool durmussa baslatir, sonra deploy-full calistirir.
# Yonetici PowerShell'de calistir!

$ErrorActionPreference = "Continue"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
$appPoolName = "BaskentEnerji-API"

Write-Host "=== API KURTARMA ===" -ForegroundColor Cyan

# 1) AppPool durumunu kontrol et
$state = & $appcmd list apppool /apppool.name:$appPoolName /text:state 2>$null
Write-Host "AppPool durumu: $state" -ForegroundColor Yellow

if ($state -ne "Started") {
    Write-Host "AppPool baslatiliyor..." -ForegroundColor Yellow
    & $appcmd start apppool /apppool.name:$appPoolName 2>$null
    Start-Sleep -Seconds 2
    $state = & $appcmd list apppool /apppool.name:$appPoolName /text:state 2>$null
    if ($state -eq "Started") {
        Write-Host "OK: AppPool baslatildi." -ForegroundColor Green
    } else {
        Write-Host "!! AppPool hala durdu: $state - deploy yine de deneniyor" -ForegroundColor Yellow
    }
} else {
    Write-Host "OK: AppPool zaten calisiyor." -ForegroundColor Green
}

# 2) Tam deploy calistir (build + publish + IIS'e kopyala)
Write-Host ""
Write-Host "Deploy baslatiliyor..." -ForegroundColor Cyan
& "$PSScriptRoot\deploy-full.ps1"

if ($LASTEXITCODE -eq 0) {
    Write-Host "DEPLOY TAMAMLANDI" -ForegroundColor Green
    Write-Host "Health check: https://api.baskentenerji.com/health" -ForegroundColor Cyan
} else {
    Write-Host "Deploy basarisiz. Yukaridaki hatalari inceleyin." -ForegroundColor Red
}

Write-Host ""
Read-Host "Enter ile cikin"
