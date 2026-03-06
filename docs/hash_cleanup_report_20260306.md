# Hash Temizliği ve İsim Netleştirme Raporu (2026-03-06)

## Uygulanan adımlar
- `%100 hash analiz` çalıştırıldı ve JSON raporu üretildi.
- `SmileMedical/SmileMedical` altındaki **hash’i birebir aynı 568 dosya** kaldırıldı.
- Kalan çakışmalı alan isim netliği için `SmileMedical/_nested_conflict_review` olarak yeniden adlandırıldı.
- Onaysız içerik birleştirme/silme yapılmadı; sadece birebir hash kopyalar kaldırıldı.

## Önce / Sonra
- Önce nested toplam: **605** dosya
- Önce birebir kopya: **568** dosya
- Sonra review alanı toplam: **37** dosya
- Sonra birebir kopya kalan: **0** dosya
- Farklı içerik (aynı relatif yol): **7** dosya
- Sadece nested içinde kalan: **30** dosya

## Kalan inceleme listesi
### A) Aynı relatif yol ama içerik farklı (manuel karar gerekir)
- `CALISMA_PRENSIPLERI.md`
- `SmileMedical.sln`
- `RestoreDeletedSources.ps1`
- `MergeIntoOneFolder.ps1`
- `SmileMedical.Data/SmileMedical.Data.csproj`
- `SmileMedical.API/SmileMedical.API.csproj`
- `SmileMedical.Business/Infrastructure/ExchangeOffice/Office/IExchangeTransactionService.cs`

### B) Sadece review alanında bulunan dosyalar (manuel karar gerekir)
- `CleanErrors.txt`
- `Search-All-Sql.ps1`
- `Install-Tools-And-Migrate.ps1`
- `Build-And-Migrate-D.ps1`
- `errors_utf8.txt`
- `Find-Exact-UseSqlServer.ps1`
- `final_mig_out.txt`
- `Final-Migrate.ps1`
- `Find-Build-Errors.ps1`
- `Search-UseSqlServer.ps1`
- `Debug-Migration-Command.ps1`
- `Fix-Using-Statement.ps1`
- `build_tail.txt`
- `Setup-SQLite-D.ps1`
- `mig_out.txt`
- `errors.txt`
- `Read-Fix-File.ps1`
- `mig_fix_2_out.txt`
- `Fix-API-Imports-And-Link.ps1`
- `mtxr.sqltools-0.28.5.vsix`
- `Search-Business-Sql.ps1`
- `Find-Errors-Retry.ps1`
- `Run-Migrations-D.ps1`
- `Locate-DB.ps1`
- `Fix-API-Package-And-Migrate.ps1`
- `migration_log.txt`
- `BusinessSqlSearch.txt`
- `Inspect-API-Config.ps1`
- `mig_fix_out.txt`
- `.vscode/launch.json`

## Referans dosyalar
- `docs/hash_analysis_20260306.json` (temizlik öncesi)
- `docs/hash_analysis_post_cleanup_20260306.json` (temizlik sonrası)