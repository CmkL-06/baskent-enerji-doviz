# TASIMA-IHTIYAR.ps1
# Baskentenerji doviz, telegram bot, MTT web sitesi verilerini ihtiyar klasorune tasir

$src   = "C:\Users\Administrator\Desktop\PROJELER"
$hedef = "C:\Users\Administrator\Desktop\ihtiyar"

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  IHTIYAR TASIMA ISLEMI" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Hedef klasoru olustur
if (-not (Test-Path $hedef)) {
    New-Item $hedef -ItemType Directory | Out-Null
    Write-Host "Hedef klasor olusturuldu: $hedef" -ForegroundColor Green
} else {
    Write-Host "Hedef klasor mevcut: $hedef" -ForegroundColor Gray
}

# Tasinacak ogeler
$ogeler = @(
    @{
        Kaynak = "$src\01_BASKENT-ENERJI-DOVIZ"
        Ad     = "01_BASKENT-ENERJI-DOVIZ"
        Acik   = "Baskentenerji doviz ofisi + telegram botu"
    },
    @{
        Kaynak = "$src\02_MTT-MONEYTRANSFERTURKEY"
        Ad     = "02_MTT-MONEYTRANSFERTURKEY"
        Acik   = "MoneyTransferTurkey.com web sitesi"
    },
    @{
        Kaynak = "$src\04_MCP-CLAUDE-ARACLARI\Sixth_ihtiyar"
        Ad     = "Sixth_ihtiyar"
        Acik   = "Sixth_ihtiyar MCP projesi"
    }
)

$basarili = 0
$hatali   = 0

foreach ($oge in $ogeler) {
    $kaynakYol = $oge.Kaynak
    $hedefYol  = Join-Path $hedef $oge.Ad

    Write-Host ""
    Write-Host "[ $($oge.Ad) ]" -ForegroundColor Yellow
    Write-Host "  Aciklama : $($oge.Acik)" -ForegroundColor Gray
    Write-Host "  Kaynak   : $kaynakYol" -ForegroundColor Gray
    Write-Host "  Hedef    : $hedefYol" -ForegroundColor Gray

    if (-not (Test-Path $kaynakYol)) {
        Write-Host "  UYARI: Kaynak bulunamadi, atlaniyor." -ForegroundColor Red
        $hatali++
        continue
    }

    if (Test-Path $hedefYol) {
        Write-Host "  UYARI: Hedef zaten mevcut, uzerine yaziliyor..." -ForegroundColor Yellow
        Remove-Item $hedefYol -Recurse -Force -ErrorAction SilentlyContinue
    }

    try {
        Move-Item -Path $kaynakYol -Destination $hedefYol -Force
        Write-Host "  BASARILI: Tasinma tamamlandi." -ForegroundColor Green
        $basarili++
    } catch {
        Write-Host "  HATA: $($_.Exception.Message)" -ForegroundColor Red
        $hatali++
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  SONUC: $basarili basarili, $hatali hata" -ForegroundColor $(if ($hatali -eq 0) { "Green" } else { "Yellow" })
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Hedef klasor: $hedef" -ForegroundColor White
Write-Host ""

# Kalan PROJELER yapisi
Write-Host "PROJELER'de kalan ana klasorler:" -ForegroundColor Cyan
Get-ChildItem "C:\Users\Administrator\Desktop\PROJELER" -Directory | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Gray
}

pause
