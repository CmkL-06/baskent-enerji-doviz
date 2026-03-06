$program = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\Program.cs"
$apiProj = "D:\QuantaQuokka\SmileMedical\SmileMedical.API\SmileMedical.API.csproj"
$dataPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.Data"
$apiPath = "D:\QuantaQuokka\SmileMedical\SmileMedical.API"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\mig_fix_2_out.txt"

# 1. Add using statement if missing
$content = Get-Content $program
if ($content -notcontains "using Microsoft.EntityFrameworkCore;") {
    Write-Host "Adding 'using Microsoft.EntityFrameworkCore;' to Program.cs..."
    $newContent = "using Microsoft.EntityFrameworkCore;" + "`r`n" + ($content -join "`r`n")
    Set-Content $program $newContent
} else {
    Write-Host "'using Microsoft.EntityFrameworkCore;' already present."
}

# 2. Add package again to be sure
Write-Host "Ensuring package reference..."
dotnet add "$apiProj" package Microsoft.EntityFrameworkCore.Sqlite

# 3. Retry Migration
Write-Host "Retrying Migration..."
cmd /c "dotnet ef migrations add InitialCreateSQLite --project ""$dataPath"" --startup-project ""$apiPath"" --output-dir Migrations --verbose > ""$outFile"" 2>&1"
cmd /c "dotnet ef database update --project ""$dataPath"" --startup-project ""$apiPath"" --verbose >> ""$outFile"" 2>&1"

if (Test-Path "$apiPath\SmileMedical.db") {
    Write-Host "SUCCESS: DB Created."
} else {
    Write-Host "FAILURE: DB Missing."
}
