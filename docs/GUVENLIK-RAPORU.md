# PENETRASYON & CYBER SECURITY TEST RAPORU

**Proje:** Başkent Enerji - Döviz Muhasebesi  
**Tarih:** 2026-02-23  
**Hedef:** baskentenerji.com / api.baskentenerji.com  
**Sunucu IP:** 45.84.191.180  
**Test Türü:** Black-box + White-box (kaynak kod erişimli)

---

## GENEL SKOR

| Kategori | Skor | Durum |
|----------|------|-------|
| Ağ Güvenliği | 2/10 | KRİTİK |
| Web Uygulama Güvenliği | 4/10 | YÜKSEK RİSK |
| API Güvenliği | 3/10 | KRİTİK |
| Kaynak Kod Güvenliği | 2/10 | KRİTİK |
| Veri Koruma | 1/10 | KRİTİK |
| **GENEL** | **2.4/10** | **KRİTİK** |

---

## KRİTİK BULGULAR (Acil Müdahale Gerekli)

### BULGU #1: appsettings.json İnternete Açık

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | https://baskentenerji.com/appcfg.txt |
| **Etki** | Veritabanı şifresi, JWT secret, email şifresi, Firebase private key TAM olarak ifşa |

**Detay:**  
`appcfg.txt` dosyası, projenin `appsettings.json` içeriğinin birebir kopyası. İçerikte:
- SQL Server bağlantı string'i (sunucu adresi, veritabanı adı, kullanıcı, **şifre**)
- JWT secret key (base64 encoded)
- Email hesap şifresi
- Firebase private key

**Kanıt:** HTTP 200, 3542 byte. İçerik `ConnectionStrings.SQL` ile başlıyor.

**Öneri:**
- [ ] **HEMEN** `appcfg.txt` dosyasını sunucudan sil
- [ ] Tüm şifreleri değiştir (DB, email, JWT key)
- [ ] Firebase service account key'i rotasyona al

---

### BULGU #2: Kaynak Kod / Deploy Paketleri İndirilebilir

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | baskentenerji.com |
| **Etki** | Uygulama kaynak kodu ve altyapı bilgileri saldırgana açık |

**Dosyalar:**
| Dosya | Boyut | İçerik |
|-------|-------|--------|
| `/dist-deploy.zip` | 1.66 MB | Frontend deploy paketi |
| `/ihtiyar-deploy.zip` | 1.66 MB | Frontend deploy paketi |
| `/index.zip` | 1.65 MB | Frontend deploy paketi |
| `/login_api.txt` | 2.4 KB | Login akışı kaynak kodu (minified JS) |
| `/apidir.txt` | 13.5 KB | API dizin yapısı listesi |
| `/scan.txt` | 123 KB | Dosya tarama raporu |
| `/scan2.txt` | 35.7 KB | Dosya tarama raporu |
| `/scan3.txt` | 4.5 KB | Asset dosya listesi |
| `/appcfg.txt` | 3.5 KB | Tam appsettings.json |
| `/fcheck.txt` | 108 B | Özellik kontrol dosyası |
| `/index2.html` | 818 B | Alternatif giriş sayfası |

**Öneri:**
- [ ] Tüm `.txt`, `.zip`, `.html` test/debug dosyalarını sil
- [ ] Web root'a erişimi sadece gereken dosyalarla sınırla

---

### BULGU #3: Veritabanı Portları İnternete Açık

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | 45.84.191.180 |
| **Etki** | Doğrudan veritabanı erişimi, ransomware, veri hırsızlığı |

**Açık Kritik Portlar (dışarıdan erişilebilir):**

| Port | Servis | Risk |
|------|--------|------|
| **1433** | **MSSQL** | KRİTİK - Veritabanına doğrudan bağlantı |
| **3306** | **MySQL** | KRİTİK - Veritabanına doğrudan bağlantı |
| **445** | **SMB** | KRİTİK - WannaCry/EternalBlue saldırıları |
| **5985** | **WinRM** | KRİTİK - Uzaktan komut çalıştırma |
| **21** | **FTP** | KRİTİK - Şifresiz dosya transferi |
| **3389** | **RDP** | YÜKSEK - Brute-force saldırıları |
| **8172** | **Plesk Deploy** | ORTA - Yönetim paneli |
| **8880** | **Plesk HTTP** | ORTA - Yönetim paneli |

**Öneri:**
- [ ] **HEMEN** Windows Firewall'da 1433, 3306, 445, 5985 portlarını kapat
- [ ] RDP'yi VPN arkasına al veya IP kısıtla
- [ ] FTP'yi devre dışı bırak, SFTP kullan
- [ ] Plesk portlarını IP kısıtla

---

### BULGU #4: Hardcoded Credentials (Kaynak Kod)

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | Kaynak kod (AnasıBerdus-Deniz) |
| **Etki** | Saldırgan kaynak koda ulaşırsa tüm sistemlere erişim |

**İfşa olan bilgiler:**

| Dosya | İçerik |
|-------|--------|
| `appsettings.json` | DB bağlantı şifresi |
| `appsettings.json` | JWT Secret Key |
| `appsettings.json` | Email şifresi |
| `appsettings.json` + `firebaseConfig.json` | Firebase private key |
| `BinanceService.cs:18-19` | Binance API Key + Secret |
| `HostService.cs:65` | Varsayılan admin şifresi: `123456asD!` |
| `CoinController.cs:65` | Hardcoded şifre: `123456789` |

**Öneri:**
- [ ] Tüm secret'ları environment variable'a taşı
- [ ] Azure Key Vault veya HashiCorp Vault kullan
- [ ] Binance API key'lerini rotasyona al
- [ ] Varsayılan admin şifresini değiştir

---

### BULGU #5: API Endpoint'leri Auth Gerektirmiyor

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | api.baskentenerji.com |
| **Etki** | Finansal verilere yetkisiz erişim |

**Auth'suz erişilebilen endpoint'ler:**

| Endpoint | Veri | Boyut |
|----------|------|-------|
| `/api/v1/Exchange/dashboard` | Toplam varlıklar, kar/zarar, şube detayları | 1949 B |
| `/api/v1/Exchange/currency` | Tüm para birimleri (26 adet, ID'ler dahil) | 5485 B |
| `/api/v1/Exchange` | Tüm döviz işlemleri (kur bilgileri dahil) | **50.6 KB** |
| `/api/v1/Exchange/vaults` | Kasa bilgileri | 2 B |
| `/api/v1/Coin` | Coin bilgileri | 2 B |

**İfşa olan hassas veriler (dashboard):**
- `totalAssets: 1,000,000` (toplam varlık)
- `monthlyProfit: 2,745.88` (aylık kar)
- Şube isimleri: ANA KASA, Kargıcak, Migros
- Şube GUID'leri ve varlık miktarları

**Kaynak kod teyidi:**
- `ExchangeController.cs` → `[Authorize]` attribute **YOK**
- `SiteController.cs` → `[Authorize]` attribute **YOK**
- `BlogController.cs` → `[Authorize]` **YORUM SATIRI** (disabled)

**Öneri:**
- [ ] Tüm controller'lara `[Authorize]` ekle
- [ ] Public olması gereken endpoint'ler için `[AllowAnonymous]` kullan
- [ ] Role-based authorization ekle

---

### BULGU #6: Zayıf Parola Hashleme

| Alan | Değer |
|------|-------|
| **Ciddiyet** | KRİTİK |
| **Konum** | UserServiceCommand.cs:184-196, HostService.cs:68-77 |
| **Etki** | Rainbow table saldırısıyla tüm şifreler kırılabilir |

**Mevcut yöntem:** SHA256 (salt yok)

**Öneri:**
- [ ] BCrypt.Net-Next veya ASP.NET Core Identity PasswordHasher kullan
- [ ] Tüm kullanıcı şifrelerini sıfırla ve yeni hash ile kaydet

---

## YÜKSEK SEVİYE BULGULAR

### BULGU #7: Rate Limiting Yok

| Alan | Değer |
|------|-------|
| **Ciddiyet** | YÜKSEK |
| **Konum** | api.baskentenerji.com/api/v1/user/login |
| **Etki** | Brute-force şifre kırma, credential stuffing |

**Test:** 10 ardışık başarısız login denemesi → hiçbiri engellenmedi (hepsi 400).

**Öneri:**
- [ ] AspNetCoreRateLimit paketi ekle
- [ ] IP bazlı rate limiting: 5 deneme/dakika
- [ ] Account lockout: 10 başarısız denemeden sonra kilitle

---

### BULGU #8: JWT Token Ömrü Çok Uzun

| Alan | Değer |
|------|-------|
| **Ciddiyet** | YÜKSEK |
| **Konum** | UserServiceCommand.cs:211 |
| **Etki** | Çalınan token 6 ay boyunca geçerli |

**Mevcut:** `DateTime.UtcNow.AddMonths(6)` (6 ay!)

**Öneri:**
- [ ] Access token: 15-60 dakika
- [ ] Refresh token mekanizması ekle
- [ ] Token revocation listesi oluştur

---

### BULGU #9: Private Firewall Profili Kapalı

| Alan | Değer |
|------|-------|
| **Ciddiyet** | YÜKSEK |
| **Konum** | Windows Firewall |
| **Etki** | İç ağ trafiği filtrelenmez |

| Profil | Durum |
|--------|-------|
| Domain | AKTİF |
| Private | **KAPALI** |
| Public | AKTİF |

**Öneri:**
- [ ] `Set-NetFirewallProfile -Profile Private -Enabled True`

---

### BULGU #10: Eski/Kullanılmayan Siteler Hala Aktif

| Alan | Değer |
|------|-------|
| **Ciddiyet** | YÜKSEK |
| **Konum** | IIS |
| **Etki** | Eski güvenlik açıkları üzerinden saldırı |

**IIS'de çalışan tüm siteler:**
- `baskentenerji.com` (aktif)
- `api.baskentenerji.com` (aktif)
- **`old.baskentenerji.com`** (GEREKLİ Mİ?)
- **`oldapi.baskentenerji.com`** (GEREKLİ Mİ?)
- `moneytransferturkey.com` (aktif)
- `tg.moneytransferturkey.com` (aktif)
- Default Web Site, pleskcontrolpanel, MailEnable WebMail, sitepreview, acme-challenge, https-redirect, 45.84.191.180

**Öneri:**
- [ ] `old.baskentenerji.com` ve `oldapi.baskentenerji.com` gereksizse durdur
- [ ] Default Web Site'ı kaldır
- [ ] IP bazlı site (45.84.191.180) FTP olarak kullanılıyor - değerlendir

---

## ORTA SEVİYE BULGULAR

### BULGU #11: HTTP Güvenlik Başlıkları Eksik

Her iki domain için de aşağıdaki güvenlik başlıkları eksik:

| Başlık | Durum | Risk |
|--------|-------|------|
| Strict-Transport-Security (HSTS) | EKSİK | MITM saldırısı |
| Content-Security-Policy (CSP) | EKSİK | XSS saldırısı |
| X-Content-Type-Options | EKSİK | MIME sniffing |
| X-Frame-Options | EKSİK | Clickjacking |
| X-XSS-Protection | EKSİK | XSS |
| Referrer-Policy | EKSİK | Bilgi sızıntısı |
| Permissions-Policy | EKSİK | Feature abuse |

**Öneri:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

---

### BULGU #12: RequireHttpsMetadata Kapalı

| Alan | Değer |
|------|-------|
| **Ciddiyet** | ORTA |
| **Konum** | Program.cs:128 |
| **Detay** | `options.RequireHttpsMetadata = false` |

**Öneri:**
- [ ] Production'da `RequireHttpsMetadata = true` yap

---

### BULGU #13: Dosya Yükleme Güvenliği Eksik

| Alan | Değer |
|------|-------|
| **Ciddiyet** | ORTA |
| **Konum** | MediaController.cs:82-108 |
| **Detay** | Dosya uzantı kontrolü var ama boyut limiti ve MIME doğrulaması yok |

**Öneri:**
- [ ] Maksimum dosya boyutu: 10 MB
- [ ] MIME type doğrulaması ekle
- [ ] Virüs taraması entegre et

---

### BULGU #14: SQL Browser Servisi Aktif

| Alan | Değer |
|------|-------|
| **Ciddiyet** | ORTA |
| **Konum** | Windows Services |
| **Detay** | SQL Server Browser servisi çalışıyor - instance keşfine olanak sağlıyor |

**Öneri:**
- [ ] `Stop-Service SQLBrowser; Set-Service SQLBrowser -StartupType Disabled`

---

### BULGU #15: Exception Handling Yetersiz

| Alan | Değer |
|------|-------|
| **Ciddiyet** | ORTA |
| **Konum** | ExceptionHandlingMiddleware.cs, CoinController.cs |
| **Detay** | Sadece ApiException handle ediliyor. CoinController'da hata durumunda stack trace veritabanına kaydediliyor. |

**Öneri:**
- [ ] Global exception handler tüm exception türlerini yakalasın
- [ ] Stack trace'leri DB'ye kaydetme, log dosyasına yaz

---

## DÜŞÜK SEVİYE BULGULAR

### BULGU #16: CORS Localhost Origin'leri

**Konum:** Program.cs:291-327  
**Detay:** Birçok localhost portu CORS whitelist'inde. Production'da gereksiz.

### BULGU #17: /health Endpoint 500 Dönüyor

**Konum:** api.baskentenerji.com/health  
**Detay:** Health check endpoint konfigüre edilmiş ama hata veriyor.

---

## OLUMLU BULGULAR

| Test | Sonuç |
|------|-------|
| SSL 3.0 | KAPALI |
| TLS 1.0 | KAPALI |
| TLS 1.1 | KAPALI |
| SQL Injection (basic) | DB hata mesajı yok |
| XSS Reflektif | Filtreleniyor |
| CORS evil origin | Reddediliyor |
| JWT sahte token | 401 reddediliyor |
| Swagger UI | Erişime kapalı |
| .env dosyası | Erişime kapalı |
| .git klasörü | Erişime kapalı |
| HTTP method (TRACE, PUT, DELETE) | Reddediliyor |
| Windows Update | 7 gün önce güncellendi |

---

## ACİL EYLEM PLANI (İlk 24 Saat)

### 1. Dosya Temizliği (10 dakika)
```powershell
# baskentenerji.com web root'undan silinecekler
$files = @("appcfg.txt","apidir.txt","login_api.txt","scan.txt","scan2.txt",
           "scan3.txt","fcheck.txt","index2.html","dist-deploy.zip",
           "ihtiyar-deploy.zip","index.zip")
$root = "C:\Inetpub\vhosts\baskentenerji.com\baskentenerji.com"
foreach ($f in $files) { Remove-Item "$root\$f" -Force -ErrorAction SilentlyContinue }
```

### 2. Firewall Kuralları (15 dakika)
```powershell
# Kritik portları kapat
New-NetFirewallRule -DisplayName "Block MSSQL External" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Block -Profile Any
New-NetFirewallRule -DisplayName "Block MySQL External" -Direction Inbound -LocalPort 3306 -Protocol TCP -Action Block -Profile Any
New-NetFirewallRule -DisplayName "Block SMB External" -Direction Inbound -LocalPort 445 -Protocol TCP -Action Block -Profile Any
New-NetFirewallRule -DisplayName "Block WinRM External" -Direction Inbound -LocalPort 5985 -Protocol TCP -Action Block -Profile Any
New-NetFirewallRule -DisplayName "Block FTP External" -Direction Inbound -LocalPort 21 -Protocol TCP -Action Block -Profile Any

# Private firewall aç
Set-NetFirewallProfile -Profile Private -Enabled True
```

### 3. Şifre Rotasyonu (30 dakika)
- [ ] SQL Server kullanıcı şifresi değiştir
- [ ] JWT secret key değiştir
- [ ] Email hesap şifresi değiştir
- [ ] Firebase key rotasyonu
- [ ] Binance API key rotasyonu
- [ ] Varsayılan admin şifresi değiştir
- [ ] Tüm kullanıcı oturumlarını sonlandır

### 4. API Authorization (1 saat)
- [ ] `ExchangeController.cs` → `[Authorize]` ekle
- [ ] `SiteController.cs` → `[Authorize]` ekle
- [ ] `BlogController.cs` → `[Authorize]` uncomment et
- [ ] `ExchangeAutoRateController.cs` → `[Authorize]` ekle
- [ ] Public olması gereken endpoint'lere `[AllowAnonymous]` ekle

### 5. SQL Browser Kapat
```powershell
Stop-Service SQLBrowser
Set-Service SQLBrowser -StartupType Disabled
```

---

## ORTA VADELİ EYLEM PLANI (1-2 Hafta)

| # | Görev | Öncelik |
|---|-------|---------|
| 1 | BCrypt parola hashleme geçişi | YÜKSEK |
| 2 | Rate limiting implementasyonu | YÜKSEK |
| 3 | JWT token ömrünü 1 saate düşür + refresh token | YÜKSEK |
| 4 | HTTP güvenlik başlıkları middleware | ORTA |
| 5 | Dosya yükleme boyut/MIME kontrolü | ORTA |
| 6 | Secret management (env vars / vault) | YÜKSEK |
| 7 | Global exception handler iyileştirmesi | ORTA |
| 8 | Eski siteleri (old.*) kaldır | ORTA |
| 9 | CORS whitelist temizliği (localhost kaldır) | DÜŞÜK |
| 10 | Health check endpoint düzelt | DÜŞÜK |
| 11 | RequireHttpsMetadata = true yap | DÜŞÜK |
| 12 | Penetrasyon testini 3 ayda bir tekrarla | DÜŞÜK |

---

## SONUÇ

Bu sunucu **ciddi güvenlik açıkları** barındırıyor. Özellikle:

1. **Veritabanı şifresinin internette açık olması** tek başına yıkıcı bir zafiyet
2. **DB portlarının dışarıya açık olması** ile birleştiğinde saldırgan doğrudan veritabanına bağlanabilir
3. **API endpoint'lerinin auth'suz erişilebilir olması** finansal verilerin ifşasına neden oluyor
4. **Deploy paketlerinin indirilebilir olması** saldırgana tam kaynak kod erişimi veriyor

**Acil müdahale için yukarıdaki "İlk 24 Saat" planını derhal uygulayın.**

---

*Rapor: Cursor AI Penetration Test Suite*  
*Test Tarihi: 2026-02-23*
