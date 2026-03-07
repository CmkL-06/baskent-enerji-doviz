$apiProj = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\SmileMedical.API.csproj"
$dataPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.Data"
$apiPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.API"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\mig_fix_out.txt"

Write-Host "Adding SQLite package to API project..."
dotnet add "$apiProj" package Microsoft.EntityFrameworkCore.Sqlite

Write-Host "Retrying Migration..."
cmd /c "dotnet ef migrations add InitialCreateSQLite --project ""$dataPath"" --startup-project ""$apiPath"" --output-dir Migrations --verbose > ""$outFile"" 2>&1"
cmd /c "dotnet ef database update --project ""$dataPath"" --startup-project ""$apiPath"" --verbose >> ""$outFile"" 2>&1"

if (Test-Path "$apiPath\SmileMedical.db") {
    Write-Host "SUCCESS: DB Created."
} else {
    Write-Host "FAILURE: DB Missing."
}
