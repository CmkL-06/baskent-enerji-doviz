# Baskent Enerji - Tam Uygulama ve Test Scripti
# Yonetici olarak calistir!

$ErrorActionPreference = "Continue"
$repoRoot = "C:\inetpub\baskent-enerji-doviz"
$logFile = "$repoRoot\scripts\CALISTIR-TUMU-LOG.txt"
$errors = @()

function Log($msg, $color) {
    if (-not $color) { $color = "White" }
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line -ForegroundColor $color
    Add-Content -Path $logFile -Value $line
}
function OK($msg)   { Log "OK: $msg" "Green" }
function WARN($msg) { Log "!! $msg" "Yellow"; $script:errors += $msg }
function ERR($msg)  { Log "XX $msg" "Red";    $script:errors += $msg }
function HEAD($msg) { Log "" "White"; Log "=== $msg ===" "Cyan" }

if (Test-Path $logFile) { Remove-Item $logFile -Force }
Log "BASKENT ENERJI - $(Get-Date)" "Cyan"

# ============================================================
# ADIM 1: SMTP ENV VAR
# ============================================================
HEAD "ADIM 1: SMTP Env Var"

$smtpPwd = [System.Environment]::GetEnvironmentVariable("Smtp__Password", "Machine")
if ($smtpPwd -and $smtpPwd.Trim().Length -gt 0) {
    OK "Smtp__Password Machine env var SET ($($smtpPwd.Length) karakter)"
} else {
    ERR "Smtp__Password Machine env var BOS veya YOK!"
    Log "  Duzeltme komutunu PowerShell admin'de calistir:" "Yellow"
    Log "  [System.Environment]::SetEnvironmentVariable('Smtp__Password','BURAYA_BREVO_SIFRE','Machine')" "Yellow"
}

try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect("smtp-relay.brevo.com", 587)
    OK "smtp-relay.brevo.com:587 baglantisi BASARILI"
    $tcp.Close()
} catch {
    ERR "smtp-relay.brevo.com:587 BASARISIZ: $($_.Exception.Message)"
}

# ============================================================
# ADIM 2: GIT COMMIT VE PUSH
# ============================================================
HEAD "ADIM 2: Git Commit ve Push"

Push-Location $repoRoot
$gitStatus = git status --short 2>&1
if ($gitStatus) {
    Log "Degisen dosyalar:" "Yellow"
    $gitStatus | ForEach-Object { Log "  $_" "Yellow" }
    git add -A 2>&1 | Out-Null
    git commit -m "fix: Plesk paths removed, CORS HTTP origins cleaned, Program.cs security (16 Haz 2026)" 2>&1 |
        ForEach-Object { Log "  $_" "Gray" }
} else {
    Log "  Commit edilecek degisiklik yok" "Gray"
}

$remote = git remote get-url origin 2>&1
Log "  Remote: $remote" "Gray"

$pushOut = git push origin BASKENT-DOVIZ 2>&1
$pushOut | ForEach-Object { Log "  $_" "Gray" }
if ($LASTEXITCODE -eq 0) {
    OK "Git push BASARILI -> origin/BASKENT-DOVIZ"
} else {
    ERR "Git push BASARISIZ (exit: $LASTEXITCODE)"
}

git log --oneline -3 2>&1 | ForEach-Object { Log "  $_" "Gray" }
Pop-Location

# ============================================================
# ADIM 3: API HEALTH CHECK
# ============================================================
HEAD "ADIM 3: API Health Check"

foreach ($url in @("https://api.baskentenerji.com/health", "https://api.baskentenerji.com/health/live", "https://api.baskentenerji.com/health/ready")) {
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        OK "$url -> $($resp.StatusCode)"
    } catch {
        ERR "$url -> $($_.Exception.Message)"
    }
}

# ============================================================
# ADIM 4: TELEGRAM BOT SERVISLERI
# ============================================================
HEAD "ADIM 4: Telegram Bot Servisleri"

foreach ($svc in @("BaskentBot-Main", "BaskentBot-Operator", "BaskentBot-Ruble")) {
    $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($s) {
        if ($s.Status -eq "Running") { OK "$svc -> RUNNING" }
        else { ERR "$svc -> $($s.Status)" }
    } else {
        WARN "$svc servisi bulunamadi"
    }
}

# ============================================================
# ADIM 5: SQL BACKUP TASK SCHEDULER
# ============================================================
HEAD "ADIM 5: SQL Backup Task Scheduler"

$backupDir = "C:\backups\sql"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    OK "Backup klasoru olusturuldu: $backupDir"
} else {
    OK "Backup klasoru mevcut: $backupDir"
}

$backupScriptPath = "$repoRoot\scripts\sql-backup-otomatik.ps1"
if (Test-Path $backupScriptPath) {
    OK "Backup scripti mevcut: $backupScriptPath"
} else {
    ERR "Backup scripti bulunamadi: $backupScriptPath"
}

$taskName = "BaskentEnerji-SQL-Backup"
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    WARN "Task '$taskName' zaten mevcut - atlandı"
} else {
    try {
        $action   = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NonInteractive -ExecutionPolicy Bypass -File `"$backupScriptPath`""
        $trigger  = New-ScheduledTaskTrigger -Daily -At "02:00"
        $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 1) -StartWhenAvailable
        $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description "Baskent Enerji SQL yedek" | Out-Null
        OK "Task Scheduler gorevi olusturuldu: $taskName (Her gece 02:00)"
    } catch {
        ERR "Task olusturulamadi: $($_.Exception.Message)"
    }
}

# ============================================================
# ADIM 6: IIS SITE DURUMU
# ============================================================
HEAD "ADIM 6: IIS Site Durumu"

$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
if (Test-Path $appcmd) {
    foreach ($site in @("BaskentEnerji-Frontend", "BaskentEnerji-API")) {
        $result = & $appcmd list site /name:$site /text:state 2>$null
        if ($result -eq "Started") { OK "IIS '$site' -> STARTED" }
        else { ERR "IIS '$site' -> '$result'" }
    }
} else {
    WARN "appcmd.exe bulunamadi"
}

# ============================================================
# ADIM 7: SSL KONTROL
# ============================================================
HEAD "ADIM 7: SSL Kontrol"

foreach ($domain in @("baskentenerji.com", "api.baskentenerji.com", "tg.moneytransferturkey.com")) {
    try {
        $req = [System.Net.HttpWebRequest]::Create("https://$domain")
        $req.Timeout = 5000
        $req.AllowAutoRedirect = $false
        $resp = $req.GetResponse()
        $cert = $req.ServicePoint.Certificate
        $expiry = [DateTime]::Parse($cert.GetExpirationDateString())
        $days = ($expiry - (Get-Date)).Days
        if ($days -gt 30)    { OK "$domain SSL -> $days gun kaldi ($expiry)" }
        elseif ($days -gt 0) { WARN "$domain SSL -> $days gun kaldi - YENILE!" }
        else                 { ERR "$domain SSL SURESI DOLMUS!" }
        $resp.Close()
    } catch {
        WARN "$domain SSL kontrol edilemedi: $($_.Exception.Message)"
    }
}

# ============================================================
# SONUC
# ============================================================
HEAD "SONUC"

if ($errors.Count -eq 0) {
    Log "TUM TESTLER BASARILI!" "Green"
} else {
    Log "$($errors.Count) sorun:" "Red"
    $errors | ForEach-Object { Log "  - $_" "Red" }
}

Log "Log: $logFile" "Cyan"
Write-Host ""
Write-Host "Tamamlandi. Enter ile cikin..." -ForegroundColor Gray
Read-Host
