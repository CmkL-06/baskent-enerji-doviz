$file = "D:\QuantaQuokka\SmileMedical\SmileMedical.Business\Infrastructure\ExchangeOffice\Office\IExchangeTransactionService.cs"
Get-Content $file | ForEach-Object { "$($_.ReadCount): $_" }
