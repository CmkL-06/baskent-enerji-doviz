# Çalışma Planı – Güvenlik, İşlevsellik ve Son Sürüm Döviz Sistemi Uyumu

**Tarih:** 2026-02-24  
**Amaç:** Mevcut yapıyı güvenlik ve işlevsellik açısından geliştirmek; son sürüme yükseltilen döviz sistemine göre yenilemek ve optimize etmek.

**Referanslar:** TEK_PROJE_STRATEJISI.md, GUVENLIK-RAPORU.md, YAPILANDIRMA_AUDIT.md, PLESK_MONEYTRANSFERTURKEY_TG_PROTOCOL.md

---

## Mevcut Yapı Özeti

| Katman | Bileşen | Teknoloji / Konum |
|--------|---------|---------------------|
| **API** | api.baskentenerji.com | .NET 8, ExchangeOffice (Exchange, Vault, Dealer, Office, Party, AutoRate, Expense) |
| **Panel** | baskentenerji.com/ihtiyar | Vue 3, exchange-v2, kasa, Z raporu, cariler, şubeler, giderler, kullanıcılar |
| **Operatör** | tg.moneytransferturkey.com | Flask (port 5000), reverse proxy, api.baskentenerji.com ile entegrasyon |
| **Botlar** | Desktop\bot | bot2, ruble_channel_bot, web_operator_panel_v2 – BASKENT_API_URL ile exchange gönderimi |
| **Veritabanı** | mtt-moneyexchangeturkey | SQL Server (SQLEXPRESS), şema mtturkey_exchange |

**Son sürüm döviz özellikleri (hedef uyum):** Exchange işlemleri, kasa (Vault) yönetimi, bayi (Dealer) CRUD + QR, şube (Office) filtreleme, otomatik kur (AutoRate), gider tanımları, cari/parti hesapları, Z raporu, yetki (Owner/Admin/Personel + User_Office).

---

## FAZ 1 – GÜVENLİK (Öncelik: Kritik)

Hedef: GUVENLIK-RAPORU.md’deki kritik ve yüksek bulguları gidermek.

### 1.1 Hassas veri ve dosya temizliği

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.1.1 | Sunucudan test/debug dosyalarını kaldır | ☐ | appcfg.txt, dist-deploy.zip, ihtiyar-deploy.zip, index.zip, login_api.txt, apidir.txt, scan*.txt, fcheck.txt, index2.html |
| 1.1.2 | Web root’ta yalnızca gerekli dosyalar kalsın; .txt/.zip test dosyalarına erişim engellensin (web.config veya IIS requestFiltering) | ☐ | baskentenerji.com httpdocs |
| 1.1.3 | Canlı appsettings.json’ın web’den okunamadığını doğrula; API dizininde varsayılan belge/listing kapalı olsun | ☐ | api.baskentenerji.com |

### 1.2 Ağ ve port güvenliği

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.2.1 | Windows Firewall: MSSQL (1433), MySQL (3306), SMB (445), WinRM (5985) internetten erişime kapatılsın; sadece localhost/gerekli IP | ☐ | DB ve yönetim sadece iç ağ/VPN |
| 1.2.2 | RDP (3389): VPN arkasına al veya IP kısıtlaması | ☐ | |
| 1.2.3 | FTP (21) devre dışı veya SFTP + güçlü auth | ☐ | |
| 1.2.4 | Plesk (8172, 8880): Mümkünse IP kısıtla | ☐ | |

### 1.3 Secret yönetimi

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.3.1 | Tüm secret’ları ortam değişkenlerine veya Plesk “Environment variables”a taşı (ConnectionStrings, JwtSecretKey, Email password, Firebase) | ☐ | appsettings’te placeholder; canlıda env |
| 1.3.2 | Binance API key (varsa) rotasyon; hardcoded key kaldır | ☐ | BinanceService.cs |
| 1.3.3 | Varsayılan admin/seed şifreleri (HostService, CoinController) kaldır veya sadece dev ortamında kullan | ☐ | 123456asD!, 123456789 |
| 1.3.4 | .gitignore’da appsettings*.json, firebase*.json, .env, *.pfx kalıcı olsun; deploy’da canlı ayarlar korunsun | ☑ | Zaten deploy-api-to-canli.ps1 koruyor |

### 1.4 API yetkilendirme

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.4.1 | Exchange controller’lar (Exchange, Vault, Office, Dealer, Party, Expense, AutoRate, VaultSnapshot) üzerinde `[Authorize]` zorunlu olsun | ☑ | Sadece login/register/public endpoint’ler AllowAnonymous |
| 1.4.2 | Site/Blog controller: Auth gerektiren aksiyonlara `[Authorize]` ekle; public sayfalar için açıkça `[AllowAnonymous]` kullan | ☑ | |
| 1.4.3 | Rol tabanlı yetkilendirme (Owner/Admin/Personel) policy’leri netleştir; şube filtreleme (User_Office) korunsun | ☑ | ValidationService, EnsureOfficeAccessAsync mevcut |

### 1.5 Kimlik doğrulama ve parola

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.5.1 | Varsayılan parola doğrulama: BCrypt birincil; SHA256 yalnızca legacy geçiş için (mevcut yapı korunabilir) | ☑ | UserServiceCommand VerifyPassword BCrypt + SHA256 |
| 1.5.2 | Seed kullanıcılar (ihtiyar, Damat, PERSONEL, Perso) BCrypt veya dokümente edilmiş SHA256 ile tutarlı olsun | ☑ | SeedUsers_SHA256_Update.sql kullanılıyor |
| 1.5.3 | JWT süresi: 24 saat veya daha kısa; refresh token isteğe bağlı eklenebilir | ☐ | UserServiceCommand token expiry |

### 1.6 Rate limiting ve brute-force

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 1.6.1 | Login endpoint’inde rate limit (örn. 5–10/dakika/IP) zaten varsa politikayı gözden geçir; yoksa ekle | ☐ | Program.cs’te login policy var; pencere/sınır kontrolü |
| 1.6.2 | Başarısız giriş sayacı + geçici kilitleme (opsiyonel) | ☐ | |

---

## FAZ 2 – SON SÜRÜM DÖVİZ SİSTEMİNE UYUM VE İŞLEVSELLİK

Hedef: API, panel ve tg/botların son sürüm döviz modeli (Exchange, Vault, Dealer, Office, AutoRate, Party) ile tam uyumlu ve tutarlı çalışması.

### 2.1 API tarafı

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 2.1.1 | Exchange endpoint’lerinin route ve response modelleri dokümante edilsin; Swagger açıklamaları güncellensin | ☐ | /api/v1/Exchange/exchange, Vaults, Offices, Dealers vb. |
| 2.1.2 | Bot/Flask’ın kullandığı exchange gönderim endpoint’i (POST exchange) ile API controller path’i birebir eşleşsin | ☑ | /User/login büyük U ile düzeltildi; exchange path kontrolü |
| 2.1.3 | CORS: Sadece gerekli origin’ler (baskentenerji.com, tg.moneytransferturkey.com, moneytransferturkey.com, localhost dev) listelensin | ☑ | Program.cs güncel |
| 2.1.4 | API sürümleme: /api/v1 sabit; ileride v2 gerekirse route prefix ile ayrılsın | ☐ | Dokümantasyon |

### 2.2 İhtiyar paneli (Vue)

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 2.2.1 | Tüm API çağrıları base URL olarak api.baskentenerji.com/api/v1 kullansın; ortam değişkeni veya config ile yönetilsin | ☐ | Build’de env |
| 2.2.2 | Exchange V2, kasa, Z raporu, cariler, şubeler, giderler, kullanıcılar sayfaları son sürüm API response’larına göre test edilsin | ☐ | 404/500 hataları ve alan adları |
| 2.2.3 | Enhancement script’ler (ana-kasa-enhance, dealer-panel-enhance, security-guard, i18n-fix) minify ve cache-bust ile güncel kalsın | ☐ | index.html script sırası |
| 2.2.4 | Yetki: Owner tüm şubeleri, Admin/Personel sadece atanmış şubeleri görsün (backend ile uyumlu) | ☑ | Backend User_Office filtreli |

### 2.3 tg / Flask ve botlar

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 2.3.1 | .env: BASKENT_API_URL=https://api.baskentenerji.com/api/v1; login path /User/login (büyük U) | ☑ | Bot/Flask düzeltildi |
| 2.3.2 | Exchange gönderim: API’deki POST exchange endpoint URL’si ve body formatı (vault, currency, amount, rate vb.) dokümana göre doğrulansın | ☐ | send_exchange_to_baskent payload |
| 2.3.3 | Flask CORS ve session ayarları tg.moneytransferturkey.com ve HTTPS ile uyumlu | ☑ | allowed_origins güncel |
| 2.3.4 | Operatör panelinde kullanılan dealer/vault/office listesi API’den çekilsin; cache/refresh mantığı net olsun | ☐ | İsteğe bağlı iyileştirme |

### 2.4 Veritabanı ve şema

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 2.4.1 | Canlı DB: mtt-moneyexchangeturkey; şema mtturkey_exchange; connection string canlıda doğru ve deploy’da korunsun | ☑ | YAPILANDIRMA_AUDIT |
| 2.4.2 | Migration’lar güncel; Dealer, Vault, Office, Party, Exchange tabloları son sürüm entity’lerle uyumlu | ☐ | EF migration listesi kontrol |
| 2.4.3 | Seed data (ofis, vault, para birimi, dealer) dokümente edilsin; test ortamı için script varsa güncel kalsın | ☐ | |

---

## FAZ 3 – OPERASYONEL İYİLEŞTİRME VE SÜREKLİLİK

### 3.1 Plesk ve dağıtım

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 3.1.1 | Tüm domain/site’lar tek referans dokümanda (Plesk bütün optimizasyon) toplansın; document root, SSL, reverse proxy net | ☐ | PLESK_MONEYTRANSFERTURKEY_TG_PROTOCOL + baskentenerji özeti |
| 3.1.2 | GitHub ↔ local ↔ Plesk akışı: commit → push → (isteğe bağlı) deploy script; canlı appsettings/web.config hiç üzerine yazılmasın | ☑ | deploy-api-to-canli.ps1 |
| 3.1.3 | İhtiyar paneli deploy: Build çıktısı httpdocs\ihtiyar’a kopyalama adımı dokümante veya script’lensin | ☐ | Deploy adımı tek komutla standardize edilsin |

### 3.2 İzleme ve log

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 3.2.1 | API hata logları: Serilog veya ASP.NET Core logging; log dizini .gitignore’da; canlıda disk kotası | ☐ | |
| 3.2.2 | Kritik aksiyonlar (exchange, vault hareketi, kullanıcı değişikliği) audit log (DB veya dosya) isteğe bağlı | ☐ | |
| 3.2.3 | Flask/bot logları: Rotate ve boyut sınırı; hassas bilgi loglanmasın | ☐ | |

### 3.3 Yedekleme ve toparlanma

| # | Görev | Durum | Not |
|---|--------|--------|-----|
| 3.3.1 | DB yedekleme periyodu (günlük/haftalık) ve saklama süresi netleştirilsin | ☐ | |
| 3.3.2 | Canlı appsettings ve kritik config’lerin yedeklenmesi (şifreli veya güvenli dizin) | ☐ | Yedekler\Baskentenerji_Config_Yedek_* |
| 3.3.3 | Deploy geri alma: Önceki publish veya Git tag’den tekrar deploy adımları kısa dokümanda | ☐ | |

---

## FAZ 4 – KISA VE ORTA VADELİ GELİŞTİRMELER

| # | Öncelik | Görev |
|---|---------|--------|
| 4.1 | Orta | Refresh token veya “Beni hatırla” ile oturum süresi yönetimi |
| 4.2 | Orta | 2FA (TOTP) isteğe bağlı admin/owner hesapları için |
| 4.3 | Düşük | API için API key (bot/3. parti entegrasyon) ayrı auth yolu |
| 4.4 | Orta | Frontend (ihtiyar) için resmi env (VITE_API_BASE_URL vb.) ve tek build/deploy komutu |
| 4.5 | Düşük | Swagger’ı sadece geliştirme/staging’de açık; canlıda kapalı veya IP kısıtlı |

---

## Uygulama Sırası Özeti

1. **Faz 1 (güvenlik):** 1.1 → 1.2 → 1.3 → 1.4 → 1.5 → 1.6 (kritik dosya ve portlar hemen).
2. **Faz 2 (döviz uyumu):** 2.1 → 2.2 → 2.3 → 2.4 (API auth ve route’lar önce, sonra panel ve tg).
3. **Faz 3 (operasyonel):** 3.1 → 3.2 → 3.3 (Plesk/doc, log, yedek).
4. **Faz 4:** Önceliğe göre planlanır.

---

## Kontrol Listesi (Hızlı Referans)

- [ ] Test/debug dosyaları sunucudan kaldırıldı
- [ ] DB ve yönetim portları dışarıya kapatıldı
- [ ] Secret’lar env/key vault’a taşındı; hardcoded kalmadı
- [ ] Tüm Exchange/Office/Vault/Dealer endpoint’leri [Authorize] ile korunuyor
- [ ] JWT süresi makul (örn. ≤24 saat)
- [ ] Login rate limit aktif
- [ ] Panel ve tg son sürüm API ile test edildi
- [ ] GitHub ↔ local ↔ Plesk akışı dokümante ve deploy canlı ayarları koruyor
- [ ] Plesk tüm siteler tek dokümanda; tg reverse proxy ve SSL doğru

Bu plan, sistemin güvenlik ve işlevsellik açısından son sürüm döviz sistemine göre yenilenmesi ve sürdürülebilir şekilde yönetilmesi için referans alınacak ana çalışma planıdır.
