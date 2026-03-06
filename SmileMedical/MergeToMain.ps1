# MergeToMain.ps1
# Merges content from QuantaQuokka_ to QuantaQuokka
# Usage: .\MergeToMain.ps1

$sourceRoot = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$targetRoot = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka"
$logFile = "$targetRoot\_merge_log.txt"

if (-not (Test-Path $targetRoot)) {
    Write-Host "Target $targetRoot does not exist! Creating it..."
    New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
}

$files = Get-ChildItem -Path $sourceRoot -Recurse
$count = 0
$skipped = 0
$errors = 0

foreach ($item in $files) {
    $relativePath = $item.FullName.Substring($sourceRoot.Length)
    $targetPath = $targetRoot + $relativePath
    
    if ($item.PSIsContainer) {
        if (-not (Test-Path $targetPath)) {
            New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
            Write-Host "Created dir: $relativePath"
        }
    } else {
        if (Test-Path $targetPath) {
            Write-Host "Skipping existing file: $relativePath"
            $skipped++
        } else {
            try {
                Move-Item -LiteralPath $item.FullName -Destination $targetPath -Force -ErrorAction Stop
                Write-Host "Moved: $relativePath"
                $count++
            } catch {
                Write-Host "ERROR moving $relativePath : $_"
                $errors++
                "Error moving $relativePath : $_" | Out-File -Append -FilePath $logFile
            }
        }
    }
}

Write-Host "Merge Complete."
Write-Host "Moved: $count"
Write-Host "Skipped: $skipped"
Write-Host "Errors: $errors"

if ($count -gt 0 -and $errors -eq 0) {
    Write-Host "Merge successful."
}
