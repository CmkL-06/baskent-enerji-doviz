# Başkent Enerji Döviz - Local import (YARDIMCI_KLASORLER -> external/local_import)
# baskentenerji.com döviz muhasebe programı repo'suna uyumlu
#
# UYARI: Yerelde onay vermeden asla gerçek çalıştırma yapılmaz.
# - Önce -DryRun ile sanal test yapın; sonra onay verip -Apply ile çalıştırın.

param(
    # Sanal test: sadece ne yapılacağını listeler; kopyalama ve git YAPILMAZ.
    [switch]$DryRun,
    # Gerçek çalıştırma: kopyala + git add/commit/push. Sadece siz onay verdikten sonra kullanın.
    [switch]$Apply,
    # Repo yolu (opsiyonel). Vermezseniz asagidaki $REPO kullanilir.
    [string]$RepoPath = "",
    # Kaynak klasor yolu (opsiyonel). Vermezseniz asagidaki $SRC kullanilir.
    [string]$SrcPath = ""
)

$ErrorActionPreference = "Stop"

# --- YAPILANDIRMA: Varsayilan yollar (parametre verilmezse bunlar kullanilir) ---
$REPO = if ($RepoPath) { $RepoPath } else { "D:\YARDIMCI_KLASORLER\QuantaQuokka" }
$SRC  = if ($SrcPath)  { $SrcPath }  else { "D:\YARDIMCI_KLASORLER" }

$DEST = "external\local_import"
# Repo kendi icinde kalmasin diye QuantaQuokka da haric (SRC = YARDIMCI_KLASORLER icindeyse)
$EXCLUDE = @(".git", "node_modules", "__pycache__", "QuantaQuokka")

# Gerçek işlem sadece -Apply ile; yoksa her zaman sadece test/dry-run
$doRealRun = $Apply
if (-not $DryRun -and -not $Apply) {
    Write-Host "SANAL TEST modu (hicbir dosya kopyalanmaz, git calistirilmaz)." -ForegroundColor Yellow
    Write-Host "Gercek calistirma icin: -Apply parametresi gerekir (onay sonrasi)." -ForegroundColor Yellow
    $DryRun = $true
}

# --- Kontroller ---
if (-not (Test-Path $REPO)) {
    Write-Host "HATA: Repo bulunamadi: $REPO" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $SRC)) {
    Write-Host "HATA: Kaynak klasor bulunamadi: $SRC" -ForegroundColor Red
    Write-Host "Lutfen YARDIMCI_KLASORLER'i olusturup tekrar deneyin." -ForegroundColor Yellow
    exit 1
}

$destPath = Join-Path $REPO $DEST

# --- Ne kopyalanacak listesi (sanal test) ---
function Get-ItemsToCopy {
    param([string]$From, [string]$To, [string[]]$Exclude, [string]$Prefix = "")
    $items = Get-ChildItem -Path $From -Force -ErrorAction SilentlyContinue
    foreach ($item in $items) {
        if ($item.Name -in $Exclude) { continue }
        $rel = if ($Prefix) { "$Prefix\$($item.Name)" } else { $item.Name }
        if ($item.PSIsContainer) {
            Get-ItemsToCopy -From $item.FullName -To (Join-Path $To $item.Name) -Exclude $Exclude -Prefix $rel
        } else {
            [PSCustomObject]@{ Relative = $rel; FullSource = $item.FullName }
        }
    }
}

$toCopy = @(Get-ItemsToCopy -From $SRC -To $destPath -Exclude $EXCLUDE)
Write-Host "`n--- Yapilandirma ---" -ForegroundColor Cyan
Write-Host "  REPO : $REPO"
Write-Host "  SRC  : $SRC"
Write-Host "  HEDEF: $destPath"
Write-Host "  HARIC: $($EXCLUDE -join ', ')"

if ($DryRun) {
    Write-Host "`n--- SANAL TEST (DryRun): Kopyalanacak ogeler ---" -ForegroundColor Cyan
    if ($toCopy.Count -eq 0) {
        Write-Host "  (Kaynak bos veya sadece haric klasorler var.)" -ForegroundColor Gray
    } else {
        $toCopy | ForEach-Object { Write-Host "  $($_.Relative)" }
        Write-Host "`n  Toplam: $($toCopy.Count) dosya" -ForegroundColor Gray
    }
    Write-Host "`n--- Gercek calistirma yapilmayacak ---" -ForegroundColor Yellow
    Write-Host "  Onay verirseniz su komutla calistirin:" -ForegroundColor White
    Write-Host "  .\Import-BaskentDovizLocalImport.ps1 -Apply" -ForegroundColor White
    exit 0
}

if (-not $doRealRun) {
    Write-Host "Gerçek islem yapilmadi. -Apply ile onay verin." -ForegroundColor Yellow
    exit 0
}

# --- GERCEK CALISTIRMA (sadece -Apply ile) ---
Write-Host "`n--- GERCEK CALISTIRMA (-Apply onayli) ---" -ForegroundColor Green

New-Item -ItemType Directory -Path $destPath -Force | Out-Null

function Copy-WithExclude {
    param([string]$From, [string]$To, [string[]]$Exclude)
    $items = Get-ChildItem -Path $From -Force -ErrorAction SilentlyContinue
    foreach ($item in $items) {
        if ($item.Name -in $Exclude) { continue }
        $target = Join-Path $To $item.Name
        if ($item.PSIsContainer) {
            if (-not (Test-Path $target)) { New-Item -ItemType Directory -Path $target -Force | Out-Null }
            Copy-WithExclude -From $item.FullName -To $target -Exclude $Exclude
        } else {
            Copy-Item -Path $item.FullName -Destination $target -Force
        }
    }
}

Write-Host "Kopyalaniyor: $SRC -> $destPath" -ForegroundColor Cyan
Copy-WithExclude -From $SRC -To $destPath -Exclude $EXCLUDE

Push-Location $REPO
try {
    git add $DEST
    if ($LASTEXITCODE -ne 0) { throw "git add failed" }
    git status --short $DEST
    git commit -m "Import local helper folders for consolidation scan"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Uyari: Commit atlandi (degisiklik yok veya zaten commit edilmis)." -ForegroundColor Yellow
    } else {
        git push -u origin HEAD
        if ($LASTEXITCODE -ne 0) { Write-Host "Uyari: git push basarisiz (remote/izin kontrol edin)." -ForegroundColor Yellow }
    }
} finally {
    Pop-Location
}

Write-Host "Tamamlandi: $destPath" -ForegroundColor Green
