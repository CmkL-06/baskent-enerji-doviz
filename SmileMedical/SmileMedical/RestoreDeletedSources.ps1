# SILINEN KAYNAK DOSYALARI - SADECE LİSTE (KOPYA YOK)
# Proje kurali: ASLA KOPYA OLUSTURULMAZ. Bu script dis yola dosya yazmaz;
# sadece -WhatIf ile "geri getirilebilecek" dosyalari listeler.
# Deneme: .\RestoreDeletedSources.ps1 -WhatIf
param([switch]$WhatIf)

$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$reportPath = Join-Path $root "_scan_report_matches.txt"
if (-not (Test-Path -LiteralPath $reportPath)) {
    Write-Host "Rapor bulunamadi: $reportPath"
    exit 1
}

$lines = Get-Content -Path $reportPath -Encoding UTF8 -ErrorAction SilentlyContinue
$restored = 0
$skipped = 0
$skippedNoCopy = 0
$errors = @()

foreach ($line in $lines) {
    $line = $line.Trim()
    if ($line -notmatch '^(.+)\|([A-F0-9]+)\|(.+)$') { continue }
    $externalPath = $Matches[1]
    $expectedHash = $Matches[2]
    $relativePath = $Matches[3]
    $relLower = $relativePath.ToLowerInvariant().Replace('/', '\')
    $skip = $false
    foreach ($seg in @('\obj\', '\bin\', '\.vs\')) {
        if ($relLower.IndexOf($seg) -ge 0) { $skip = $true; break }
    }
    if ($skip) { continue }
    $sourceInProject = Join-Path $root $relativePath

    # Sadece kaynak (dis yol) YOKSA geri getir - silinmis demektir
    if (Test-Path -LiteralPath $externalPath -PathType Leaf) {
        $skipped++
        continue
    }
    if (-not (Test-Path -LiteralPath $sourceInProject -PathType Leaf)) {
        $errors += "Projede yok: $relativePath"
        continue
    }
    $hash = (Get-FileHash -Path $sourceInProject -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
    if ($hash -ne $expectedHash) {
        $errors += "Proje dosyasi degismis: $relativePath"
        continue
    }

    $externalDir = Split-Path -Parent $externalPath
    if (-not (Test-Path -LiteralPath $externalDir -PathType Container)) {
        if (-not $WhatIf) {
            New-Item -ItemType Directory -Path $externalDir -Force | Out-Null
        }
    }
    if ($WhatIf) {
        Write-Host "[WhatIf] Geri getirilebilir (kopya kullanilmaz): $externalPath"
        $restored++
    } else {
        Write-Host "Atlandi (kural: kopya yok): $externalPath"
        $skippedNoCopy++
    }
}

Write-Host "--- Sonuc: Listelenen=$restored, Atlanan(zaten var)=$skipped, Kopya yok kuraliyla atlandi=$skippedNoCopy, Hata=$($errors.Count)"
if ($errors.Count -gt 0) {
    $errPath = Join-Path $root "_restore_errors.txt"
    $errors | Set-Content -Path $errPath -Encoding UTF8
    Write-Host "Hatalar: $errPath"
}
