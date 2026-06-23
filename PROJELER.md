# PROJELER — Klasör Haritası ve Proje Kökü Kılavuzu

> Son güncelleme: 18 Haziran 2026
> Bu belge, `baskent-enerji-doviz` mono-repo içindeki tüm projeleri ve dosyaları sınıflandırır.

---

## 📦 PROJE 1: BASKENT-ENERJI
**Domain:** baskentenerji.com | api.baskentenerji.com
**Teknoloji:** .NET 8 ASP.NET Core Web API · EF Core · MSSQL · Vue 3 (Vite)
**Veritabanı:** `mtt-moneyexchangeturkey` (şema: `mtturkey_exchange`)

### Klasörler
| Klasör | Açıklama |
|--------|----------|
| `BaskentEnerji.API/` | Ana API projesi (.NET 8 Controller-based) |
| `BaskentEnerji.Business/` | İş mantığı katmanı — Exchange, AutoRate, Blog, Coin, Site hizmetleri |
| `BaskentEnerji.Data/` | EF Core DbContext — `BaskentEnerjiDbContext` |
| `BaskentEnerji.Entity/` | Entity / DTO / Request / Response modelleri |
| `BaskentEnerji.Tests/` | Unit test katmanı |
| `BaskentEnerji.sln` | Visual Studio Solution dosyası |
| `dashboard/index.html` | Yönetim paneli → /ihtiyar/ redirect (tek dosya) |
| `login/index.html` | Login sayfası (tek dosya) |
| `deploy/` | Derlenmiş publish çıktıları (publish-api, publish-api-windows) |
| `_publish_output/` | ESKİ publish çıktısı — kullanılmıyor, temizlenebilir |

### API Endpoint'leri
- `/api/v1/Exchange` — Döviz/Kasa işlemleri
- `/api/v1/User/login` — JWT Auth (BCrypt + SHA256 legacy)
- `/api/v1/Diagnostics/*` — Sistem teşhis
- `/health` — Sağlık kontrolü
- `/swagger` — API dökümantasyonu

### Canlı URL'ler
- Frontend: https://baskentenerji.com/ihtiyar (Vue 3 — IIS üzerinde)
- API: https://api.baskentenerji.com/api/v1
- Deploy yolu: `C:\inetpub\baskentenerji-api\` (IIS fiziksel yol)

### BaskentEnerji Scriptleri (scripts/baskent/)
| Script | Açıklama |
|--------|----------|
| `build-and-deploy-api.ps1` | Build + IIS deploy |
| `Baskentenerji_API_Test.ps1` | API endpoint test |
| `Test-API-AfterDeploy.ps1` | Deploy sonrası doğrulama |
| `baskent_test.py` | Python test scripti |
| `single_project_audit.py` | Yapısal/güvenlik drift kontrolü |
| `check-row-counts.ps1` | DB satır sayısı kontrol |
| `dump-users.ps1` | Kullanıcıları dışa aktar |
| `query-users.ps1` | Kullanıcı sorgula |
| `create-ihtiyar-user.ps1` | ihtiyar kullanıcısı oluştur |
| `check-smtp-env.ps1` | SMTP env kontrol |
| `set-smtp-from.ps1` | SMTP From adresi ayarla |
| `DUZELT-SMTP-VE-GIT.ps1` | SMTP + Git düzelt |

---

## 📦 PROJE 2: MONEY-TRANSFER-TURKEY (MTT)
**Domain:** moneytransferturkey.com | tg.moneytransferturkey.com | api.moneytransferturkey.com
**Teknoloji:** .NET 8 Minimal API · EF Core · MSSQL · HTML/Bootstrap (paneller)
**Veritabanı:** `mtt-moneyexchangeturkey` (şema: `mtturkey_exchange`)

### Klasörler
| Klasör | Açıklama |
|--------|----------|
| `mtt-api/` | Yeni .NET 8 Minimal API — BCrypt auth, JWT, dealer/operator/admin endpoint'leri |
| `mtt-api/Models/` | User, Dealer, Operator, Transaction, ExchangeRate, ChatMessage, CryptoDeposit |
| `mtt-api/Data/MttDbContext.cs` | EF Core DbContext |
| `mtt-api/Endpoints/` | AuthEndpoints, DealerEndpoints, OperatorEndpoints, AdminEndpoints |
| `mtt-api/Helpers/JwtHelper.cs` | JWT token üretici |
| `mtt-panels/` | HTML paneller (Bootstrap 5) |
| `mtt-panels/login.html` | Panel girişi |
| `mtt-panels/dealer.html` | Bayi paneli |
| `mtt-panels/operator.html` | Operatör paneli |
| `mtt-panels/admin.html` | Yönetici paneli |

### API Endpoint'leri (mtt-api)
- `POST /api/auth/login` — BCrypt doğrulama, JWT döner
- `POST /api/auth/change-password` — Şifre değiştir (auth gerekli)
- `GET /api/dealer/dashboard` — Bayi panosu
- `GET /api/operator/transactions` — Operatör işlemleri
- `GET /api/admin/dashboard` — Yönetici panosu

### Ortam Değişkenleri
| Değişken | Açıklama |
|----------|----------|
| `MTT_DB_CONNECTION` | MSSQL bağlantı dizesi (Machine level) |
| `MTT_JWT_SECRET` | JWT imzalama anahtarı (Machine level) |

### Canlı URL'ler
- Paneller: https://tg.moneytransferturkey.com (IIS fiziksel yol: `C:\inetpub\mtt-panels\`)
- API: https://api.moneytransferturkey.com (port 5200, IIS fiziksel yol: `C:\inetpub\moneytransfer-api\`)

### MTT Scriptleri (scripts/mtt/)
| Script | Açıklama |
|--------|----------|
| `deploy-mtt-api.ps1` | MTT API build + IIS deploy |
| `deploy-mtt-panels.ps1` | HTML panelleri deploy |
| `set-mtt-env-vars.ps1` | MTT_DB_CONNECTION + MTT_JWT_SECRET env ayarla |
| `test-mtt-api.ps1` | MTT API test |
| `quick-api-test.ps1` | Hızlı login testi (localhost:5000) |
| `fix-mtt-api-sqlaccess.ps1` | App Pool kimliğini LocalSystem yap |
| `diagnose-mtt-api.ps1` | Hata teşhis (startup-error.txt + event log) |
| `fix-and-redeploy-mtt-api.ps1` | Env kontrol + build + deploy + test |
| `iisreset-and-test.ps1` | IIS tam yeniden başlat + test |
| `add-mtt-api-localhost-binding.ps1` | localhost:5000 binding ekle |
| `read-mtt-api-eventlog.ps1` | Windows Event Log oku |
| `create-mtt-admin.ps1` | İlk admin kullanıcısı oluştur (BCrypt) |
| `Calistir-SeedUsers_SHA256.ps1` | Exchange DB kullanıcı seed |
| `SeedUsers_SHA256_Update.sql` | ihtiyar/Damat/PERSONEL seed SQL |

---

## 📦 PROJE 3: TELEGRAM-BOT
**Domain:** tg.moneytransferturkey.com (Flask web paneli)
**Teknoloji:** Python 3 · python-telegram-bot 20.6 · Flask · pyodbc · MSSQL
**Veritabanı:** `mtturkey_telegram` (ayrı DB)
**BağIantı:** BaskentEnerji API'sine `https://api.baskentenerji.com/api/v1` üzerinden bağlanır

### Klasör: telegram-bot/
| Dosya | Açıklama |
|-------|----------|
| `main_bot.py` | Ana müşteri botu (QR → Para birimi → USDT/Ruble akışı) |
| `operator_bot.py` | Operatör botu |
| `ruble_bot.py` | Ruble kanal botu |
| `exchange_rates.py` | Kur çekici (BaskentEnerji API) |
| `baskent_api.py` | BaskentEnerji API istemcisi |
| `crypto_exchanges.py` | Binance API entegrasyonu |
| `database.py` | MSSQL veri erişimi (pyodbc) |
| `config.py` | Merkezi konfigürasyon (.env okuma) |
| `translations.py` | Çok dil desteği (TR/RU) |
| `run_all.py` | Tüm botları başlat |
| `panel_kontrol.py` | Flask web paneli |
| `kanal_bot_kontrol.py` | Kanal bot kontrolü |
| `requirements.txt` | Python bağımlılıkları |

### Config (.env) Değişkenleri
| Değişken | Açıklama |
|----------|----------|
| `MAIN_BOT_TOKEN` | Ana bot Telegram token |
| `OPERATOR_BOT_TOKEN` | Operatör bot token |
| `RUBLE_BOT_TOKEN` | Ruble bot token |
| `DB_SERVER/DB_NAME/DB_USERNAME/DB_PASSWORD` | SQL bağlantısı |
| `BASKENT_API_URL/BASKENT_USERNAME/BASKENT_PASSWORD` | API erişimi |
| `ADMIN_ID` | Telegram yönetici ID |
| `FLASK_SECRET_KEY/FLASK_PORT` | Flask ayarları |

---

## 🔧 PAYLAŞILAN ALTYAPI

### scripts/shared/ — IIS, SQL, Backup Scriptleri
| Script | Açıklama |
|--------|----------|
| `IIS-API-AppPool-Optimize.ps1` | IIS App Pool optimizasyon ayarları |
| `IIS-Kontrol.ps1` | IIS site/pool durum raporu |
| `diagnose-iis.ps1` | IIS sorun teşhis |
| `check-iis-bindings.ps1` | IIS binding listesi |
| `grant-system-sql-access.ps1` | LocalSystem'a SQL erişimi ver |
| `sql-backup-otomatik.ps1` | Otomatik SQL yedekleme (Task Scheduler'a eklendi) |
| `setup-backup-task.ps1` | Task Scheduler backup görevi kur |
| `restore-mssql-backup.ps1` | SQL .bak geri yükleme |
| `restore-main-db.ps1` | Ana DB geri yükleme |
| `fix-restore-main-db.ps1` | Geri yükleme hata düzeltme |
| `check-db-env.ps1` | DB ortam değişkeni kontrol |
| `fix-sql-login-mapping.ps1` | SQL login mapping düzelt |
| `fix-schema-default.ps1` | Schema varsayılanı düzelt |
| `fix-schema-perms.ps1` | Schema izinleri düzelt |
| `Import-BaskentDovizLocalImport.ps1` | Yerel klasörden import |
| `Kontrol_GitHub_Senkron.ps1` | GitHub senkron kontrol |
| `CALISTIR-TUMU.ps1` | Tüm scriptleri sırayla çalıştır |

### docs/ — Teknik Dokümantasyon
| Dosya | İlgili Proje |
|-------|-------------|
| `FINAL_KANONIK_YAPI.md` | BaskentEnerji |
| `TEK_PROJE_STRATEJISI.md` | BaskentEnerji |
| `CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md` | BaskentEnerji |
| `REFERANS_VERITABANI_VE_YAPILANDIRMA.md` | BaskentEnerji + MTT |
| `IIS_APPLICATION_POOL_OPTIMIZASYON.md` | Paylaşılan |
| `GUVENLIK-RAPORU.md` | Paylaşılan |
| `MASTER_DURUM_RAPORU.md` | Genel |
| `HATA_AYIKLAMA_404_VE_SISTEM_ANALIZI.md` | BaskentEnerji |

### backups/ — Yedek Dosyalar
| Dosya | İçerik |
|-------|--------|
| `backup_dbdump_2511200657.dump` | PostgreSQL/MariaDB dump (eski sunucu) |
| `baskent_user-data_2602171048.zip` | BaskentEnerji kullanıcı verisi |
| `fresh_backups_20260320.zip` | 20 Mart 2026 canlı sistem yedeği |
| `moneytransfer.sql` | MTT MariaDB şema dump (eski) |
| `mtt_user-data_2602171048.zip.part1/2` | MTT büyük kullanıcı verisi (parçalanmış) |
| `mtturkey_exchange_2025-11-20.zip` | Exchange DB yedeği |
| `MoneyTransferTurkey_backup_20260616_193735.zip` | MTT tam yedek |
| `api-logs-archive/` | BaskentEnerji API stdout log arşivi |

---

## ⚠️ TEMİZLENECEK / ÇÖZÜLMEYECEK
| Klasör/Dosya | Durum |
|--------------|-------|
| `_publish_output/` | Eski publish çıktısı — kullanılmıyor, **silinebilir** |
| `deploy/publish-api/` | Eski publish — deploy-mtt-api yerine kullanılıyor mu kontrol et |
| `external/` | Sadece README var — içerik boş |

---

## 🗺️ PROJE / KLASÖR EŞLEŞTİRME ÖZETİ

```
baskent-enerji-doviz/
│
├── ── BASKENT-ENERJI ──────────────────────────────
│   ├── BaskentEnerji.API/        ← .NET 8 API
│   ├── BaskentEnerji.Business/   ← İş katmanı
│   ├── BaskentEnerji.Data/       ← EF Core
│   ├── BaskentEnerji.Entity/     ← Modeller
│   ├── BaskentEnerji.Tests/      ← Testler
│   ├── BaskentEnerji.sln         ← Solution
│   ├── dashboard/                ← Admin redirect
│   ├── login/                    ← Login sayfası
│   └── deploy/                   ← Build çıktıları
│
├── ── MONEY-TRANSFER-TURKEY ───────────────────────
│   ├── mtt-api/                  ← .NET 8 Minimal API
│   └── mtt-panels/               ← HTML paneller (Bootstrap)
│
├── ── TELEGRAM-BOT ────────────────────────────────
│   └── telegram-bot/             ← Python Flask + botlar
│
├── ── SCRIPTLER ───────────────────────────────────
│   └── scripts/
│       ├── baskent/              ← BaskentEnerji scriptleri
│       ├── mtt/                  ← MTT scriptleri
│       └── shared/               ← IIS, SQL, Backup (paylaşılan)
│
└── ── ORTAK ───────────────────────────────────────
    ├── docs/                     ← Dokümantasyon
    ├── backups/                  ← Yedekler
    ├── external/                 ← Harici import (boş)
    ├── README.md                 ← Ana proje readme
    └── PROJELER.md               ← Bu dosya
```
