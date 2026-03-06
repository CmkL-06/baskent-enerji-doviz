# Başkent Enerji Döviz - Teknik Analiz Raporu (2026-03-06)

## 1) Branch / Git Durumu
```
## cursor/flaky-test-resolution-146b...origin/cursor/flaky-test-resolution-146b
435cbc5 (HEAD -> cursor/flaky-test-resolution-146b, origin/cursor/flaky-test-resolution-146b) Merge remote-tracking branch 'origin/BASKENT-DOVIZ' into cursor/flaky-test-resolution-146b
dfb5e07 (origin/HEAD, origin/BASKENT-DOVIZ) Baskent: local import script, external/local_import (.cursor rules, SmileMedical refs) - disarida kalmasin
3af48fb Merge main into BASKENT-DOVIZ: local import report ve tek referans
76e0807 (origin/main) Add local import scan report
9fd0f10 Align core API behavior and auth validation safeguards
c655f37 Import local helper folders for consolidation scan
635f7b6 Add repo consolidation verification report
2a56aab Add missing diagnostics controller from API repository
00fdd13 Consolidate API repo companion assets into monorepo
5de2044 Harden generated payment and transaction numbers
```

## 2) GitHub Repo Durumu
```json
[{"name":"baskent-enerji-doviz","nameWithOwner":"CmkL-06/baskent-enerji-doviz","updatedAt":"2026-03-06T22:48:37Z","url":"https://github.com/CmkL-06/baskent-enerji-doviz"},{"name":"baskentenerji-doviz-api","nameWithOwner":"CmkL-06/baskentenerji-doviz-api","updatedAt":"2026-03-01T20:13:24Z","url":"https://github.com/CmkL-06/baskentenerji-doviz-api"}]
```

## 3) external/local_import Envanteri
- exists: true
- file_count: 6
- total_size_mb: 0.01
- top_level_counts:
  - .cursor: 4
  - README.md: 1
  - SmileMedical: 1

## 4) SmileMedical Envanteri
- exists: true
- file_count: 1386
- total_size_mb: 74.85
- top_extensions:
  - .cs: 1065
  - .dll: 140
  - .ps1: 57
  - .txt: 29
  - .md: 16
  - .json: 14
  - .vue: 12
  - .png: 12
  - .js: 10
  - .csproj: 9
  - <noext>: 4
  - .html: 4

## 5) Git İzlenen Dosya Hacmi / Riskli Uzantılar
```
tracked_files_total: 2003
tracked_files_smilemedical: 1386
tracked_files_external_local_import: 6
tracked_binary_like_count: 143
```

## 6) Build/Test Durumu
```
Build succeeded.
    6 Warning(s)
    0 Error(s)
dotnet test exit_code: 0
solution_projects:
Project(s)
----------
AnasıTAS_Deniz.API/AnasıTAS_Deniz.API.csproj
AnasıTAS_Deniz.Business/AnasıTAS_Deniz.Business.csproj
AnasıTAS_Deniz.Data/AnasıTAS_Deniz.Data.csproj
AnasıTAS_Deniz.Entity/AnasıTAS_Deniz.Entity.csproj
```

## 7) Notlar
- Yeni local import raporu dosyasi: `baskent_enerji_doviz_local_import.md`
- Import script: `scripts/Import-BaskentDovizLocalImport.ps1`
- Branch merge: `origin/BASKENT-DOVIZ` -> `cursor/flaky-test-resolution-146b` tamamlandi.