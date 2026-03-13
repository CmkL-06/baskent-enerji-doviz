# Plesk Panel – Tüm Yapılandırmaları Tamamlama Rehberi

**Tarih:** 2026-02-24  
**Amaç:** Plesk üzerinden baskentenerji.com, api.baskentenerji.com, moneytransferturkey.com ve tg subdomain’inin hosting, SSL ve gerekli ayarlarını tek rehberde tamamlamak.

---

## Genel sıra

1. baskentenerji.com (ana site + ihtiyar paneli)  
2. api.baskentenerji.com (döviz API)  
3. moneytransferturkey.com (ana site)  
4. tg.moneytransferturkey.com (operatör paneli, reverse proxy)  
5. Son kontroller  

---

## 1. baskentenerji.com

### 1.1 Plesk’te domain’i açma

- **Plesk** → **Web Sitesi ve Alan Adları** (veya **Domains**) → **baskentenerji.com** tıklayın.

### 1.2 Hosting & DNS

- **Hosting ve DNS** (veya **Hosting Settings**) bölümüne girin.
- **Belge kökü (Document root):**
  - Şu an sunucuda: `httpdocs\public`
  - **Olması gereken:** `httpdocs` (ihtiyar paneli `httpdocs\ihtiyar` altında; ana sayfa `httpdocs\index.html`).
- Değiştiriyorsanız: Belge kökünü **httpdocs** yapın (veya tam yol: `C:\Inetpub\vhosts\baskentenerji.com\httpdocs`) → **Kaydet**.
- **Varsayılan belge:** `index.html` tanımlı olsun.

### 1.3 SSL/TLS

- **SSL/TLS Sertifikaları** (veya **SSL/TLS**) bölümüne girin.
- **Let’s Encrypt** ile sertifika alın veya mevcut sertifikayı kullanın.
- **Güvenli bağlantıyı zorunlu kıl (HTTPS yönlendirmesi)** isteğe bağlı açılabilir.

### 1.4 Kontrol

- Tarayıcıda `https://baskentenerji.com/` → ana sayfa, ardından giriş/ihtiyar yönlendirmesi.
- `https://baskentenerji.com/ihtiyar/` → panel açılıyor olmalı.

---

## 2. api.baskentenerji.com

### 2.1 Plesk’te domain’i açma

- **Web Sitesi ve Alan Adları** → **api.baskentenerji.com** tıklayın.

### 2.2 Hosting ayarları

- **Hosting ve DNS** → **Belge kökü:**
  - **Olması gereken:** `api.baskentenerji.com` (yani `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com`).
- Plesk’te genelde alan adına özel bir klasör adı kullanılır; fiziksel yol sunucuda bu klasöre işaret etmeli.

### 2.3 Uygulama ayarları (ASP.NET Core)

- **Uygulama Havuzu** veya **IIS Ayarları** (Plesk’te “IIS” / “Application Pool” varsa):
  - **.NET CLR sürümü:** **Yönetilen Kod Yok** (ASP.NET Core için).
  - Uygulama havuzu adı: örn. `api.baskentenerji.com(domain)(pool)`.
- API klasöründe `web.config` ve `BaskentEnerji.API.dll` (veya güncel API DLL’i) mevcut olmalı.

### 2.4 SSL/TLS

- **SSL/TLS** → api.baskentenerji.com için sertifika atayın (Let’s Encrypt veya mevcut cert).

### 2.5 Kontrol

- `https://api.baskentenerji.com/api/v1/User/login` → POST ile `{"Mail":"ihtiyar","Password":"owner1"}` → 200 + token.

---

## 3. moneytransferturkey.com

### 3.1 Plesk’te domain’i açma

- **Web Sitesi ve Alan Adları** → **moneytransferturkey.com** tıklayın.

### 3.2 Hosting ayarları

- **Belge kökü:** Nuxt/Node yapısına göre genelde `httpdocs\public` (veya mevcut yapı ne ise onu koruyun).
- DNS A kaydı sunucu IP’sine işaret etmeli (Plesk DNS veya harici NS).

### 3.3 SSL/TLS

- **SSL/TLS** → moneytransferturkey.com için sertifika atayın.

---

## 4. tg.moneytransferturkey.com (subdomain)

### 4.1 Subdomain’i açma / düzenleme

- **Web Sitesi ve Alan Adları** → **moneytransferturkey.com** tıklayın.
- **Alt alan adları (Subdomains)** → **tg** satırını bulun (yoksa **Alt alan adı ekle** → ad: **tg**).

### 4.2 Hosting – belge kökü

- **tg** → **Hosting ayarları** (veya **Hosting and DNS**).
- **Belge kökü:**
  - **Önerilen:** `tg.moneytransferturkey.com` (tam yol: `C:\Inetpub\vhosts\moneytransferturkey.com\tg.moneytransferturkey.com`).
  - Plesk “public” kullanıyorsa: `tg.moneytransferturkey.com\public` da olabilir; bu durumda `web.config` bu klasörde veya üst klasörde olmalı.

### 4.3 web.config (reverse proxy)

Sunucuda aşağıdaki dosya mevcut olmalı ve içeriği buna uygun olmalı:

**Dosya:** `C:\Inetpub\vhosts\moneytransferturkey.com\tg.moneytransferturkey.com\web.config`  
(veya document root `public` ise: `...\tg.moneytransferturkey.com\public\web.config`)

**İçerik (Flask’a proxy):**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="Proxy to Flask" stopProcessing="true">
          <match url="(.*)" />
          <action type="Rewrite" url="http://127.0.0.1:5000/{R:1}" />
        </rule>
      </rules>
    </rewrite>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="X-XSS-Protection" value="1; mode=block" />
        <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
      </customHeaders>
    </httpProtocol>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="10485760" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
```

- **URL Rewrite** ve **Application Request Routing (ARR)** sunucuda yüklü olmalı; ARR’de **Proxy** etkin olmalı (IIS Yöneticisi → Sunucu → Application Request Routing → Server Proxy Settings → Enable proxy).

### 4.4 SSL/TLS (tg)

- **tg** alt alan adı → **SSL/TLS** → Let’s Encrypt ile sertifika alın veya ana domain sertifikasını kullanın (wildcard değilse tg için ayrı cert gerekir).

### 4.5 Kontrol

- Flask (port 5000) çalışıyorsa: `https://tg.moneytransferturkey.com/` → operatör paneli girişi.
- Çalışmıyorsa: `Desktop\bot\autostart.bat` veya `bstart.bat` ile Flask’ı başlatın.

---

## 5. Son kontroller (özet)

| Kontrol | Nerede | Beklenen |
|---------|--------|----------|
| baskentenerji.com belge kökü | Plesk → baskentenerji.com → Hosting | **httpdocs** |
| api.baskentenerji.com belge kökü | Plesk → api.baskentenerji.com → Hosting | **api.baskentenerji.com** klasörü |
| api App Pool | IIS / Plesk | Yönetilen Kod Yok (ASP.NET Core) |
| tg belge kökü | Plesk → moneytransferturkey.com → Subdomains → tg | **tg.moneytransferturkey.com** (veya public) |
| tg web.config | Sunucu dosya yolu | Reverse proxy 127.0.0.1:5000, güvenlik başlıkları |
| SSL | Tüm domain/subdomain | Sertifika atanmış, HTTPS açılır |
| ARR Proxy | IIS → Application Request Routing | Etkin |

---

## 6. Plesk’te yapılamayanlar (IIS / sunucu tarafı)

Aşağıdakiler Plesk arayüzünde olmayabilir; gerekirse **IIS Yöneticisi** veya **sunucu dosya sistemi** ile yapılır:

- **baskentenerji.com fiziksel yolunu httpdocs yapmak:** Plesk’te “Belge kökü” alanına `httpdocs` yazıp kaydetmek genelde yeterli; değişmezse IIS’te site → Temel Ayarlar → Fiziksel yol.
- **tg web.config:** Plesk Dosya Yöneticisi veya RDP ile ilgili klasöre eklenir/düzenlenir.
- **ARR Proxy:** IIS Yöneticisi → Sunucu → Application Request Routing → Server Proxy Settings.

Bu rehberi Plesk’te sırayla uygulayarak tüm yapılandırmaları tamamlayabilirsiniz.
