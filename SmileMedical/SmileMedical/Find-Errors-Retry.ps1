$logFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\build_bus.log"
$outFile = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_\SmileMedical\CleanErrors.txt"

$content = Get-Content $logFile
$errors = $content | Select-String -Pattern "(error\s+CS|Hata\s+CS|FAILED|BAŞARISIZ)"

if ($errors) {
    $errors | ForEach-Object { $_.Line } | Out-File -Encoding utf8 $outFile
    Write-Host "Found $($errors.Count) error lines."
} else {
    Write-Host "No explicit error lines found."
}
