# Project: QuantaQuokka_
# Opens the project in Cursor IDE
$projectRoot = $PSScriptRoot

if (Get-Command cursor -ErrorAction SilentlyContinue) {
    cursor $projectRoot
} else {
    $cursorPath = "$env:LOCALAPPDATA\Programs\cursor\Cursor.exe"
    if (Test-Path $cursorPath) {
        Start-Process $cursorPath -ArgumentList "`"$projectRoot`""
    } else {
        Write-Host "Cursor IDE bulunamadi. Lutfen yuklu oldugundan emin olun."
    }
}
