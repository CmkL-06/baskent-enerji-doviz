$target = "..\..\QuantaQuokka"
if (Test-Path $target) {
    Get-ChildItem $target -Recurse | Select-Object FullName, Length, LastWriteTime | Format-Table
} else {
    Write-Host "Target does not exist (accessible?)"
}
