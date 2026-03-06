# IIS ayarlari kontrol - baskentenerji.com ve api.baskentenerji.com
# Kullanim: .\IIS-Kontrol.ps1

$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
Write-Host "=== IIS Kontrol ===" -ForegroundColor Cyan

# baskentenerji.com vdir
$vdir = & $appcmd list vdir "baskentenerji.com/" /text:physicalPath 2>$null
if ($vdir) {
    Write-Host "baskentenerji.com fiziksel yol: $vdir" -ForegroundColor $(if ($vdir -like "*\httpdocs") { "Green" } else { "Yellow" })
    if ($vdir -notlike "*\httpdocs") { Write-Host "  Beklenen: ...\httpdocs (httpdocs\public degil)" -ForegroundColor Gray }
} else {
    Write-Host "baskentenerji.com vdir okunamadi" -ForegroundColor Red
}

# api.baskentenerji.com vdir
$apiVdir = & $appcmd list vdir "api.baskentenerji.com/" /text:physicalPath 2>$null
if ($apiVdir) {
    Write-Host "api.baskentenerji.com fiziksel yol: $apiVdir" -ForegroundColor Green
} else {
    Write-Host "api.baskentenerji.com vdir okunamadi" -ForegroundColor Red
}

# Site durumlari
$sites = & $appcmd list site 2>$null
foreach ($name in "baskentenerji.com", "api.baskentenerji.com") {
    $line = $sites | Select-String -Pattern "SITE `"$name`""
    if ($line -match "state:(\w+)") {
        $state = $matches[1]
        Write-Host "${name} site durumu: $state" -ForegroundColor $(if ($state -eq "Started") { "Green" } else { "Yellow" })
    }
}

# URL Rewrite - web.config'te rewrite varsa modul gerekli
$webConfig = "C:\Inetpub\vhosts\baskentenerji.com\httpdocs\web.config"
if (Test-Path $webConfig) {
    $hasRewrite = Select-String -Path $webConfig -Pattern "<rewrite>" -Quiet
    if ($hasRewrite) {
        Write-Host "web.config'te <rewrite> var - URL Rewrite modulu yuklu olmali." -ForegroundColor Gray
    }
}

Write-Host "`nBitti. Detay: BASKENT_PROJE\IIS_AYARLARI_YAPILANDIRMA.md" -ForegroundColor Gray
