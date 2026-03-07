$path = "D:\QuantaQuokka\SmileMedical"
Get-ChildItem -Path $path -Recurse -Filter "*.cs" | Select-String -Pattern "UseSqlServer" | Select-Object Path, LineNumber, Line
