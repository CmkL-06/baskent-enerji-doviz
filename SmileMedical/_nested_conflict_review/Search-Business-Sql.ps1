$path = "D:\QuantaQuokka\SmileMedical\SmileMedical.Business"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\BusinessSqlSearch.txt"
Get-ChildItem -Path $path -Recurse -Filter "*.cs" | Select-String -Pattern "UseSqlServer|Microsoft.EntityFrameworkCore.SqlServer" | ForEach-Object {
    "File: $($_.Path)"
    "Line: $($_.LineNumber)"
    "Content: $($_.Line.Trim())"
    "----------------"
} | Out-File -Encoding utf8 $outFile
