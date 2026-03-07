# IIS Ayarları – Yapılandırma Rehberi

**Tarih:** 25.02.2026

---

## 1. Gerekli IIS bileşenleri

| Bileşen | Amaç |
|---------|------|
| **URL Rewrite** | baskentenerji.com `web.config` içindeki kurallar (/, /login → /ihtiyar/) için gerekli. Yüklü değilse site 500 veya kurallar çalışmaz. |
| **Default Document** | Ana sayfa (/) için index.html sunulması (veya web.config’teki Root-To-IndexHtml kuralı). |
| **ASP.NET 4.x** (baskentenerji.com) | App pool .NET 4.0 kullanıyorsa. |
| **.NET Core / No Managed Code** (api.baskentenerji.com) | API için. |

---

## 2. URL Rewrite modülü

- **Kurulum yapıldı:** URL Rewrite 2.1 (rewrite_amd64_en-US.msi) sessiz kurulum ile yüklendi; baskentenerji.com app pool recycle edildi. Yapılandırma: httpdocs\web.config içindeki rewrite kuralları kullanılıyor.
- **Kontrol:** IIS Yöneticisi → Sunucu → “URL Rewrite” ikonu görünüyor mu?
- **Yüklü değilse:** [URL Rewrite 2.1](https://www.iis.net/downloads/microsoft/url-rewrite) indirip kurun (Web Platform Installer veya doğrudan MSI).
- **Kurulum sonrası:** IIS’i veya ilgili app pool’u yeniden başlatın; baskentenerji.com `web.config` kuralları çalışır.

---

## 3. baskentenerji.com ayarları

| Ayar | Beklenen | Nereden |
|------|----------|--------|
| **Fiziksel yol** | `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` | IIS → baskentenerji.com → Temel Ayarlar → Fiziksel yol |
| **App Pool** | baskentenerji.com(domain)(4.0)(pool) veya Plesk’in atadığı pool | Site → Temel Ayarlar → Uygulama Havuzu |
| **Varsayılan belge** | index.html (web.config’te de tanımlı) | Site veya httpdocs → Varsayılan Belge |
| **Bağlamalar** | http/:80, https/:443 (baskentenerji.com, www, ipv4) | Site → Bağlamalar |

**Not:** Kök fiziksel yol **httpdocs** olmalı (httpdocs\public değil); `index.html` ve `ihtiyar` klasörü httpdocs altında.

---

## 4. api.baskentenerji.com ayarları

| Ayar | Beklenen |
|------|----------|
| **Fiziksel yol** | `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com` |
| **App Pool** | api.baskentenerji.com(domain)(pool) |
| **.NET CLR sürümü** | Yönetilen Kod Yok (ASP.NET Core) |

---

## 5. Plesk üzerinden kontrol

- **Web Sitesi ve Alan Adları** → **baskentenerji.com** → **Hosting Ayarları**:
  - **Belge kökü:** `httpdocs` (veya tam yol `...\httpdocs`).
- **Apache ve nginx Ayarları** kullanıyorsanız, IIS ile çakışmaması için Plesk’te “Proxy mode” / “IIS as backend” gibi seçenekleri kontrol edin.

---

## 6. Hızlı doğrulama (PowerShell)

Sunucuda çalıştırın:

```powershell
# Fiziksel yol
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "baskentenerji.com/" /text:physicalPath

# Site durumu
& "$env:windir\system32\inetsrv\appcmd.exe" list site "baskentenerji.com"
```

baskentenerji.com fiziksel yol çıktısı: `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` olmalı.

---

## 7. Özet

- **IIS ayarları yapılandırılmamış** deniyorsa: Önce **URL Rewrite** kurulu mu kontrol edin; sonra **baskentenerji.com** için fiziksel yol = **httpdocs**, varsayılan belge = **index.html** olduğundan emin olun. API için app pool “Yönetilen Kod Yok” ve yol = **api.baskentenerji.com** klasörü olmalı.
