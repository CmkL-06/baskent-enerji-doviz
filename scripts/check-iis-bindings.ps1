Import-Module WebAdministration

Write-Host "=== TUM IIS SITE BINDINGS ===" -ForegroundColor Cyan
Get-WebBinding | Select-Object -Property `
    @{N='Site';E={$_.ItemXPath -replace ".*\[@name='([^']+)'\].*", '$1'}},
    protocol, bindingInformation |
    Sort-Object Site | Format-Table -AutoSize

Write-Host ""
Write-Host "=== MONEYTRANSFERTURKEY ILE ESLESEN SITELER ===" -ForegroundColor Yellow
Get-WebBinding | Where-Object { $_.bindingInformation -like "*moneytransfer*" } |
    Select-Object -Property `
    @{N='Site';E={$_.ItemXPath -replace ".*\[@name='([^']+)'\].*", '$1'}},
    protocol, bindingInformation | Format-Table -AutoSize

Write-Host ""
Write-Host "=== BASKENTENERJI ILE ESLESEN SITELER ===" -ForegroundColor Green
Get-WebBinding | Where-Object { $_.bindingInformation -like "*baskent*" } |
    Select-Object -Property `
    @{N='Site';E={$_.ItemXPath -replace ".*\[@name='([^']+)'\].*", '$1'}},
    protocol, bindingInformation | Format-Table -AutoSize

Write-Host ""
Write-Host "=== HOSTNAME OLMAYAN (catch-all) BINDINGS ===" -ForegroundColor Red
Get-WebBinding | Where-Object { $_.bindingInformation -match "^[^:]*:[^:]*:$" } |
    Select-Object -Property `
    @{N='Site';E={$_.ItemXPath -replace ".*\[@name='([^']+)'\].*", '$1'}},
    protocol, bindingInformation | Format-Table -AutoSize

pause
