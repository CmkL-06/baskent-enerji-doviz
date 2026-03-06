# Quick scan: Desktop only (excluding QuantaQuokka_) - REPORT ONLY
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$indexPath = Join-Path $root "_hash_index.txt"
$reportPath = Join-Path $root "_scan_report_desktop_only.txt"

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
Get-ChildItem -Path $desktop -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    $full = $_.FullName
    if ($full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return }
    $totalScanned++
    try {
        $hash = (Get-FileHash -Path $full -Algorithm SHA256 -ErrorAction Stop).Hash
        if ($hashToRel.ContainsKey($hash)) { $matches += "${full}|${hash}|$($hashToRel[$hash])" }
    } catch {}
}
$matches | Set-Content -Path $reportPath -Encoding UTF8
Write-Host "Desktop scan: $totalScanned files, $($matches.Count) matches."
