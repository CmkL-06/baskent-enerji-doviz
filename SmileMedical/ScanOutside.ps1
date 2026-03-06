# Scan locations outside QuantaQuokka_ for files that match (same SHA256) - REPORT ONLY, no move
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$indexPath = Join-Path $root "_hash_index.txt"
$reportPath = Join-Path $root "_scan_report_matches.txt"

# Build hash -> relative path(s) from index (first path wins if duplicate hash)
$hashToRel = @{}
Get-Content -Path $indexPath -Encoding UTF8 -ErrorAction SilentlyContinue | ForEach-Object {
    $line = $_.Trim()
    if ($line -match '^([A-F0-9]+)\|(.+)$') {
        $h = $Matches[1]; $r = $Matches[2]
        if (-not $hashToRel.ContainsKey($h)) { $hashToRel[$h] = $r }
    }
}

$searchRoots = @(
    "c:\Users\CmkL-Owner\Desktop",
    "c:\Users\CmkL-Owner\Documents",
    "c:\Users\CmkL-Owner\Downloads",
    "c:\Users\CmkL-Owner\Source",
    "c:\Users\CmkL-Owner\OneDrive"
)

$matches = @()
$totalScanned = 0
$errors = @()

foreach ($searchRoot in $searchRoots) {
    if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) { continue }
    Get-ChildItem -Path $searchRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $full = $_.FullName
        # Skip anything inside QuantaQuokka_
        if ($full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return }
        # Skip our own report/index/build script
        if ($_.Name -match '^_hash_index\.txt$|^_scan_report|^BuildHashIndex\.ps1$|^ScanOutside\.ps1$') { return }
        $totalScanned++
        try {
            $hash = (Get-FileHash -Path $full -Algorithm SHA256 -ErrorAction Stop).Hash
            if ($hashToRel.ContainsKey($hash)) {
                $rel = $hashToRel[$hash]
                $matches += "${full}|${hash}|${rel}"
            }
        } catch {
            $errors += "Error: $full - $_"
        }
    }
}

$matches | Set-Content -Path $reportPath -Encoding UTF8
Write-Host "Scanned: $totalScanned | Matches (outside): $($matches.Count)"
if ($errors.Count -gt 0) {
    $errPath = Join-Path $root "_scan_errors.txt"
    $errors | Set-Content -Path $errPath -Encoding UTF8
    Write-Host "Errors: $($errors.Count) -> $errPath"
}
