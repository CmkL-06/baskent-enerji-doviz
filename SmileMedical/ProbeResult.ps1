$target = "..\..\QuantaQuokka\SmileMedical"
if (-not (Test-Path $target)) { $target = "..\..\QuantaQuokka" }
Get-ChildItem $target
Get-ChildItem -Path "$target\_Scripts" -ErrorAction SilentlyContinue | Select-Object Name
Get-ChildItem -Path "$target\_Docs" -ErrorAction SilentlyContinue | Select-Object Name
