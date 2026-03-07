$projectPath = "D:\QuantaQuokka\SmileMedical"
$apiPath = "$projectPath\SmileMedical.API"
$dataPath = "$projectPath\SmileMedical.Data"

Write-Host "Running EF Migrations for D: Drive (SQLite)..."

# Create Migration
Write-Host "Adding Migration: InitialCreateSQLite..."
dotnet ef migrations add InitialCreateSQLite --project "$dataPath" --startup-project "$apiPath" --output-dir Migrations

# Update Database
Write-Host "Updating Database..."
dotnet ef database update --project "$dataPath" --startup-project "$apiPath"

if (Test-Path "$apiPath\SmileMedical.db") {
    Write-Host "SUCCESS: SmileMedical.db created." -ForegroundColor Green
} else {
    Write-Host "WARNING: Database file not found." -ForegroundColor Yellow
}
