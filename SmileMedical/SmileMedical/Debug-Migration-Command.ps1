$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
$cmd = Get-Command dotnet-ef -ErrorAction SilentlyContinue
if ($cmd) {
    Write-Host "dotnet-ef found at: $($cmd.Source)"
} else {
    Write-Host "dotnet-ef NOT found in PATH."
    Write-Host "Checking default location..."
    if (Test-Path "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe") {
        Write-Host "Found at default location. Adding to PATH."
        $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
    } else {
        Write-Host "CRITICAL: dotnet-ef executable not found."
    }
}

$projectPath = "D:\QuantaQuokka\SmileMedical"
$apiPath = "$projectPath\SmileMedical.API"
$dataPath = "$projectPath\SmileMedical.Data"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\mig_out.txt"

Write-Host "Running migration..."
cmd /c "dotnet ef migrations add InitialCreateSQLite --project ""$dataPath"" --startup-project ""$apiPath"" --output-dir Migrations --verbose > ""$outFile"" 2>&1"
Write-Host "Done."
