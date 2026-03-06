$program = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\Program.cs"
$csproj = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\SmileMedical.API.csproj"

Write-Host "--- Program.cs Imports ---"
Get-Content $program -TotalCount 30

Write-Host "`n--- CSProj Content ---"
Get-Content $csproj
