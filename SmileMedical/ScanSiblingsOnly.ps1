# Scan only direct siblings of QuantaQuokka_ on Desktop (one level), then one level of subdirs - fast
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$indexPath = Join-Path $root "_hash_index.txt"
$reportPath = Join-Path $root "_scan_report_siblings.txt"

$hashToRel = @{}
Get-Content -Path $indexPath -Encoding UTF8 -ErrorAction SilentlyContinue | ForEach-Object {
    $line = $_.Trim()
    if ($line -match '^([A-F0-9]+)\|(.+)$') {
        $h = $Matches[1]; $r = $Matches[2]
        if (-not $hashToRel.ContainsKey($h)) { $hashToRel[$h] = $r }
    }
}

$matches = @()
$totalScanned = 0
$desktop = "c:\Users\CmkL-Owner\Desktop"
# Only desktop direct children (siblings of QuantaQuokka_) and one level down each
Get-ChildItem -Path $desktop -Directory -ErrorAction SilentlyContinue | Where-Object { $_.FullName -ne $root } | ForEach-Object {
    Get-ChildItem -Path $_.FullName -Recurse -File -Depth 2 -ErrorAction SilentlyContinue | ForEach-Object {
        $full = $_.FullName
        $totalScanned++
        try {
            $hash = (Get-FileHash -Path $full -Algorithm SHA256 -ErrorAction Stop).Hash
            if ($hashToRel.ContainsKey($hash)) { $matches += "${full}|${hash}|$($hashToRel[$hash])" }
        } catch {}
    }
}
# Also desktop-level files (not in any folder)
Get-ChildItem -Path $desktop -File -ErrorAction SilentlyContinue | ForEach-Object {
    $totalScanned++
    try {
        $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256 -ErrorAction Stop).Hash
        if ($hashToRel.ContainsKey($hash)) { $matches += "$($_.FullName)|${hash}|$($hashToRel[$hash])" }
    } catch {}
}
$matches | Set-Content -Path $reportPath -Encoding UTF8
Write-Host "Siblings scan: $totalScanned files, $($matches.Count) matches."
