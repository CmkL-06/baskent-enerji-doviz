$file = "D:\QuantaQuokka\SmileMedical\SmileMedical.Business\Infrastructure\ExchangeOffice\Office\IExchangeTransactionService.cs"
$content = Get-Content $file
$newContent = $content | Where-Object { $_ -notmatch "using SmileMedical.Data.Migrations;" }
Set-Content $file $newContent
Write-Host "Fixed IExchangeTransactionService.cs"
