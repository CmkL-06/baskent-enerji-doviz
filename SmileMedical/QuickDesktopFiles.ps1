$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$lines = Get-Content (Join-Path $root "_hash_index.txt") -Encoding UTF8
$hashToRel = @{}
foreach ($l in $lines) {
    if ($l -match '^([A-F0-9]+)\|(.+)$') { $hashToRel[$Matches[1]] = $Matches[2] }
}
$matches = @()
Get-ChildItem "c:\Users\CmkL-Owner\Desktop" -File -ErrorAction SilentlyContinue | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256 -ErrorAction SilentlyContinue).Hash
    if ($hash -and $hashToRel[$hash]) { $matches += $_.FullName }
}
$reportPath = Join-Path $root "_scan_report_desktop_root.txt"
$matches | Set-Content $reportPath -Encoding UTF8
"Count: " + $matches.Count
