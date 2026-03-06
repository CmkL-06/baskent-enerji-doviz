$path = "D:\QuantaQuokka\SmileMedical"
Get-ChildItem -Path $path -Recurse -Filter "*.cs" | Select-String -Pattern "Microsoft.EntityFrameworkCore.SqlServer|UseSqlServer" | ForEach-Object {
    Write-Host "Match: $($_.Path)"
    Write-Host "Line: $($_.LineNumber)"
    Write-Host "Txt: $($_.Line.Trim())"
}
