param (
    [string]$Source,
    [string]$Destination
)

Write-Host "Verifying integrity between:"
Write-Host "Source: $Source"
Write-Host "Dest:   $Destination"
Write-Host "----------------------------------------"

if (-not (Test-Path $Source)) { Write-Error "Source not found!"; exit 1 }
if (-not (Test-Path $Destination)) { Write-Host "Destination not found (yet)."; exit 0 }

$srcStats = Get-ChildItem -Path $Source -Recurse -File -Force | Measure-Object -Property Length -Sum
$dstStats = Get-ChildItem -Path $Destination -Recurse -File -Force | Measure-Object -Property Length -Sum

Write-Host "Source Files: $($srcStats.Count)"
Write-Host "Dest Files:   $($dstStats.Count)"
Write-Host "Source Size:  $($srcStats.Sum / 1MB) MB"
Write-Host "Dest Size:    $($dstStats.Sum / 1MB) MB"

if ($srcStats.Count -eq $dstStats.Count -and $srcStats.Sum -eq $dstStats.Sum) {
    Write-Host "SUCCESS: Folders are identical." -ForegroundColor Green
} else {
    Write-Host "WARNING: Discrepancy detected." -ForegroundColor Yellow
}
