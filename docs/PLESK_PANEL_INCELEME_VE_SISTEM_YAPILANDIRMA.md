# Plesk Panel – Derinlemesine İnceleme ve Sistem Yapılandırması

**Tarih:** 2026-02  
**Amaç:** Plesk panelini kapsamlı incelemek, hataları tespit edip düzeltmek, sistem konfigürasyonunu tek belgede toplamak.

---

## 1. Mevcut Sistem Özeti (Doğrulanan)

| Kontrol | Sonuç | Not |
|---------|--------|-----|
| **baskentenerji.com vdir** | `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` | Doğru |
| **api.baskentenerji.com vdir** | `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com` | Doğru |
| **baskentenerji.com site** | Started, 80/443 bindings | OK |
| **api.baskentenerji.com site** | Started, 80/443 bindings | OK |
| **Firewall 8443** | Plesk HTTPS, Plesk HTTPS 8443 (SMB Web) | Açık |
| **Firewall 80/443** | Plesk kuralları mevcut | Açık |
| **Port 8443** | LISTENING (Plesk panel) | Çalışıyor |

---

## 2. Plesk Panel Erişim ve SMB Web View

| Adres | Amaç |
|--------|------|
| **https://localhost:8443** | Plesk Obsidian giriş |
| **https://127.0.0.1:8443** | Aynı (yerel) |
| **https://45.84.191.180:8443** | Uzaktan (sunucu IP) |
| **https://localhost:8443/smb/web/view** | SMB Web View (dosya paylaşım yönetimi) |

**Sertifika uyarısı:** Self-signed ise tarayıcıda “Gelişmiş” → “adrese git” ile devam edin.

---

## 3. Derinlemesine İnceleme Kontrol Listesi

### 3.1 Plesk Panel İçi (Elle Yapılacak)

- [ ] **Giriş:** https://localhost:8443 → admin/abonelik kullanıcı adı ve şifre ile giriş.
- [ ] **Araçlar ve Ayarlar → Güvenlik:** Güvenlik duvarı kurallarında 8443, 80, 443 izinli mi?
- [ ] **Web Sitesi ve Alan Adları:** baskentenerji.com, api.baskentenerji.com listeleniyor mu?
- [ ] **baskentenerji.com → Hosting ve DNS:** Belge kökü **httpdocs** (public değil).
- [ ] **api.baskentenerji.com → Hosting ve DNS:** Belge kökü **api.baskentenerji.com** klasörü.
- [ ] **SSL/TLS:** Her domain için sertifika atanmış mı? (Let’s Encrypt veya mevcut cert.)
- [ ] **Git (varsa):** api.baskentenerji.com için “Deploy from repository” kullanılıyorsa appsettings/web.config korunuyor mu?

### 3.2 IIS ile Uyum (Plesk IIS kullanıyorsa)

- [ ] **App Pool – api.baskentenerji.com:** .NET CLR = **Yönetilen Kod Yok** (ASP.NET Core).
- [ ] **App Pool – baskentenerji.com:** .NET 4.0 veya No Managed Code (statik/Vue için).
- [ ] **URL Rewrite:** Modül yüklü mü? (baskentenerji.com web.config rewrite kuralları için.)
- [ ] **ARR (Application Request Routing):** tg reverse proxy için “Proxy” etkin mi?

### 3.3 Dosya ve İzinler

- [ ] **httpdocs:** index.html, ihtiyar\index.html, web.config mevcut mu?
- [ ] **api.baskentenerji.com:** web.config, AnasıTAS-Deniz.API.dll (veya güncel API DLL) mevcut mu?
- [ ] **Klasör izinleri:** IIS_IUSRS ve App Pool kimliği ilgili klasörlerde okuma (ve API’de çalıştırma) yetkisi var mı?

### 3.4 Yaygın Hatalar ve Düzeltmeler

| Hata | Olası sebep | Düzeltme |
|------|----------------|----------|
| 404 ana sayfa / ihtiyar | Belge kökü httpdocs\public | Plesk/IIS’te belge kökünü **httpdocs** yap |
| 404 API | vdir yanlış veya DLL yok | vdir = api.baskentenerji.com klasörü; publish çıktısını kopyala |
| 500 API giriş | JWT/DB veya eksik DLL | appsettings.json (ConnectionStrings, JwtSecretKey); Microsoft.IdentityModel.Protocols 7.x (bkz. proje) |
| Plesk 8443 açılmıyor | Firewall veya servis | Aşağıdaki PowerShell ile firewall kuralı ekle; Plesk servisini kontrol et |
| tg 502 | Flask (5000) kapalı veya ARR proxy kapalı | Flask’ı başlat; IIS’te ARR → Proxy etkin |

---

## 4. Sistem Konfigürasyonu (PowerShell / Sunucu)

### 4.1 Firewall – 8443 (Plesk) Zaten Açık

Mevcut kurallar: **Plesk HTTPS 8443 (SMB Web)**, **SECURITY: Allow Plesk HTTPS** (8443). Ek gerekiyorsa:

```powershell
netsh advfirewall firewall add rule name="Plesk HTTPS 8443 (SMB Web)" dir=in action=allow protocol=TCP localport=8443 profile=any
```

### 4.2 IIS vdir Doğrulama ve Düzeltme

**Kontrol:**
```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "baskentenerji.com/" /text:physicalPath
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "api.baskentenerji.com/" /text:physicalPath
```

**Beklenen:**  
- baskentenerji.com → `C:\Inetpub\vhosts\baskentenerji.com\httpdocs`  
- api.baskentenerji.com → `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com`

**Yanlışsa (örn. httpdocs\public):**
```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" set vdir "baskentenerji.com/" -physicalPath:"C:\Inetpub\vhosts\baskentenerji.com\httpdocs"
```

### 4.3 App Pool – API “Yönetilen Kod Yok”

IIS Yöneticisi → Uygulama Havuzları → **api.baskentenerji.com(domain)(pool)** → Gelişmiş Ayarlar → **.NET CLR Sürümü** = **Yönetilen Kod Yok**.

### 4.4 Dosya Varlığı Kontrolü

```powershell
$httpdocs = "C:\Inetpub\vhosts\baskentenerji.com\httpdocs"
$api = "C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com"
Test-Path "$httpdocs\index.html"           # True
Test-Path "$httpdocs\ihtiyar\index.html"   # True
Test-Path "$api\web.config"                 # True
Test-Path "$api\AnasıTAS-Deniz.API.dll"    # True (veya güncel DLL adı)
```

---

## 5. Plesk’te Yapılacak Sıra (Özet)

1. **Panele giriş:** https://localhost:8443 (veya sunucu IP:8443).
2. **baskentenerji.com:** Hosting → Belge kökü **httpdocs**; SSL atanmış olsun.
3. **api.baskentenerji.com:** Hosting → Belge kökü **api.baskentenerji.com**; SSL atanmış olsun.
4. **IIS uyumu:** API app pool “Yönetilen Kod Yok”; URL Rewrite yüklü.
5. **tg (varsa):** Subdomain tg → belge kökü tg.moneytransferturkey.com; web.config reverse proxy; ARR Proxy etkin.
6. **Güvenlik:** Plesk Güvenlik Duvarı’nda 80, 443, 8443 izinli; gerekirse ek kural (yukarıdaki netsh).

---

## 6. İlgili Belgeler

| Belge | İçerik |
|--------|--------|
| **PLESK_TUM_YAPILANDIRMA_TAMAMLAMA.md** | Domain/subdomain adım adım (baskentenerji, api, moneytransferturkey, tg) |
| **PLESK_DESKTOP_ERISIM.md** | Plesk erişim adresleri, firewall, SMB Web View |
| **IIS_AYARLARI_YAPILANDIRMA.md** | URL Rewrite, vdir, app pool |
| **HATA_AYIKLAMA_404_VE_SISTEM_ANALIZI.md** | 404 nedenleri ve düzeltmeler |
| **PLESK_MONEYTRANSFERTURKEY_TG_PROTOCOL.md** | tg subdomain ve reverse proxy |

---

## 7. Hızlı Teşhis Komutları

```powershell
# Port 8443 dinleniyor mu?
netstat -ano | findstr ":8443"

# Site durumları
& "$env:windir\system32\inetsrv\appcmd.exe" list site

# vdir yolları
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "baskentenerji.com/" /text:physicalPath
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "api.baskentenerji.com/" /text:physicalPath
```

Bu belge, Plesk panel incelemesi ve sistem konfigürasyonunu tek yerden yönetmenizi sağlar. Hata gördüğünüzde ilgili bölüm ve tablolardan düzeltmeyi uygulayın.
