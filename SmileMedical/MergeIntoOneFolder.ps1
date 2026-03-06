# IKI PROJEYI TEK KLASOR HALINE GETIR
# QuantaQuokka_ + Quantum Terminal -> Desktop\QuantaQuokka\SmileMedical + Quantum_Terminal
# CALISMA_PRENSIPLERI: Once -WhatIf ile calistirin, listeyi onaylayin, sonra gercek calistirin.
param([switch]$WhatIf)

$desktop = "c:\Users\CmkL-Owner\Desktop"
$sourceRoot = Join-Path $desktop "QuantaQuokka_"
$targetRoot = Join-Path $desktop "QuantaQuokka"
$smileMedical = Join-Path $targetRoot "SmileMedical"
$quantumTerminal = Join-Path $targetRoot "Quantum_Terminal"

$quantumReportPaths = @(
    (Join-Path $desktop "Quantum_Terminal_Teknik_Rapor.txt"),
    (Join-Path $desktop "Raporlar_Merkez\Quantum_Terminal_Teknik_Rapor.txt"),
    (Join-Path $desktop "PC_Arsiv_Duzenli\01_Raporlar_Analiz\Quantum_Terminal_Teknik_Rapor.txt"),
    (Join-Path $desktop "PC_Arsiv_Duzenli\01_Raporlar_Analiz\Quantum_Terminal_Teknik_Rapor_8bfa06.txt")
)

if ($WhatIf) {
    Write-Host "[WhatIf] Asagidaki islemler YAPILACAK (simdi hicbir sey degistirilmiyor):"
    Write-Host ""
    Write-Host "1. Klasor olusturulacak: $targetRoot"
    Write-Host "2. Klasor olusturulacak: $smileMedical"
    Write-Host "3. Tasinacak: $sourceRoot\* -> $smileMedical\ (tum icerik)"
    Write-Host "4. Bos kalan $sourceRoot silinecek"
    Write-Host "5. Klasor olusturulacak: $quantumTerminal"
    foreach ($p in $quantumReportPaths) {
        if (Test-Path -LiteralPath $p -PathType Leaf) {
            Write-Host "6. Tasinacak: $p -> $quantumTerminal\"
        }
    }
    Write-Host ""
    Write-Host "Onay verirseniz -WhatIf OLMADAN calistirin: .\MergeIntoOneFolder.ps1"
    exit 0
}

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    Write-Host "HATA: Kaynak bulunamadi: $sourceRoot"
    exit 1
}

Write-Host "Olusturuluyor: $targetRoot"
New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
Write-Host "Olusturuluyor: $smileMedical"
New-Item -ItemType Directory -Path $smileMedical -Force | Out-Null

Write-Host "Tasiniyor: QuantaQuokka_ icerigi -> SmileMedical\"
Get-ChildItem -Path $sourceRoot -Force -ErrorAction SilentlyContinue | ForEach-Object {
    Move-Item -LiteralPath $_.FullName -Destination $smileMedical -Force -ErrorAction Stop
    Write-Host "  Tasindi: $($_.Name)"
}

if (Test-Path -LiteralPath $sourceRoot -PathType Container) {
    $remaining = Get-ChildItem -Path $sourceRoot -Force -ErrorAction SilentlyContinue
    if (-not $remaining) {
        Remove-Item -LiteralPath $sourceRoot -Force -ErrorAction SilentlyContinue
        Write-Host "Bos klasor silindi: QuantaQuokka_"
    }
}

Write-Host "Olusturuluyor: $quantumTerminal"
New-Item -ItemType Directory -Path $quantumTerminal -Force | Out-Null
foreach ($p in $quantumReportPaths) {
    if (Test-Path -LiteralPath $p -PathType Leaf) {
        $dest = Join-Path $quantumTerminal (Split-Path -Leaf $p)
        Move-Item -LiteralPath $p -Destination $dest -Force -ErrorAction SilentlyContinue
        Write-Host "Tasindi: $(Split-Path -Leaf $p) -> Quantum_Terminal\"
    }
}

Write-Host ""
Write-Host "Bitti. Tek klasor: $targetRoot"
Write-Host "  - SmileMedical\ (eski QuantaQuokka_ icerigi)"
Write-Host "  - Quantum_Terminal\ (rapor dosyalari)"
