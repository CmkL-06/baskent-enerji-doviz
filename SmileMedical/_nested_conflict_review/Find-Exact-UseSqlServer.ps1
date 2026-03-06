$path = "D:\QuantaQuokka\SmileMedical\SmileMedical.Business"
Get-ChildItem -Path $path -Recurse -Filter "*.cs" | Select-String -Pattern "UseSqlServer" | ForEach-Object {
    Write-Host "File: $($_.Path)"
    Write-Host "Line: $($_.LineNumber)"
    Write-Host "Content: $($_.Line.Trim())"
    Write-Host "----------------"
}
