# QuantaQuokka_ hash index - SHA256 for all files
$root = "c:\Users\CmkL-Owner\Desktop\QuantaQuokka_"
$out  = Join-Path $root "_hash_index.txt"
$list = @()
Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -eq "_hash_index.txt" -or $_.Name -eq "BuildHashIndex.ps1") { return }
    try {
        $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256 -ErrorAction Stop).Hash
        $rel = $_.FullName.Substring($root.Length).TrimStart('\')
        $list += "$hash|$rel"
    } catch {}
}
$list | Set-Content -Path $out -Encoding UTF8
Write-Host "Total hashed: $($list.Count)"
