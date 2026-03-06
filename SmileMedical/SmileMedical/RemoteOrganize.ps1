# RemoteOrganize.ps1
# Organizes files in ..\..\QuantaQuokka\SmileMedical from the outside

$targetRoot = "..\..\QuantaQuokka\SmileMedical"
if (-not (Test-Path $targetRoot)) {
    $targetRoot = "..\..\QuantaQuokka"
}

Write-Host "Organizing target: $targetRoot"

$scriptsDir = Join-Path $targetRoot "_Scripts"
$docsDir = Join-Path $targetRoot "_Docs"
$logsDir = Join-Path $targetRoot "_Logs"

if (-not (Test-Path $scriptsDir)) { New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null }
if (-not (Test-Path $docsDir)) { New-Item -ItemType Directory -Path $docsDir -Force | Out-Null }
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir -Force | Out-Null }

# Move Scripts
Get-ChildItem -Path $targetRoot -File | Where-Object { $_.Extension -in ".ps1", ".bat" } | ForEach-Object {
    $dest = Join-Path $scriptsDir $_.Name
    Move-Item -LiteralPath $_.FullName -Destination $dest -Force
    Write-Host "Moved Script: $($_.Name)"
}

# Move Docs
Get-ChildItem -Path $targetRoot -File | Where-Object { 
    ($_.Extension -eq ".md" -or $_.Name -like "*report*" -or $_.Name -like "*RAPORU*" -or $_.Name -like "*OZET*") -and 
    $_.Name -notlike "README.md"
} | ForEach-Object {
    $dest = Join-Path $docsDir $_.Name
    Move-Item -LiteralPath $_.FullName -Destination $dest -Force
    Write-Host "Moved Doc: $($_.Name)"
}

# Move Logs
Get-ChildItem -Path $targetRoot -File | Where-Object { 
    $_.Name -like "*error*" -or $_.Name -like "_*" 
} | ForEach-Object {
    $dest = Join-Path $logsDir $_.Name
    Move-Item -LiteralPath $_.FullName -Destination $dest -Force
    Write-Host "Moved Log: $($_.Name)"
}

Write-Host "Organization Complete."
