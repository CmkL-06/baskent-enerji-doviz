# api.baskentenerji.com App Pool - En iyi durum ayarlari
# Kullanim: .\IIS-API-AppPool-Optimize.ps1 (yönetici PowerShell)

$ErrorActionPreference = "Stop"
$appPoolName = "api.baskentenerji.com(domain)(pool)"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"

if (-not (Test-Path $appcmd)) {
    Write-Host "Hata: appcmd bulunamadi. IIS kurulu mu?" -ForegroundColor Red
    exit 1
}

$exists = & $appcmd list apppool $appPoolName 2>$null
if (-not $exists) {
    Write-Host "Uyari: App pool '$appPoolName' bulunamadi." -ForegroundColor Yellow
    exit 1
}

& $appcmd set config -section:system.applicationHost/applicationPools "/[name='$appPoolName'].queueLength:2000" /commit:apphost
& $appcmd set config -section:system.applicationHost/applicationPools "/[name='$appPoolName'].processModel.idleTimeout:00:20:00" /commit:apphost
& $appcmd set config -section:system.applicationHost/applicationPools "/[name='$appPoolName'].recycling.periodicRestart.time:1.00:00:00" /commit:apphost
& $appcmd recycle apppool /apppool.name:$appPoolName

Write-Host "App pool optimize edildi: queueLength=2000, idleTimeout=20dk, restart=24saat" -ForegroundColor Green
