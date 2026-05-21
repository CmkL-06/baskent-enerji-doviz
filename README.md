# Başkent Enerji & Money Transfer Turkey — Ana Proje Deposu

> **Kanonik Branch:** `BASKENT-DOVIZ` | **Son Güncelleme:** 21 Mayıs 2026 | **Durum:** Aktif Geliştirme
>
> Bu depo, **Başkent Enerji Döviz Muhasebe Sistemi** ve **Money Transfer Turkey** projelerinin tüm kaynak kodlarını, canlı sistem yedeklerini, otomasyon araçlarını ve teknik dokümantasyonunu tek çatıda birleştiren ana merkezdir.
>
> ---
>
> ## 🌐 Canlı Ortam URL'leri
>
> | Servis | URL | Durum |
> |---|---|---|
> | Yönetim Paneli | https://baskentenerji.com/ihtiyar | Vue 3 Frontend |
> | REST API | https://api.baskentenerji.com/api/v1 | .NET 8 Backend |
> | Operatör Paneli | https://tg.moneytransferturkey.com | Flask + IIS Proxy |
> | Ana Site | https://moneytransferturkey.com | Nuxt.js |
> | API Health | https://api.baskentenerji.com/health | 200 OK |
>
> ---
>
> ## 🏗️ Sistem Mimarisi
>
> ```
> ┌─────────────────────────────────────────────────────────────────┐
> │                    BAŞKENT ENERJİ SİSTEMİ                       │
> ├──────────────────┬──────────────────┬──────────────────────────┤
> │   FRONTEND       │   BACKEND (API)  │   OPERASYON              │
> │                  │                  │                          │
> │ baskentenerji    │ api.baskent      │ tg.moneytransfer         │
> │ .com/ihtiyar     │ enerji.com       │ turkey.com               │
> │                  │                  │                          │
> │ Vue 3 + Vite     │ .NET 8 Web API   │ Flask (port 5000)        │
> │ Exchange V2      │ JWT Auth         │ IIS Reverse Proxy        │
> │ Kasa, Z-Rapor    │ EF Core          │ Telegram Botları         │
> │ Bayi Paneli      │ MSSQL Server     │ Python 3                 │
> └──────────────────┴──────────────────┴──────────────────────────┘
>                             │
>                    ┌────────┴────────┐
>                    │   VERİTABANI   │
>                    │ mtt-moneyexch  │
>                    │ angeturkey     │
>                    │ şema:          │
>                    │ mtturkey_exch  │
>                    │ ange           │
>                    └────────────────┘
> ```
>
> ---
>
> ## 📦 Proje Yapısı (Kanonik)
>
> ```
> baskent-enerji-doviz/
> ├── BaskentEnerji.API/          # .NET 8 Web API (KANONİK BACKEND)
> │   ├── Controllers/
> │   │   ├── ExchangeOffice/     # Exchange, Vault, AutoRate, VaultSnapshot
> │   │   ├── Site/               # Page, Menu, Theme, SEO, Media, Slider, Tag
> │   │   ├── Blog/               # Blog yönetimi
> │   │   ├── Coin/               # Kripto takip
> │   │   ├── DiagnosticsController.cs
> │   │   └── UserController.cs   # JWT Auth
> │   ├── HostedServices/
> │   ├── Features/
> │   ├── Services/
> │   ├── Program.cs              # 415 satır — JWT, CORS, Swagger, Rate Limit
> │   └── BaskentEnerji.API.csproj
> ├── BaskentEnerji.Business/     # İş Mantığı Katmanı
> ├── BaskentEnerji.Data/         # EF Core DbContext (mtturkey_exchange)
> ├── BaskentEnerji.Entity/       # Entity / DTO / Request / Response modelleri
> ├── BaskentEnerji.Tests/        # Test Katmanı
> ├── MoneyTransferTurkey/        # LEGACY — Sadece referans, aktif geliştirme yok
> ├── telegram-bot/               # Python Telegram botları
> │   ├── main_bot.py
> │   ├── operator_bot.py
> │   ├── ruble_bot.py
> │   ├── exchange_rates.py
> │   ├── baskent_api.py
> │   └── run_all.py
> ├── deploy/                     # Derlenmiş publish paketleri
> │   ├── publish-api/            # Windows publish
> │   └── publish-api-linux/      # Linux publish
> ├── dashboard/                  # Frontend build çıktısı (index.html redirect)
> ├── backups/                    # SQL yedekleri (.bak, .sql, .dump)
> ├── docs/                       # Teknik dokümantasyon (bkz. aşağıda)
> ├── scripts/                    # Otomasyon ve test scriptleri
> ├── login/                      # Login sayfası dosyaları
> ├── index.html                  # Root redirect → /ihtiyar/
> ├── bayi-paneli-gelistirme.js   # Bayi paneli enhance script
> ├── i18n-fix.js                 # Çoklu dil fix
> └── BaskentEnerji.sln           # Visual Studio Solution
> ```
>
> ---
>
> ## 🚀 Hızlı Başlangıç
>
> ### Backend API (Lokal)
> ```bash
> cd BaskentEnerji.API
> dotnet restore
> dotnet run
> # API: http://localhost:5093
> # Swagger: http://localhost:5093/swagger
> # Health: http://localhost:5093/health
> ```
>
> ### Build ve Doğrulama
> ```bash
> dotnet build BaskentEnerji.API/BaskentEnerji.API.csproj -c Release --no-restore
> curl http://localhost:5093/health
> bash scripts/api_tooling_smoke.sh
> ```
>
> ### Telegram Botları
> ```bash
> cd telegram-bot
> pip install -r requirements.txt
> cp .env.example .env  # API tokenlarını doldurun
> python run_all.py
> ```
>
> ### Deploy (Canlı Sunucu)
> ```powershell
> # Windows — Plesk sunucusu
> .\deploy\deploy-api-to-canli.ps1
>
> # Linux — cPanel/GoDaddy hosting
> bash scripts/package_single_project.sh
> ```
>
> ---
>
> ## 🔑 Teknoloji Yığını
>
> | Katman | Teknoloji | Versiyon |
> |---|---|---|
> | Backend API | .NET / ASP.NET Core | 8.0 |
> | ORM | Entity Framework Core | 8.0.6 |
> | Auth | JWT Bearer | 8.0.6 |
> | API Dok. | Swashbuckle (Swagger) | 6.4.0 |
> | Veritabanı | MSSQL Server (SQLEXPRESS) | — |
> | Frontend Panel | Vue 3 + Vite | — |
> | Operatör Paneli | Python Flask | — |
> | Botlar | Python 3 + python-telegram-bot | — |
> | Hosting | GoDaddy cPanel (alanyayatirim.com) | — |
> | Canlı Sunucu | Plesk (Windows/IIS) | — |
>
> ---
>
> ## 🔐 Güvenlik
>
> - JWT kimlik doğrulama — Tüm exchange/vault/dealer endpoint'leri `[Authorize]` korumalı
> - - Rol tabanlı yetkilendirme: `Owner`, `Admin`, `Personel`
>   - - Şube filtresi: Kullanıcı sadece atandığı şubeyi (User_Office) görebilir
>     - - BCrypt şifre hash'leme (SHA256 yalnızca legacy geçiş için)
>       - - Rate limiting: Login endpoint'inde aktif
>         - - Secret yönetimi: `.env` ve ortam değişkenleri — repo'da düz metin tutulmaz
>           - - CORS: Yalnızca yetkili origin'ler (baskentenerji.com, tg.moneytransferturkey.com, localhost)
>            
>             - > ⚠️ Bu depo hassas veriler (API keyler, DB bağlantıları) içerebilir. Yalnızca yetkili personel erişmeli.
>               >
>               > ---
>               >
>               > ## 📊 Platform Durumu (21 Mayıs 2026)
>               >
>               > | Platform | Durum | Notlar |
>               > |---|---|---|
>               > | GitHub (BASKENT-DOVIZ branch) | ✅ Aktif | Ana geliştirme branch |
>               > | Replit (baskent-enerji-doviz) | ⚠️ Kredi bitti | Frontend geliştirme ortamı, Upgrade gerekli |
>               > | GoDaddy cPanel (alanyayatirim.com) | ✅ Çalışıyor | Disk: 4GB/50GB, SSL yenileme gerekli |
>               > | baskentenerji.com | ⚠️ Uygulama kurulmamış | cPanel'de deploy bekleniyor |
>               > | api.baskentenerji.com | ⚠️ Uygulama kurulmamış | .NET API deploy bekleniyor |
>               > | baskentdanismanlik.com | ⚠️ Uygulama kurulmamış | Deploy bekleniyor |
>               > | Plesk (canlı sunucu) | ✅ Çalışıyor | api.baskentenerji.com + tg subdomain aktif |
>               > | GoDaddy SSL | ⚠️ Yakında sona erecek | Otomatik yenileme başarısız |
>               >
>               > ---
>               >
>               > ## 📚 Dokümantasyon (docs/)
>               >
>               > | Dosya | Açıklama |
>               > |---|---|
>               > | `TEK_PROJE_STRATEJISI.md` | Kanonik proje sınırı ve release kapsamı |
>               > | `FINAL_KANONIK_YAPI.md` | Final güçlü yapı ve bileşen entegrasyon standardı |
>               > | `CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md` | Güvenlik ve operasyon yol haritası (Faz 1–4) |
>               > | `DURUM_ANALIZI_20260307.md` | En güncel durum özeti |
>               > | `REFERANS_VERITABANI_VE_YAPILANDIRMA.md` | Veritabanı ve yapılandırma referansı |
>               > | `PLESK_MONEYTRANSFERTURKEY_TG_PROTOCOL.md` | Plesk + API + Telegram entegrasyon protokolü |
>               > | `ESKI_SUNUCU_VERI_REFERANSI.md` | Eski sunucu verisini silmeden kullanma politikası |
>               >
>               > ---
>               >
>               > ## 🛠️ Otomasyon Scriptleri (scripts/)
>               >
>               > | Script | Açıklama |
>               > |---|---|
>               > | `single_project_audit.py` | Yapısal/güvenlik drift kontrolü |
>               > | `api_tooling_smoke.sh` | Health + kritik route smoke testi |
>               > | `package_single_project.sh` | Release paketleme |
>               > | `Baskentenerji_API_Test.ps1` | PowerShell API test |
>               > | `Test-API-AfterDeploy.ps1` | Deploy sonrası doğrulama |
>               > | `IIS-API-AppPool-Optimize.ps1` | IIS optimizasyon |
>               > | `SeedUsers_SHA256_Update.sql` | Kullanıcı seed SQL |
>               >
>               > ---
>               >
>               > ## 🔄 Geliştirme Akışı
>               >
>               > ```
>               > Yerel Geliştirme (VS Code / Rider)
>               >          │
>               >          ▼
>               >     git push → GitHub (BASKENT-DOVIZ branch)
>               >          │
>               >          ▼
>               >   deploy-api-to-canli.ps1
>               >          │
>               >          ▼
>               > Plesk Canlı Sunucu (api.baskentenerji.com)
>               > ```
>               >
>               > > ⚠️ Deploy sırasında canlı `appsettings.json` ve `web.config` **asla üzerine yazılmaz**.
>               > >
>               > > ---
>               > >
>               > > ## 📋 Açık Görevler (Öncelik Sırasına Göre)
>               > >
>               > > ### 🔴 Kritik
>               > > - [ ] GoDaddy SSL sertifikası yenileme (otomatik yenileme başarısız)
>               > > - [ ] - [ ] `baskentenerji.com` ve `api.baskentenerji.com` için GoDaddy cPanel'e uygulama kurulumu
>               > > - [ ] - [ ] JWT token süresi ≤24 saat olarak ayarlanması
>               > >
>               > > - [ ] ### 🟡 Yüksek Öncelik
>               > > - [ ] - [ ] Login rate limit penceresi ve sınırı gözden geçirme
>               > > - [ ] - [ ] `BaskentEnerji.Tests` için NuGet CI/yerel cache stratejisi (NU1301 hatası)
>               > > - [ ] - [ ] Panel build'i için `VITE_API_BASE_URL` env değişkeni standartlaştırma
>               > > - [ ] - [ ] İhtiyar panel deploy scripti tek komutla otomatize etme
>               > >
>               > > - [ ] ### 🟢 Orta Öncelik
>               > > - [ ] - [ ] Refresh token veya "Beni Hatırla" özelliği
>               > > - [ ] - [ ] Swagger'ı sadece dev/staging'de açık tutma
>               > > - [ ] - [ ] Migration listesi güncelliği (Dealer, Vault, Office, Party tabloları)
>               > > - [ ] - [ ] Replit kredi yükseltme veya alternatif geliştirme ortamı
>               > >
>               > > - [ ] ---
>               > >
>               > > - [ ] ## 💾 Yedekler
>               > >
>               > > - [ ] - `backups/fresh_backups_20260320.zip` — 20 Mart 2026 canlı sistem yedeği
>               > > - [ ] - `backups/mtt_user-data_*.zip.part1/2` — Büyük kullanıcı veri yedekleri (parçalanmış)
>               > > - [ ] - SQL yedekleri: `.bak`, `.sql`, `.dump` formatında
>               > >
>               > > - [ ] ---
>               > >
>               > > - [ ] ## 🌍 İlgili Repolar ve Platformlar
>               > >
>               > > - [ ] | Platform | URL / Açıklama |
>               > > - [ ] |---|---|
>               > > - [ ] | **Replit** | https://replit.com/@kulemlakyatirim/baskent-enerji-doviz |
>               > > - [ ] | **GoDaddy cPanel** | sxb1plzcpnl490361.prod.sxb1.secureserver.net:2083 |
>               > > - [ ] | **GoDaddy Hosting** | host.godaddy.com (alanyayatirim.com hesabı) |
>               > > - [ ] | **GoDaddy Airo Builder** | airo-builder.godaddy.com/develop/z2sd91mef0 |
>               > >
>               > > - [ ] ---
>               > >
>               > > - [ ] *Son güncelleme: 21 Mayıs 2026 — Tüm platformlar gözden geçirildi ve durum tablosu güncellendi.*
