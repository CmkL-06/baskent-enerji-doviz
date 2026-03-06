# TAŞIMA - Rapor dosyasindaki eslesmeleri QuantaQuokka_ icinde hedef yola tasir.
# KAYNAK DOSYA ASLA SILINMEZ. Hedef zaten varsa sadece atlanir.
# Deneme: .\MoveMatches.ps1 -WhatIf
# Farkli rapor: .\MoveMatches.ps1 -ReportFile "_scan_report_desktop_only.txt"
param([switch]$WhatIf, [string]$ReportFile = "_scan_report_matches.txt")

$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$reportPath = Join-Path $root $ReportFile
if (-not (Test-Path -LiteralPath $reportPath)) {
    Write-Host "Rapor dosyasi bulunamadi: $reportPath"
    Write-Host "Once ScanOutside.ps1 veya ScanDesktopOnly.ps1 calistirip _scan_report_matches.txt olusturun."
    exit 1
}

$lines = Get-Content -Path $reportPath -Encoding UTF8 -ErrorAction SilentlyContinue
$moved = 0
$skipped = 0
$missing = 0
$missingList = @()
$errors = @()

# Sadece kaynak/config dosyalarini tasi; obj, bin, .vs (derleme/onbellek) atlanir - yanlis bos dosya eslesmeleri engellenir
$skipTargetSegments = @('\obj\', '\bin\', '\.vs\')
foreach ($line in $lines) {
    $line = $line.Trim()
    if ($line -notmatch '^(.+)\|([A-F0-9]+)\|(.+)$') { continue }
    $externalPath = $Matches[1]
    $expectedHash = $Matches[2]
    $relativePath = $Matches[3]
    $relLower = $relativePath.ToLowerInvariant().Replace('/', '\')
    $skip = $false
    foreach ($seg in $skipTargetSegments) {
        if ($relLower.IndexOf($seg) -ge 0) { $skip = $true; break }
    }
    if ($skip) { continue }
    $targetPath = Join-Path $root $relativePath

    if (-not (Test-Path -LiteralPath $externalPath -PathType Leaf)) {
        $missing++
        $missingList += $externalPath
        continue
    }
    $currentHash = (Get-FileHash -Path $externalPath -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
    if ($currentHash -ne $expectedHash) {
        $errors += "Hash degisti, tasinmadi: $externalPath"
        continue
    }
    if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
        $targetHash = (Get-FileHash -Path $targetPath -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
        if ($targetHash -eq $expectedHash) {
            $skipped++; Write-Host "Zaten mevcut (kaynak SILINMEDI, atlandi): $relativePath"
            continue
        }
    }
    $targetDir = Split-Path -Parent $targetPath
    if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) {
        if (-not $WhatIf) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
    }
    if ($WhatIf) {
        Write-Host "[WhatIf] Tasinacak: $externalPath -> $targetPath"
        $moved++
    } else {
        try {
            Move-Item -LiteralPath $externalPath -Destination $targetPath -Force -ErrorAction Stop
            Write-Host "Tasindi: $relativePath"
            $moved++
        } catch {
            $errors += "Tasima hatasi: $externalPath - $_"
        }
    }
}

Write-Host "--- Sonuc: Tasinan=$moved, Atlanan(zaten mevcut)=$skipped, Kaynak yok=$missing, Hata=$($errors.Count)"
if ($missingList.Count -gt 0) {
    $missPath = Join-Path $root "_move_skipped_missing.txt"
    $missingList | Set-Content -Path $missPath -Encoding UTF8
    Write-Host "Kaynak bulunamayan (rapor guncel degil): $missPath"
}
if ($errors.Count -gt 0) {
    $errPath = Join-Path $root "_move_errors.txt"
    $errors | Set-Content -Path $errPath -Encoding UTF8
    Write-Host "Hatalar: $errPath"
}
