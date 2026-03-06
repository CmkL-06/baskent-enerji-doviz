$projectPath = "D:\QuantaQuokka\SmileMedical"
$apiPath = "$projectPath\SmileMedical.API"
$dataPath = "$projectPath\SmileMedical.Data"
$logFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\BuildLog.txt"

Start-Transcript -Path $logFile -Append

Write-Host "1. Building Project..."
dotnet build "$apiPath\SmileMedical.API.csproj"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Success." -ForegroundColor Green
    
    Write-Host "2. Installing dotnet-ef (just in case)..."
    dotnet tool install --global dotnet-ef
    
    Write-Host "3. Retrying Migrations..."
    dotnet ef migrations add InitialCreateSQLite --project "$dataPath" --startup-project "$apiPath" --output-dir Migrations --verbose
    
    Write-Host "4. Updating Database..."
    dotnet ef database update --project "$dataPath" --startup-project "$apiPath" --verbose
} else {
    Write-Host "Build Failed." -ForegroundColor Red
}

Stop-Transcript
if (Test-Path "$apiPath\SmileMedical.db") { Write-Host "DB Exists." } else { Write-Host "DB Missing." }
