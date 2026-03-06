$loc1 = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\SmileMedical.db"
$loc2 = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\SmileMedical.db"
$loc3 = "D:\QuantaQuokka\SmileMedical\SmileMedical.Data\SmileMedical.db"
$loc4 = "D:\QuantaQuokka\SmileMedical\SmileMedical.db"

Write-Host "Checking locations..."
if (Test-Path $loc1) { Write-Host "FOUND at API: $loc1" }
if (Test-Path $loc2) { Write-Host "FOUND at CWD: $loc2" }
if (Test-Path $loc3) { Write-Host "FOUND at Data: $loc3" }
if (Test-Path $loc4) { Write-Host "FOUND at Root: $loc4" }

Write-Host "Done."
