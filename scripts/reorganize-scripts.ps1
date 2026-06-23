# reorganize-scripts.ps1
# scripts/ klasorunu proje bazli alt klasorlere reorganize eder.
# Calistirilmadan once ONCE inceleyin.
# Yonetici (Administrator) olarak calistirilmalidir.

$ScriptsDir = "C:\inetpub\baskent-enerji-doviz\scripts"
$Baskent    = "$ScriptsDir\baskent"
$Mtt        = "$ScriptsDir\mtt"
$Shared     = "$ScriptsDir\shared"

# Klasorleri olustur
New-Item -ItemType Directory -Force -Path $Baskent  | Out-Null
New-Item -ItemType Directory -Force -Path $Mtt      | Out-Null
New-Item -ItemType Directory -Force -Path $Shared   | Out-Null

Write-Host "=== scripts/ Reorganizasyonu ===" -ForegroundColor Cyan

# ── BaskentEnerji scriptleri ─────────────────────────────────
$baskentFiles = @(
    "build-and-deploy-api.ps1",
    "Baskentenerji_API_Test.ps1",
    "Test-API-AfterDeploy.ps1",
    "baskent_test.py",
    "single_project_audit.py",
    "check-row-counts.ps1",
    "dump-users.ps1",
    "query-users.ps1",
    "create-ihtiyar-user.ps1",
    "check-smtp-env.ps1",
    "set-smtp-from.ps1",
    "DUZELT-SMTP-VE-GIT.ps1",
    "run-build-deploy.bat",
    "run-git-pull-and-deploy.bat",
    "run-check-rows.bat",
    "run-dump-users.bat",
    "run-query-users.bat",
    "run-create-ihtiyar.bat",
    "run-check-smtp.bat",
    "run-set-smtp-from.bat",
    "diagnose-api-500.bat"
)

foreach ($f in $baskentFiles) {
    $src = "$ScriptsDir\$f"
    if (Test-Path $src) {
        Move-Item -Path $src -Destination "$Baskent\$f" -Force
        Write-Host "  [BASKENT] $f" -ForegroundColor Green
    }
}

# ── MoneyTransferTurkey scriptleri ──────────────────────────
$mttFiles = @(
    "deploy-mtt-api.ps1",
    "deploy-mtt-panels.ps1",
    "set-mtt-env-vars.ps1",
    "test-mtt-api.ps1",
    "fix-mtt-api-sqlaccess.ps1",
    "add-mtt-api-localhost-binding.ps1",
    "read-mtt-api-eventlog.ps1",
    "diagnose-mtt-api.ps1",
    "fix-and-redeploy-mtt-api.ps1",
    "iisreset-and-test.ps1",
    "quick-api-test.ps1",
    "create-mtt-admin.ps1",
    "Calistir-SeedUsers_SHA256.ps1",
    "SeedUsers_SHA256_Update.sql",
    "run-deploy-mtt-api.bat",
    "run-deploy-mtt-panels.bat",
    "run-set-mtt-env.bat",
    "run-test-mtt-api.bat",
    "run-fix-mtt-api-sqlaccess.bat",
    "run-diagnose-mtt-api.bat",
    "run-fix-and-redeploy-mtt-api.bat",
    "run-iisreset-and-test.bat"
)

foreach ($f in $mttFiles) {
    $src = "$ScriptsDir\$f"
    if (Test-Path $src) {
        Move-Item -Path $src -Destination "$Mtt\$f" -Force
        Write-Host "  [MTT]     $f" -ForegroundColor Yellow
    }
}

# ── Paylasillan/Altyapi scriptleri ───────────────────────────
$sharedFiles = @(
    "IIS-API-AppPool-Optimize.ps1",
    "IIS-Kontrol.ps1",
    "diagnose-iis.ps1",
    "check-iis-bindings.ps1",
    "grant-system-sql-access.ps1",
    "sql-backup-otomatik.ps1",
    "setup-backup-task.ps1",
    "restore-mssql-backup.ps1",
    "restore-main-db.ps1",
    "fix-restore-main-db.ps1",
    "check-db-env.ps1",
    "fix-sql-login-mapping.ps1",
    "fix-schema-default.ps1",
    "fix-schema-perms.ps1",
    "Import-BaskentDovizLocalImport.ps1",
    "Kontrol_GitHub_Senkron.ps1",
    "CALISTIR-TUMU.ps1",
    "run-restore.bat",
    "run-restore-main.bat",
    "run-fix-restore.bat",
    "run-setup-backup.bat",
    "run-grant-system-sql.bat",
    "run-diagnose-iis.bat",
    "run-check-db-env.bat",
    "run-fix-sql-login.bat",
    "restart-api-apppool.bat",
    "run-fix-schema.bat",
    "run-fix-schema-perms.bat",
    "run-check-iis-bindings.bat",
    "git-commit-backup-scripts.bat",
    "git-commit-restore-scripts.bat",
    "git-commit-deploy-scripts.bat",
    "git-commit-new-scripts.bat",
    "git-commit-automapper.bat",
    "git-commit-nullable-fix.bat",
    "git-status-check.bat",
    "api_tooling_smoke.sh",
    "package_single_project.sh"
)

foreach ($f in $sharedFiles) {
    $src = "$ScriptsDir\$f"
    if (Test-Path $src) {
        Move-Item -Path $src -Destination "$Shared\$f" -Force
        Write-Host "  [SHARED]  $f" -ForegroundColor Cyan
    }
}

# Kalan dosyalari listele
Write-Host ""
Write-Host "=== Klasorde kalan dosyalar ===" -ForegroundColor Magenta
Get-ChildItem -Path $ScriptsDir -File | Where-Object { $_.Name -notin @("README.md","reorganize-scripts.ps1","CALISTIR-TUMU-LOG.txt","login-body.example.json","users-dump.txt","IIS-Kontrol.ps1") } | ForEach-Object {
    Write-Host "  KALAN: $($_.Name)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Reorganizasyon tamamlandi." -ForegroundColor Green
Write-Host "Bat dosyalarinin yollarini guncellemeyi unutmayin!" -ForegroundColor Yellow
pause
