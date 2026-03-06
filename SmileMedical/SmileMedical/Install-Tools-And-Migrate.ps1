$log = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\migration_log.txt"
Start-Transcript -Path $log -Append

Write-Host "Installing dotnet-ef globally..."
dotnet tool install --global dotnet-ef
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tool install failed, trying update..."
    dotnet tool update --global dotnet-ef
}

Write-Host "Verifying dotnet-ef..."
dotnet tool list -g

$projectPath = "D:\QuantaQuokka\SmileMedical"
$apiPath = "$projectPath\SmileMedical.API"
$dataPath = "$projectPath\SmileMedical.Data"

Write-Host "Running Migrations..."
# Ensure verbose output to see why it fails
dotnet ef migrations add InitialCreateSQLite --project "$dataPath" --startup-project "$apiPath" --output-dir Migrations --verbose
dotnet ef database update --project "$dataPath" --startup-project "$apiPath" --verbose

if (Test-Path "$apiPath\SmileMedical.db") { Write-Host "SUCCESS: DB Created." } else { Write-Host "FAILURE: DB Missing." }

Stop-Transcript
