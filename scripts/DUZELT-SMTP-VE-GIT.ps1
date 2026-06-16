# Duzeltme Scripti: SMTP Firewall + Git Temizleme
# Yonetici (Administrator) olarak calistir!

$repoRoot = "C:\inetpub\baskent-enerji-doviz"
$ErrorActionPreference = "Continue"

function OK($m)   { Write-Host "OK: $m" -ForegroundColor Green }
function ERR($m)  { Write-Host "XX $m"  -ForegroundColor Red }
function HEAD($m) { Write-Host ""; Write-Host "=== $m ===" -ForegroundColor Cyan }

# ============================================================
# ADIM 1: Windows Firewall - SMTP Outbound Port 587
# ============================================================
HEAD "ADIM 1: Windows Firewall SMTP Kural"

$ruleName = "SMTP Outbound Brevo 587"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) {
    OK "Kural zaten mevcut: $ruleName"
} else {
    try {
        New-NetFirewallRule `
            -DisplayName $ruleName `
            -Direction Outbound `
            -Protocol TCP `
            -RemotePort 587 `
            -Action Allow `
            -Profile Any | Out-Null
        OK "Firewall kurali olusturuldu: $ruleName (TCP Out 587)"
    } catch {
        ERR "Kural olusturulamadi: $($_.Exception.Message)"
    }
}

# Test et
Write-Host "  SMTP baglanti testi yapiliyor..." -ForegroundColor Gray
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.ConnectAsync("smtp-relay.brevo.com", 587).Wait(8000) | Out-Null
    if ($tcp.Connected) {
        OK "smtp-relay.brevo.com:587 BASARILI baglanabildi"
        $tcp.Close()
    } else {
        ERR "smtp-relay.brevo.com:587 hala kapali - hosting/ISP engelliyor olabilir"
    }
} catch {
    ERR "smtp-relay.brevo.com:587 BASARISIZ: $($_.Exception.Message)"
    Write-Host "  NOT: Port 587 hoster tarafindan engelleniyorsa Brevo'nun 465 (SSL) portunu deneyin" -ForegroundColor Yellow
}

# ============================================================
# ADIM 2: Git - pycache ve log dosyasini untrack et
# ============================================================
HEAD "ADIM 2: Git Temizleme"

Push-Location $repoRoot

# __pycache__ untrack
$pycache = "telegram-bot/__pycache__"
$tracked = git ls-files $pycache 2>&1
if ($tracked) {
    git rm -r --cached $pycache 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    OK "__pycache__ git takibinden cikarildi"
} else {
    OK "__pycache__ zaten takip edilmiyor"
}

# LOG dosyasini untrack et
$logTracked = git ls-files "scripts/CALISTIR-TUMU-LOG.txt" 2>&1
if ($logTracked) {
    git rm --cached "scripts/CALISTIR-TUMU-LOG.txt" 2>&1 | Out-Null
    OK "CALISTIR-TUMU-LOG.txt git takibinden cikarildi"
} else {
    OK "CALISTIR-TUMU-LOG.txt zaten takip edilmiyor"
}

# SSL script duzeltmesi + bu scriptin kendisi de commit edilecek
git add -A 2>&1 | Out-Null
$status = git status --short 2>&1
if ($status) {
    Write-Host "  Commit edilecekler:" -ForegroundColor Gray
    $status | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    git commit -m "chore: remove pycache from tracking, fix SSL check script (16 Haz 2026)" 2>&1 |
        ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    $pushOut = git push origin BASKENT-DOVIZ 2>&1
    $pushOut | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    if ($LASTEXITCODE -eq 0) { OK "Git push BASARILI" }
    else { ERR "Git push BASARISIZ" }
} else {
    OK "Commit edilecek degisiklik yok"
}

Pop-Location

# ============================================================
# SONUC
# ============================================================
Write-Host ""
Write-Host "=== TAMAMLANDI ===" -ForegroundColor Cyan
Write-Host "Sonraki adimlar:" -ForegroundColor Yellow
Write-Host "  1. API deploy: C:\inetpub\baskent-enerji-doviz\deploy\deploy-full.ps1" -ForegroundColor Yellow
Write-Host "  2. GitHub PR: https://github.com/CmkL-06/baskent-enerji-doviz/pulls" -ForegroundColor Yellow
Write-Host "     PR #6 kapat, PR #11 ve PR #15 merge et" -ForegroundColor Yellow
Write-Host ""
Read-Host "Enter ile cikin"
