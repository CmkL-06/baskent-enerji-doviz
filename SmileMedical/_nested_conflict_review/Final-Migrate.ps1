$dataPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.Data"
$apiPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.API"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\final_mig_out.txt"

Write-Host "Restoring packages..."
dotnet restore "$apiPath\SmileMedical.API.csproj"

Write-Host "Adding Migration..."
cmd /c "dotnet ef migrations add InitialCreateSQLite --project ""$dataPath"" --startup-project ""$apiPath"" --output-dir Migrations --verbose > ""$outFile"" 2>&1"

Write-Host "Updating Database..."
cmd /c "dotnet ef database update --project ""$dataPath"" --startup-project ""$apiPath"" --verbose >> ""$outFile"" 2>&1"

if (Test-Path "$apiPath\SmileMedical.db") {
    Write-Host "SUCCESS: DB Created."
} else {
    Write-Host "FAILURE: DB Missing."
}
