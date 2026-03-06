# OrganizeQuantaQuokka.ps1
# Organizes files in QuantaQuokka root into _Scripts, _Docs, _Logs

$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka\SmileMedical"
# Note: The merge might have put things in QuantaQuokka root OR QuantaQuokka\SmileMedical depending on structure.
# Let's check both or assume standard structure.
# Based on previous lists, the clutter was in QuantaQuokka_\SmileMedical.
# So after merge, it should be in QuantaQuokka\SmileMedical.

if (-not (Test-Path $root)) {
    # Fallback to just QuantaQuokka if SmileMedical subdir doesn't exist
    $root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka"
}

Write-Host "Organizing $root..."

$scriptsDir = Join-Path $root "_Scripts"
$docsDir = Join-Path $root "_Docs"
$logsDir = Join-Path $root "_Logs"

New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null
New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

# Move Scripts
Get-ChildItem -Path $root -File | Where-Object { $_.Extension -in ".ps1", ".bat" } | Move-Item -Destination $scriptsDir -Force
Write-Host "Moved Scripts"

# Move Docs (md, txt reports - excluding crucial project files like sln)
Get-ChildItem -Path $root -File | Where-Object { 
    ($_.Extension -eq ".md" -or $_.Name -like "*report*" -or $_.Name -like "*RAPORU*" -or $_.Name -like "*OZET*") -and 
    $_.Name -notlike "README.md"
} | Move-Item -Destination $docsDir -Force
Write-Host "Moved Docs"

# Move Logs/Errors
Get-ChildItem -Path $root -File | Where-Object { 
    $_.Name -like "*error*" -or $_.Name -like "_*" 
} | Move-Item -Destination $logsDir -Force
Write-Host "Moved Logs"

Write-Host "Organization Complete."
