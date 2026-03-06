# PROJE ADAYLARI - PC'de proje/yarim kalan ornekleri LISTELER. TASIMA/SILME YOK.
# Sonuc: _project_candidates_report.txt
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$reportPath = Join-Path $root "_project_candidates_report.txt"

$searchRoots = @(
    "c:\Users\CmkL-Owner\Desktop",
    "c:\Users\CmkL-Owner\Documents",
    "c:\Users\CmkL-Owner\Source",
    "c:\Users\CmkL-Owner\Downloads"
)

$skipNames = @('node_modules', '.git', 'obj', 'bin', 'packages', '.vs', 'venv', '.venv')
$found = @()
$seen = @{}

foreach ($searchRoot in $searchRoots) {
    if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) { continue }
    Get-ChildItem -Path $searchRoot -Directory -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $dir = $_.FullName
        $name = $_.Name
        if ($seen[$dir]) { return }
        foreach ($s in $skipNames) {
            if ($dir.IndexOf("\$s\", [StringComparison]::OrdinalIgnoreCase) -ge 0) { return }
        }
        $isProject = $false
        $reason = ""
        # Derinlik siniri yok - en dip klasorlere kadar tara
        $sln = Get-ChildItem -Path $dir -Filter "*.sln" -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($sln) { $isProject = $true; $reason = "*.sln" }
        if (-not $isProject) {
            $csproj = Get-ChildItem -Path $dir -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($csproj) { $isProject = $true; $reason = "*.csproj" }
        }
        if (-not $isProject) {
            $pkg = Join-Path $dir "package.json"
            if (Test-Path -LiteralPath $pkg -PathType Leaf) { $isProject = $true; $reason = "package.json" }
        }
        if (-not $isProject) {
            $git = Join-Path $dir ".git"
            if (Test-Path -LiteralPath $git -PathType Container) { $isProject = $true; $reason = ".git" }
        }
        if (-not $isProject) {
            $py = Get-ChildItem -Path $dir -Filter "requirements.txt" -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($py) { $isProject = $true; $reason = "requirements.txt" }
        }
        if ($isProject) {
            $seen[$dir] = $true
            $found += "$dir|$reason"
        }
    }
}

$found | Set-Content -Path $reportPath -Encoding UTF8
Write-Host "Bulunan proje adayi: $($found.Count). Rapor: $reportPath"
