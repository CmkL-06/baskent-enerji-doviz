# Scripts - Başkent Enerji Döviz

Bu klasör, projeye ait otomasyon ve test scriptlerini içerir.

## Script Dosyaları

| Dosya | Açıklama |
|-------|----------|
| Baskentenerji_API_Test.ps1 | API test scripti |
| Calistir-SeedUsers_SHA256.ps1 | SHA256 ile kullanıcı seed scripti |
| Clean-SmileMedical-ExactHashDuplicates.ps1 | Hash bazlı nested kopya temizliği |
| IIS-API-AppPool-Optimize.ps1 | IIS App Pool optimizasyon scripti |
| IIS-Kontrol.ps1 | IIS kontrol scripti |
| SeedUsers_SHA256_Update.sql | Kullanıcı seed SQL güncellemesi |
| Test-API-AfterDeploy.ps1 | Deploy sonrası API test |
| baskent_test.py | Python test scripti |
| login-body.example.json | Login body örnek JSON |
| package_single_project.sh | Kanonik proje paketleme scripti |
| single_project_audit.py | Yapısal/güvenlik drift kontrol scripti |

## Kullanım

- Repo sağlık kontrolü:
  - `python3 scripts/single_project_audit.py`
- Kanonik release paketi:
  - `bash scripts/package_single_project.sh`
