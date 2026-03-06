$logFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\build_bus.log"
Get-Content $logFile | Select-String -Pattern "(error|Hata)\s+CS\d+" | ForEach-Object {
    Write-Host "----------------ERROR FOUND----------------"
    Write-Host $_.Line.Trim()
    Write-Host "-------------------------------------------"
}
