$target = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka"
Write-Host "Checking: $target"
if (Test-Path $target) {
    Get-ChildItem $target
    $smile = Join-Path $target "SmileMedical"
    if (Test-Path $smile) {
        Write-Host "Checking SmileMedical subfolder..."
        Get-ChildItem $smile
    }
} else {
    Write-Host "Target not found."
}
if (Test-Path "C:\QuantaQuokka") { Write-Host "WARNING: Found C:\QuantaQuokka" }
