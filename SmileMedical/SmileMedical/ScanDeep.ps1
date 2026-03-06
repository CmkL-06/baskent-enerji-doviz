# DERIN TARAMA - Kullanici profili ve diger suruculerde hash eslesmesi (rapor only, tasima yok)
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$indexPath = Join-Path $root "_hash_index.txt"
$reportPath = Join-Path $root "_scan_report_matches.txt"

$hashToRel = @{}
Get-Content -Path $indexPath -Encoding UTF8 -ErrorAction SilentlyContinue | ForEach-Object {
    $line = $_.Trim()
    if ($line -match '^([A-F0-9]+)\|(.+)$') {
        $h = $Matches[1]; $r = $Matches[2]
        if (-not $hashToRel.ContainsKey($h)) { $hashToRel[$h] = $r }
    }
}

# Derin tarama kokleri: kullanici profili + masaustu disindaki yaygin konumlar
$searchRoots = @(
    "c:\Users\CmkL-Owner\Desktop",
    "c:\Users\CmkL-Owner\Documents",
    "c:\Users\CmkL-Owner\Downloads",
    "c:\Users\CmkL-Owner\Source",
    "c:\Users\CmkL-Owner\OneDrive",
    "c:\Users\CmkL-Owner\Pictures",
    "c:\Users\CmkL-Owner\Videos",
    "c:\Users\CmkL-Owner\Favorites",
    "c:\Users\CmkL-Owner\AppData\Local\Temp",
    "c:\Users\CmkL-Owner\AppData\Roaming",
    "c:\Users\CmkL-Owner"
)

# Hiz icin atlanacak alt klasor isimleri (cok dosya icerir)
$skipDirs = @('node_modules', '.git', 'obj', 'bin', 'packages', '.vs', 'Debug', 'Release', 'x64', 'x86', 'bower_components', '.nuget', 'Cache', 'Code Cache', 'GPUCache', 'D3DSCache', 'Grpc')

$matches = @()
$totalScanned = 0
$errors = @()
$seenRoots = @{}

foreach ($searchRoot in $searchRoots) {
    if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) { continue }
    $normRoot = $searchRoot.TrimEnd('\').ToLowerInvariant()
    if ($seenRoots[$normRoot]) { continue }
    $seenRoots[$normRoot] = $true

    Get-ChildItem -Path $searchRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $full = $_.FullName
        if ($full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return }
        $skip = $false
        foreach ($d in $skipDirs) {
            if ($full.IndexOf("\$d\", [StringComparison]::OrdinalIgnoreCase) -ge 0) { $skip = $true; break }
        }
        if ($skip) { return }
        if ($_.Name -match '^_hash_index\.txt$|^_scan_report|^BuildHashIndex\.ps1$|^ScanOutside\.ps1$|^ScanDeep\.ps1$') { return }
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
Write-Host "Derin tarama bitti. Taranan: $totalScanned | Eslesen (disarida): $($matches.Count)"
if ($errors.Count -gt 0) {
    $errPath = Join-Path $root "_scan_errors.txt"
    $errors | Set-Content -Path $errPath -Encoding UTF8
    Write-Host "Hatalar: $($errors.Count) -> $errPath"
}
