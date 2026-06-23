# deploy-mtt-panel-giris.ps1
# 1. C:\inetpub\mtt-panel-login\ klasorune panel-giris.html deploy eder
# 2. IIS'te baskentenerji.com sitesine /mtt-panel virtual application ekler
# 3. tg.moneytransferturkey.com panellerini gunceller

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$PanelGirisKaynak = "C:\inetpub\baskent-enerji-doviz\mtt-panels\panel-giris.html"
$PanelGirisHedef  = "C:\inetpub\mtt-panel-login"
$TgKaynak         = "C:\inetpub\baskent-enerji-doviz\mtt-panels"
$TgHedef          = "C:\inetpub\moneytransfer-tg"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  MTT Panel-Giris Deploy" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Giris klasorunu olustur ve panel-giris.html kopyala
Write-Host "[1/5] mtt-panel-login klasoru hazirlaniyor..." -ForegroundColor Yellow
if (-not (Test-Path $PanelGirisHedef)) {
    New-Item $PanelGirisHedef -ItemType Directory | Out-Null
    Write-Host "      Klasor olusturuldu." -ForegroundColor Green
} else {
    Write-Host "      Klasor mevcut." -ForegroundColor Gray
}

Copy-Item $PanelGirisKaynak -Destination "$PanelGirisHedef\index.html" -Force
Write-Host "      panel-giris.html -> index.html kopyalandi OK" -ForegroundColor Green

# web.config
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <defaultDocument>
      <files><clear /><add value="index.html" /></files>
    </defaultDocument>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
    <httpProtocol>
      <customHeaders>
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="X-Content-Type-Options" value="nosniff" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
'@ | Set-Content "$PanelGirisHedef\web.config" -Encoding UTF8
Write-Host "      web.config olusturuldu OK" -ForegroundColor Green

# 2. tg panellerini guncelle (token handling guncellemesi)
Write-Host ""
Write-Host "[2/5] TG panelleri guncelleniyor..." -ForegroundColor Yellow
$panelDosyalar = @("operator.html", "dealer.html", "admin.html", "login.html")
foreach ($f in $panelDosyalar) {
    $src = Join-Path $TgKaynak $f
    if (Test-Path $src) {
        Copy-Item $src -Destination $TgHedef -Force
        Write-Host "      $f guncellendi OK" -ForegroundColor Green
    } else {
        Write-Host "      $f bulunamadi, atlaniyor" -ForegroundColor Yellow
    }
}

# 3. IIS modulu
Write-Host ""
Write-Host "[3/5] IIS WebAdministration yukleniyor..." -ForegroundColor Yellow
Import-Module WebAdministration -ErrorAction Stop
Write-Host "      Yuklendi OK" -ForegroundColor Green

# 4. baskentenerji.com site adini bul
Write-Host ""
Write-Host "[4/5] baskentenerji.com IIS sitesi aranıyor..." -ForegroundColor Yellow
$beSite = Get-Website | Where-Object {
    $_.Bindings.Collection | Where-Object { $_.bindingInformation -like "*baskentenerji*" }
} | Select-Object -First 1

if ($null -eq $beSite) {
    Write-Host "      UYARI: baskentenerji.com IIS sitesi bulunamadi!" -ForegroundColor Red
    Write-Host "      Mevcut siteler:" -ForegroundColor Yellow
    Get-Website | Format-Table Name, PhysicalPath, State -AutoSize
    Write-Host ""
    Write-Host "      Manuel olarak eklenebilir:" -ForegroundColor Cyan
    Write-Host "      IIS Manager -> baskentenerji.com site -> Add Application" -ForegroundColor Cyan
    Write-Host "      Alias: mtt-panel, Path: $PanelGirisHedef" -ForegroundColor Cyan
} else {
    Write-Host "      Site bulundu: $($beSite.Name)" -ForegroundColor Green

    # /mtt-panel virtual application mevcut mu?
    $appPath = "IIS:\Sites\$($beSite.Name)\mtt-panel"
    $existingApp = Get-WebApplication -Site $beSite.Name -Name "mtt-panel" -ErrorAction SilentlyContinue

    if ($null -eq $existingApp) {
        New-WebApplication -Site $beSite.Name -Name "mtt-panel" `
            -PhysicalPath $PanelGirisHedef `
            -ApplicationPool $beSite.ApplicationPool
        Write-Host "      /mtt-panel virtual application olusturuldu OK" -ForegroundColor Green
    } else {
        Set-ItemProperty "$appPath" -Name physicalPath -Value $PanelGirisHedef
        Write-Host "      /mtt-panel zaten mevcut, guncellendi OK" -ForegroundColor Green
    }
}

# 5. TG sitesini yeniden baslat
Write-Host ""
Write-Host "[5/5] MoneyTransfer-TG sitesi yeniden baslatiliyor..." -ForegroundColor Yellow
$tgSite = Get-Website -Name "MoneyTransfer-TG" -ErrorAction SilentlyContinue
if ($tgSite) {
    Stop-Website -Name "MoneyTransfer-TG" -ErrorAction SilentlyContinue
    Start-Website -Name "MoneyTransfer-TG"
    Write-Host "      Baslatildi OK" -ForegroundColor Green
} else {
    Write-Host "      MoneyTransfer-TG sitesi bulunamadi (deploy-mtt-panels.ps1 ile deploy gerekli)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Tamamlandi!" -ForegroundColor Green
Write-Host ""
Write-Host "  Login URL  : https://baskentenerji.com/mtt-panel" -ForegroundColor White
Write-Host "  Operator   : https://tg.moneytransferturkey.com/operator" -ForegroundColor Gray
Write-Host "  Dealer     : https://tg.moneytransferturkey.com/dealer" -ForegroundColor Gray
Write-Host "  Admin      : https://tg.moneytransferturkey.com/admin" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

pause
