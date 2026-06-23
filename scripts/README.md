# Scripts — Proje Otomasyon Scriptleri

Bu klasör üç projeye ait tüm PowerShell, Python ve Shell scriptlerini içerir.

## Klasör Yapısı

```
scripts/
├── baskent/          ← BaskentEnerji.com (api.baskentenerji.com)
├── mtt/              ← MoneyTransferTurkey (api.moneytransferturkey.com)
├── shared/           ← Paylaşılan altyapı (IIS, SQL, Backup)
└── reorganize-scripts.ps1   ← Klasör organizasyon scripti
```

## BASKENT-ENERJI (scripts/baskent/)

| Script | Açıklama |
|--------|----------|
| `build-and-deploy-api.ps1` | BaskentEnerji API build + IIS deploy |
| `Baskentenerji_API_Test.ps1` | API endpoint testi (localhost + canlı) |
| `Test-API-AfterDeploy.ps1` | Deploy sonrası /health + login doğrulama |
| `baskent_test.py` | Python API test scripti |
| `single_project_audit.py` | Yapısal/güvenlik drift kontrol aracı |
| `check-row-counts.ps1` | DB tablo satır sayısı kontrol |
| `dump-users.ps1` | Kullanıcıları dışa aktar |
| `query-users.ps1` | Kullanıcı sorgula |
| `create-ihtiyar-user.ps1` | ihtiyar kullanıcısı oluştur |
| `check-smtp-env.ps1` | SMTP ortam değişkeni kontrol |
| `set-smtp-from.ps1` | SMTP From adresi ayarla |
| `DUZELT-SMTP-VE-GIT.ps1` | SMTP + Git düzeltme |

## MONEY-TRANSFER-TURKEY (scripts/mtt/)

| Script | Açıklama |
|--------|----------|
| `deploy-mtt-api.ps1` | MTT API build + IIS deploy (port 5200) |
| `deploy-mtt-panels.ps1` | HTML panelleri IIS'e deploy et |
| `set-mtt-env-vars.ps1` | MTT_DB_CONNECTION + MTT_JWT_SECRET ayarla |
| `test-mtt-api.ps1` | MTT API test |
| `quick-api-test.ps1` | Hızlı localhost:5000 login testi |
| `fix-mtt-api-sqlaccess.ps1` | App Pool'u LocalSystem yap (SQL Integrated Security) |
| `diagnose-mtt-api.ps1` | startup-error.txt + event log teşhis |
| `fix-and-redeploy-mtt-api.ps1` | Env kontrol + build + deploy + test (5 adım) |
| `iisreset-and-test.ps1` | IIS tam yeniden başlat + API test |
| `add-mtt-api-localhost-binding.ps1` | localhost:5000 binding ekle |
| `read-mtt-api-eventlog.ps1` | Windows Event Log oku |
| `create-mtt-admin.ps1` | İlk admin kullanıcısı oluştur (BCrypt + SQL) |
| `Calistir-SeedUsers_SHA256.ps1` | Exchange DB kullanıcı seed (SQL bağlantısı içerir) |
| `SeedUsers_SHA256_Update.sql` | ihtiyar/Damat/PERSONEL seed SQL |

## PAYLAŞILAN ALTYAPI (scripts/shared/)

| Script | Açıklama |
|--------|----------|
| `IIS-API-AppPool-Optimize.ps1` | IIS App Pool ayarlarını optimize et |
| `IIS-Kontrol.ps1` | IIS site/pool durum raporu |
| `diagnose-iis.ps1` | IIS sorun teşhis |
| `check-iis-bindings.ps1` | Tüm IIS binding'leri listele |
| `grant-system-sql-access.ps1` | NT AUTHORITY\SYSTEM'e SQL erişimi ver |
| `sql-backup-otomatik.ps1` | Otomatik SQL yedekleme scripti |
| `setup-backup-task.ps1` | Task Scheduler backup görevi kur |
| `restore-mssql-backup.ps1` | SQL .bak dosyasını geri yükle |
| `restore-main-db.ps1` | Ana veritabanı geri yükleme |
| `fix-restore-main-db.ps1` | Geri yükleme hata düzeltme |
| `check-db-env.ps1` | DB ortam değişkeni kontrol |
| `fix-sql-login-mapping.ps1` | SQL login/user mapping düzelt |
| `fix-schema-default.ps1` | Schema varsayılanını düzelt |
| `fix-schema-perms.ps1` | Schema izinlerini düzelt |
| `Import-BaskentDovizLocalImport.ps1` | Yerel klasörden import |
| `Kontrol_GitHub_Senkron.ps1` | GitHub senkronizasyon kontrol |
| `CALISTIR-TUMU.ps1` | Tüm scriptleri sırayla çalıştır |
| `api_tooling_smoke.sh` | Health + kritik route smoke testi |
| `package_single_project.sh` | Release paketleme scripti |

## Kullanım

```powershell
# BaskentEnerji API test
.\baskent\Baskentenerji_API_Test.ps1

# MTT API deploy
.\mtt\deploy-mtt-api.ps1

# IIS kontrol
.\shared\IIS-Kontrol.ps1

# Organizasyonu uygula (henuz uygulanmadiysa)
.\reorganize-scripts.ps1
```

> **Not:** `.bat` dosyaları ilgili proje alt klasörüne taşınmıştır.
> Yönetici (Administrator) olarak çalıştırın.
