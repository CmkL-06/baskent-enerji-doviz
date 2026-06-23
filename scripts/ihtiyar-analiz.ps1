# ihtiyar-analiz.ps1
# Desktop\ihtiyar klasörünü tara, canlı sistemle karşılaştır, rapor üret

$ihtiyarRoot = "C:\Users\Administrator\Desktop\ihtiyar"
$canliRoot   = "C:\inetpub"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ihtiyar Klasörü Analiz Raporu" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ---- 1. GENEL YAPI ----
Write-Host "[1] GENEL YAPI" -ForegroundColor Yellow
Get-ChildItem $ihtiyarRoot -Directory | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -Recurse -ErrorAction SilentlyContinue
    $dosya = ($alt | Where-Object { -not $_.PSIsContainer }).Count
    $mb = [math]::Round(($alt | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum / 1MB, 1)
    Write-Host "  $($_.Name) — $dosya dosya, $mb MB" -ForegroundColor Gray
}

# ---- 2. 01_BASKENT-ENERJI-DOVIZ ----
Write-Host ""
Write-Host "[2] 01_BASKENT-ENERJI-DOVIZ — Alt Klasörler" -ForegroundColor Yellow
$be = "$ihtiyarRoot\01_BASKENT-ENERJI-DOVIZ"
Get-ChildItem $be -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -Recurse -ErrorAction SilentlyContinue
    $dosya = ($alt | Where-Object { -not $_.PSIsContainer }).Count
    $mb = [math]::Round(($alt | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum / 1MB, 1)
    Write-Host "  $($_.Name) — $dosya dosya, $mb MB" -ForegroundColor Gray
}

# ---- 3. 02_MTT-MONEYTRANSFERTURKEY ----
Write-Host ""
Write-Host "[3] 02_MTT-MONEYTRANSFERTURKEY — Alt Klasörler" -ForegroundColor Yellow
$mtt = "$ihtiyarRoot\02_MTT-MONEYTRANSFERTURKEY"
Get-ChildItem $mtt -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -Recurse -ErrorAction SilentlyContinue
    $dosya = ($alt | Where-Object { -not $_.PSIsContainer }).Count
    $mb = [math]::Round(($alt | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum / 1MB, 1)
    Write-Host "  $($_.Name) — $dosya dosya, $mb MB" -ForegroundColor Gray
}

# ---- 4. 04_MCP-CLAUDE-ARACLARI ----
Write-Host ""
Write-Host "[4] 04_MCP-CLAUDE-ARACLARI — Alt Klasörler" -ForegroundColor Yellow
$mcp = "$ihtiyarRoot\04_MCP-CLAUDE-ARACLARI"
Get-ChildItem $mcp -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -Recurse -ErrorAction SilentlyContinue
    $dosya = ($alt | Where-Object { -not $_.PSIsContainer }).Count
    $mb = [math]::Round(($alt | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum / 1MB, 1)
    Write-Host "  $($_.Name) — $dosya dosya, $mb MB" -ForegroundColor Gray
}

# ---- 5. CANLI SİSTEM DURUMU ----
Write-Host ""
Write-Host "[5] CANLI SİSTEM — C:\inetpub" -ForegroundColor Yellow
Get-ChildItem $canliRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $alt = Get-ChildItem $_.FullName -Recurse -ErrorAction SilentlyContinue
    $dosya = ($alt | Where-Object { -not $_.PSIsContainer }).Count
    $mb = [math]::Round(($alt | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum / 1MB, 1)
    Write-Host "  $($_.Name) — $dosya dosya, $mb MB" -ForegroundColor Gray
}

# ---- 6. MTT API vs ihtiyar ----
Write-Host ""
Write-Host "[6] MTT API DURUM KONTROLÜ" -ForegroundColor Yellow
$mttApi = "C:\inetpub\moneytransfer-api"
if (Test-Path $mttApi) {
    $files = Get-ChildItem $mttApi -Recurse | Where-Object { -not $_.PSIsContainer }
    $mb = [math]::Round(($files | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
    Write-Host "  moneytransfer-api — $($files.Count) dosya, $mb MB" -ForegroundColor Green
    Write-Host "  Son değişiklik: $((Get-Item $mttApi).LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  moneytransfer-api YOK!" -ForegroundColor Red
}

$mttTg = "C:\inetpub\moneytransfer-tg"
if (Test-Path $mttTg) {
    $files = Get-ChildItem $mttTg -Recurse | Where-Object { -not $_.PSIsContainer }
    Write-Host "  moneytransfer-tg — $($files.Count) dosya" -ForegroundColor Green
    # Hangi html dosyaları var?
    Get-ChildItem $mttTg -Filter "*.html" | ForEach-Object {
        Write-Host "    $($_.Name) — $($_.LastWriteTime)" -ForegroundColor DarkGray
    }
} else {
    Write-Host "  moneytransfer-tg YOK!" -ForegroundColor Red
}

# ---- 7. IIS Site Listesi ----
Write-Host ""
Write-Host "[7] IIS SİTELERİ" -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    Get-Website | Format-Table Name, State, PhysicalPath -AutoSize
} catch {
    Write-Host "  IIS WebAdministration yüklenemedi" -ForegroundColor Red
}

# ---- 8. Son .ps1 dosyalar (ihtiyar'da) ----
Write-Host ""
Write-Host "[8] ihtiyar'daki PowerShell Scriptleri" -ForegroundColor Yellow
Get-ChildItem $ihtiyarRoot -Recurse -Filter "*.ps1" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 20 | ForEach-Object {
    Write-Host "  $($_.LastWriteTime.ToString('dd.MM.yy HH:mm'))  $($_.FullName.Replace($ihtiyarRoot,''))" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Analiz Tamamlandı" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
pause
