# api.baskentenerji.com - AnasiTAS-Deniz API deploy
# Kullanim: .\deploy-api-to-canli.ps1
# Oncesinde: dotnet publish ... -o publish-api (zaten yapildi)

$ErrorActionPreference = "Stop"
$source = "C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-api"
$target = "C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com"
$appPoolName = "api.baskentenerji.com(domain)(pool)"
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

# 4) Sadece eski SmileMedical artiklarini sil (mevcut API'nin deps/runtimeconfig silinmez)
@(
    "SmileMedical.API.deps.json",
    "SmileMedical.API.runtimeconfig.json"
) | ForEach-Object {
    $f = Join-Path $target $_
    if (Test-Path $f) {
        Remove-Item $f -Force
        Write-Host "Silindi: $_"
    }
}

# 5) App Pool'u baslat
& $appcmd start apppool /apppool.name:$appPoolName 2>$null
Write-Host "Deploy tamamlandi. App Pool yeniden baslatildi."
