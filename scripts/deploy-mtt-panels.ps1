# deploy-mtt-panels.ps1
# MoneyTransferTurkey panel dosyalarini IIS'e deploy eder
# tg.moneytransferturkey.com -> C:\inetpub\moneytransfer-tg\

param(
    [string]$SourceDir  = "C:\inetpub\baskent-enerji-doviz\mtt-panels",
    [string]$TargetDir  = "C:\inetpub\moneytransfer-tg",
    [string]$SiteName   = "MoneyTransfer-TG"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  MTT Panel Deploy Scripti" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# 1. Hedef klasoru olustur
Write-Host "[1/5] Hedef klasor olusturuluyor: $TargetDir" -ForegroundColor Yellow
if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    Write-Host "      Klasor olusturuldu." -ForegroundColor Green
} else {
    Write-Host "      Klasor zaten mevcut." -ForegroundColor Green
}

# 2. Panel HTML dosyalarini kopyala
Write-Host "[2/5] Panel dosyalari kopyalaniyor..." -ForegroundColor Yellow
$files = @("login.html", "dealer.html", "operator.html", "admin.html")
foreach ($f in $files) {
    $src = Join-Path $SourceDir $f
    if (Test-Path $src) {
        Copy-Item $src -Destination $TargetDir -Force
        Write-Host "      Kopyalandi: $f" -ForegroundColor Green
    } else {
        Write-Host "      UYARI: $f bulunamadi!" -ForegroundColor Red
    }
}

# 3. web.config olustur (URL routing icin)
Write-Host "[3/5] web.config olusturuluyor..." -ForegroundColor Yellow
$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>

    <!-- MIME types -->
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".woff" />
      <mimeMap fileExtension=".woff" mimeType="font/woff" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".woff2" mimeType="font/woff2" />
    </staticContent>

    <!-- Default document -->
    <defaultDocument>
      <files>
        <clear />
        <add value="login.html" />
      </files>
    </defaultDocument>

    <!-- URL Rewrite: /login -> login.html, /dealer -> dealer.html, etc. -->
    <rewrite>
      <rules>
        <rule name="Login" stopProcessing="true">
          <match url="^login$" />
          <action type="Rewrite" url="login.html" />
        </rule>
        <rule name="Dealer" stopProcessing="true">
          <match url="^dealer$" />
          <action type="Rewrite" url="dealer.html" />
        </rule>
        <rule name="Operator" stopProcessing="true">
          <match url="^operator$" />
          <action type="Rewrite" url="operator.html" />
        </rule>
        <rule name="Admin" stopProcessing="true">
          <match url="^admin$" />
          <action type="Rewrite" url="admin.html" />
        </rule>
        <!-- API proxy: /api/* -> BaskentEnerji API (gelecekte MTT API endpoint olacak) -->
        <!-- Simdilik API yok; 404 dondurecek - backend kurulduktan sonra guncellenecek -->
      </rules>
    </rewrite>

    <!-- Guvenlik headerlari -->
    <httpProtocol>
      <customHeaders>
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-XSS-Protection" value="1; mode=block" />
        <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
      </customHeaders>
    </httpProtocol>

  </system.webServer>
</configuration>
'@
$webConfig | Out-File -FilePath (Join-Path $TargetDir "web.config") -Encoding UTF8
Write-Host "      web.config olusturuldu." -ForegroundColor Green

# 4. IIS site fiziksel yolunu guncelle
Write-Host "[4/5] IIS site fiziksel yolu guncelleniyor: $SiteName -> $TargetDir" -ForegroundColor Yellow
Import-Module WebAdministration -ErrorAction SilentlyContinue

$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if ($null -eq $site) {
    Write-Host "      UYARI: '$SiteName' IIS sitesi bulunamadi!" -ForegroundColor Red
    Write-Host "      Mevcut siteler:" -ForegroundColor Yellow
    Get-Website | Format-Table Name, PhysicalPath, State -AutoSize
} else {
    $oldPath = $site.PhysicalPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $TargetDir
    Write-Host "      Eski yol: $oldPath" -ForegroundColor Gray
    Write-Host "      Yeni yol: $TargetDir" -ForegroundColor Green

    # Siteyi yeniden baslat
    Write-Host "[5/5] Site yeniden baslatiliyor..." -ForegroundColor Yellow
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Start-Website -Name $SiteName
    Write-Host "      Site baslatildi." -ForegroundColor Green
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Deploy tamamlandi!" -ForegroundColor Green
Write-Host "  URL: https://tg.moneytransferturkey.com" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Panel URL'leri:" -ForegroundColor White
Write-Host "    Login    : /login" -ForegroundColor Gray
Write-Host "    Bayi     : /dealer" -ForegroundColor Gray
Write-Host "    Operator : /operator" -ForegroundColor Gray
Write-Host "    Admin    : /admin" -ForegroundColor Gray
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

pause
