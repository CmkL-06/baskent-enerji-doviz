# Plesk Panel – moneytransferturkey.com ve tg Subdomain Yapılandırma Protokolü

**Tarih:** 2026-02-24  
**Amaç:** moneytransferturkey.com ana site ile tg subdomain’ini Plesk’te tek tip protokol ile yapılandırmak; API ve güvenlik ayarlarını netleştirmek.

---

## 1. Genel mimari

| Bileşen | Açıklama | Teknoloji |
|---------|----------|-----------|
| **moneytransferturkey.com** | Ana site | Node (Nuxt) – `httpdocs\public` |
| **tg.moneytransferturkey.com** | Operatör/Admin paneli | Flask (port 5000) – IIS reverse proxy |
| **api.baskentenerji.com** | Döviz/Exchange API | .NET 8 (baskentenerji domain’i) |

tg ve botlar, **api.baskentenerji.com** üzerinden Baskent Enerji API’ye bağlanır (login, exchange). moneytransferturkey.com ana sitesi gerekirse aynı API’yi CORS ile kullanabilir.

---

## 2. Plesk’te moneytransferturkey.com (ana site)

### 2.1 Hosting & DNS

| Ayar | Önerilen değer | Not |
|------|-----------------|-----|
| **Domain** | moneytransferturkey.com | Plesk → Domains listesinde |
| **Document root (Fiziksel yol)** | `C:\Inetpub\vhosts\moneytransferturkey.com\httpdocs\public` | Nuxt çıktısı (veya mevcut yapı) |
| **DNS** | A kaydı sunucu IP’sine (45.84.191.180 vb.) | Plesk DNS veya harici NS |

### 2.2 SSL/TLS

- **Let’s Encrypt** veya mevcut sertifika ile HTTPS açık olmalı.
- HTTP → HTTPS yönlendirmesi isteğe bağlı (Plesk’te “Permanent SEO-safe 301 redirect” açılabilir).

### 2.3 API kullanımı (ana site)

- Ana site (Nuxt) api.baskentenerji.com’a istek atacaksa:
  - **CORS:** API tarafında `https://moneytransferturkey.com` zaten izinli (Program.cs).
  - **Base URL:** `https://api.baskentenerji.com/api/v1` (login: `POST …/User/login`, diğer endpoint’ler aynı base altında).

---

## 3. Plesk’te tg.moneytransferturkey.com (subdomain)

### 3.1 Subdomain oluşturma / düzenleme

1. **Plesk** → **Domains** → **moneytransferturkey.com**.
2. **Subdomains** → **tg** (yoksa **Add Subdomain** → ad: `tg`).

### 3.2 Hosting & Document root

| Ayar | Değer | Zorunlu |
|------|--------|---------|
| **Document root** | `C:\Inetpub\vhosts\moneytransferturkey.com\tg.moneytransferturkey.com` | Evet |
| **Alternatif (Plesk public kullanıyorsa)** | `...\tg.moneytransferturkey.com\public` | Bu durumda `web.config` bu klasörde veya üst klasörde olmalı |

**Önemli:** Document root, **reverse proxy için kullanılan web.config** dosyasının bulunduğu klasör (veya altı) olmalı. IIS sitenin fiziksel yolu şu an `...\tg.moneytransferturkey.com\public` ise, `web.config` üst klasörde (`tg.moneytransferturkey.com`) ise IIS üst klasördeki config’i miras alır; yine de tek kaynak için **document root’u doğrudan `tg.moneytransferturkey.com`** (public olmadan) yapmak daha net olur.

### 3.3 tg için web.config (Reverse proxy + güvenlik)

Hedef klasör: `C:\Inetpub\vhosts\moneytransferturkey.com\tg.moneytransferturkey.com\web.config` (veya document root ise `public` ise `...\public\web.config`).

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

- **Rewrite:** Tüm istekler Flask’a (localhost:5000) proxy edilir.
- **customHeaders:** Güvenlik başlıkları (baskentenerji tarafındaki API protokolü ile uyumlu).

### 3.4 SSL/TLS (tg)

- **tg** için ayrı Let’s Encrypt sertifikası: Plesk → moneytransferturkey.com → Subdomains → tg → **SSL/TLS** → Let’s Encrypt.
- Veya ana domain sertifikası wildcard değilse, tg için ayrı cert gerekir.

### 3.5 IIS Application Request Routing (ARR)

- Reverse proxy çalışması için: **IIS Manager** → sunucu adı → **Application Request Routing** → **Server Proxy Settings** → **Enable proxy** işaretli olmalı.

---

## 4. tg tarafında API yapılandırması (Flask + botlar)

### 4.1 Kullanılan API

| Amaç | URL | Yöntem |
|------|-----|--------|
| Login | `https://api.baskentenerji.com/api/v1/User/login` | POST (Mail, Password) |
| Exchange gönderme | `https://api.baskentenerji.com/api/v1/ExchangeOffice/Exchange/...` | POST (Bearer token) |

### 4.2 Ortam değişkenleri (.env – bot / Flask paneli)

Aynı protokol tüm bileşenlerde kullanılmalı:

```env
BASKENT_API_URL=https://api.baskentenerji.com/api/v1
BASKENT_USERNAME=ihtiyar
BASKENT_PASSWORD=owner1
BASKENT_DEFAULT_VAULT_ID=<uuid>
BASKENT_USDT_CURRENCY_ID=<id>
BASKENT_RUBLE_CURRENCY_ID=<id>
```

- **BASKENT_API_URL** sonunda `/api/v1` olmalı; login path’i **/User/login** (büyük U, .NET route ile uyumlu).
- Flask ve botlarda login endpoint’i **/User/login** olacak şekilde güncellendi.

### 4.3 CORS (Flask panel)

Flask (`web_operator_panel_v2.py`) CORS’ta şunlar tanımlı:

- `https://tg.moneytransferturkey.com`, `http://tg.moneytransferturkey.com`
- `http://localhost:5000`, `http://127.0.0.1:5000`

API tarafında (api.baskentenerji.com) CORS’ta `https://tg.moneytransferturkey.com` zaten ekli.

---

## 5. Uygulama kontrol listesi (Plesk panelde)

- [ ] **moneytransferturkey.com** → Document root doğru (httpdocs\public veya mevcut Nuxt yapısı).
- [ ] **moneytransferturkey.com** → SSL açık, DNS A kaydı doğru.
- [ ] **tg** subdomain → Document root: `...\tg.moneytransferturkey.com` (veya public ise web.config yerleşimi doğru).
- [ ] **tg** → `web.config` reverse proxy + güvenlik başlıkları ile güncel.
- [ ] **tg** → SSL/TLS (Let’s Encrypt veya mevcut cert).
- [ ] **IIS ARR** → Proxy enabled.
- [ ] **Flask (port 5000)** çalışıyor (autostart.bat / bstart.bat).
- [ ] **.env** → BASKENT_API_URL, BASKENT_USERNAME, BASKENT_PASSWORD ve diğer BASKENT_* değişkenleri doğru; login path /User/login kullanılıyor.

---

## 6. Erişim URL’leri (özet)

| URL | Açıklama |
|-----|----------|
| https://moneytransferturkey.com | Ana site (Nuxt) |
| https://tg.moneytransferturkey.com/ | Operatör paneli giriş |
| https://tg.moneytransferturkey.com/login_up.php | Eski URL → /login’e yönlendirir |
| https://tg.moneytransferturkey.com/operator | Operatör paneli |
| https://tg.moneytransferturkey.com/admin | Admin paneli |
| https://api.baskentenerji.com | Baskent Enerji API (döviz; tg ve ana site buraya istek atar) |

Bu protokol, Plesk’te moneytransferturkey.com ve tg yapılandırmasının tekrarlanabilir ve tutarlı olması için kullanılır.
