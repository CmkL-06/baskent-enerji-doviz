# api.baskentenerji.com - BaskentEnerji API deploy
# Kullanim: .\deploy-api-to-canli.ps1
# Oncesinde: dotnet publish BaskentEnerji.API -c Release -r win-x64 --no-self-contained -o deploy\publish-api
# NOT: Eski Plesk sunucusu (45.84.191.180) iptal edildi. Aktif sunucu: WIN-RF5UU12C449 (159.195.55.1)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $repoRoot) { $repoRoot = "C:\inetpub\baskent-enerji-doviz" }
$source = Join-Path $repoRoot "deploy\publish-api"
$target = "C:\inetpub\wwwroot\api"
$appPoolName = "BaskentEnerji-API"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"

# 0) App Pool'u durdur (DLL kilitli olmasin)
& $appcmd stop apppool /apppool.name:$appPoolName 2>$null

# 1) Canli appsettings ve web.config yedekle (uyeler uzerine yazilmasin)
$appsettingsBackup = Join-Path $target "appsettings.json.bak_deploy"
$webConfigBackup = Join-Path $target "web.config.bak_deploy"
if (Test-Path (Join-Path $target "appsettings.json")) {
    Copy-Item (Join-Path $target "appsettings.json") $appsettingsBackup -Force
}
if (Test-Path (Join-Path $target "web.config")) {
    Copy-Item (Join-Path $target "web.config") $webConfigBackup -Force
}

# 2) Publish ciktisini canliya kopyala (appsettings ve web.config haric - sonra geri yukleriz)
Get-ChildItem $source -File | ForEach-Object {
    $destPath = Join-Path $target $_.Name
    if ($_.Name -eq "appsettings.json" -or $_.Name -eq "web.config") { return }
    Copy-Item $_.FullName $destPath -Force
}
Get-ChildItem $source -Directory | ForEach-Object {
    $destDir = Join-Path $target $_.Name
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item (Join-Path $_.FullName "*") $destDir -Recurse -Force
}

# 3) Canli appsettings ve web.config geri yukle (canli ayarlar kalsin)
if (Test-Path $appsettingsBackup) {
    Copy-Item $appsettingsBackup (Join-Path $target "appsettings.json") -Force
}
if (Test-Path $webConfigBackup) {
    Copy-Item $webConfigBackup (Join-Path $target "web.config") -Force
}

# 4) Eski API adlarından kalan deps/runtimeconfig artiklarini sil
Get-ChildItem -LiteralPath $target -File -Filter "*.API.*config*.json" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -notmatch "^BaskentEnerji\.API\.(deps|runtimeconfig)\.json$") {
        Remove-Item $_.FullName -Force
        Write-Host "Silindi: $($_.Name)"
    }
}
Get-ChildItem -LiteralPath $target -File -Filter "*.API.deps.json" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -ne "BaskentEnerji.API.deps.json") {
        Remove-Item $_.FullName -Force
        Write-Host "Silindi: $($_.Name)"
    }
}

# 5) App Pool'u baslat
& $appcmd start apppool /apppool.name:$appPoolName 2>$null
Write-Host "Deploy tamamlandi. App Pool yeniden baslatildi."
