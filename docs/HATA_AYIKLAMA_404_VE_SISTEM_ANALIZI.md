# HTTP 404.0 – Hata Ayıklama ve Sistem Analizi (Geniş Perspektif)

**Tarih:** 2026-02-24  
**Amaç:** 404.0 hatasının kök nedenini bulmak ve tüm sistemi (IIS, API, tg, dosya yapısı) derinlemesine tarayıp yorumlamak.

---

## 1. HTTP 404.0 – Olası Nedenler (IIS / Windows)

| Neden | Açıklama | Kontrol |
|-------|----------|--------|
| **Yanlış belge kökü** | Site `httpdocs\public` gibi bir alt klasöre işaret ediyorsa, `/ihtiyar/` veya `/index.html` fiziksel yolda yoktur → 404. | IIS’te baskentenerji.com vdir → physicalPath **httpdocs** olmalı (public değil). |
| **Dosya gerçekten yok** | `index.html` veya `ihtiyar\index.html` silinmiş/taşınmış. | `httpdocs\index.html` ve `httpdocs\ihtiyar\index.html` var mı? |
| **Varsayılan belge kapalı** | Default document listesi boş veya index.html yok. | web.config’te `<defaultDocument><files>` içinde index.html tanımlı (mevcut). |
| **Rewrite sonrası dosya yok** | Kural `/ihtiyar/index.html`’e rewrite ediyor ama belge kökü farklıysa dosya bulunamaz. | Belge kökü = `...\httpdocs` ve `httpdocs\ihtiyar\index.html` mevcut olmalı. |
| **Yanlış site / binding** | İstek farklı bir siteye (örn. default web) gidiyor, orada yol yok. | Host header ve binding: baskentenerji.com için 80/443. |
| **Handler / izin** | Static file handler devre dışı veya klasör okuma izni yok. | IIS → Site → Handler Mappings; klasör izinleri (IIS_IUSRS okumalı). |

---

## 2. Hangi URL 404 Veriyor?

- **/** veya **/ihtiyar** → Büyük olasılıkla belge kökü veya eksik dosya.
- **/ihtiyar/dashboard** → SPA rewrite ile `ihtiyar/index.html`’e gider; o dosya yoksa 404.
- **/api/...** → API ayrı site (api.baskentenerji.com); yanlış host’ta /api açıyorsan 404.
- **/ihtiyar/assets/xxx.js** → Dosya yolu `httpdocs\ihtiyar\assets\...` olmalı; yoksa 404.

Aynı tarayıcıda tam hata veren **URL’yi** not edin (örn. `https://baskentenerji.com/ihtiyar`).

---

## 3. Yapılacak Kontroller (Sırayla)

### 3.1 baskentenerji.com – Belge kökü

**PowerShell (yönetici):**
```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" list vdir "baskentenerji.com/" /text:physicalPath
```
- **Beklenen:** `C:\Inetpub\vhosts\baskentenerji.com\httpdocs`
- **Eğer** `...\httpdocs\public` ise → 404 sebebi olabilir. Düzelt:
```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" set vdir "baskentenerji.com/" -physicalPath:"C:\Inetpub\vhosts\baskentenerji.com\httpdocs"
```

### 3.2 Dosya varlığı

**PowerShell:**
```powershell
$b = "C:\Inetpub\vhosts\baskentenerji.com\httpdocs"
Test-Path "$b\index.html"           # True olmali
Test-Path "$b\ihtiyar\index.html"  # True olmali
Get-ChildItem $b -Name              # index.html, ihtiyar, web.config vb. gorunmeli
```
- Biri **False** ise: eksik dosyayı kopyalayın veya yeniden deploy edin.

### 3.3 web.config – Rewrite sırası

- **Root-Redirect-To-Ihtiyar:** `^$|^/$` → `/ihtiyar/` redirect. Burada dosya aranmaz; redirect gider.
- **SPA-Ihtiyar:** `/ihtiyar/...` için dosya yoksa `/ihtiyar/index.html` rewrite.
- **SPA-Root:** Diğer tüm istekler (dosya/değilse) `/ihtiyar/index.html` rewrite.

Çakışma: Root kuralı redirect yapıyor; `/ihtiyar/` açıldığında istek `ihtiyar/index.html` veya dizin listesi. Dizin listesi kapalıysa ve `ihtiyar/index.html` yoksa 404. Yani **belge kökü ve dosya varlığı** en kritik nokta.

### 3.4 API (api.baskentenerji.com)

- 404 bu sitede ise: vdir physicalPath `...\api.baskentenerji.com` olmalı; içinde `web.config` ve `AnasıTAS-Deniz.API.dll` olmalı.
- **Swagger:** `https://api.baskentenerji.com/swagger` → 404 ise Swagger devre dışı veya yol farklı olabilir (yan etki).

### 3.5 tg.moneytransferturkey.com

- 404 bu sitede ise: document root `...\tg.moneytransferturkey.com` veya `...\tg.moneytransferturkey.com\public`; içinde **web.config** (reverse proxy kuralı) olmalı. Flask (5000) çalışmıyorsa proxy 502 verir, 404 değil; yine de kontrol edin.

---

## 4. Geniş Sistem Taraması – Kontrol Listesi

| Katman | Kontrol | Beklenen |
|--------|--------|----------|
| **IIS – baskentenerji.com** | Site state Started, vdir = httpdocs | Evet |
| **IIS – api.baskentenerji.com** | Site Started, vdir = api.baskentenerji.com, App Pool “Yönetilen Kod Yok” | Evet |
| **IIS – tg** | Site Started, vdir = tg...\public veya tg..., web.config rewrite var | Evet |
| **Dosya – httpdocs** | index.html, ihtiyar\index.html, ihtiyar\assets\*, web.config | Hepsi mevcut |
| **Dosya – API** | AnasıTAS-Deniz.API.dll, appsettings.json, web.config | Mevcut |
| **URL Rewrite** | Modül yüklü (IIS’te “URL Rewrite” ikonu) | Yüklü |
| **ARR (tg için)** | Application Request Routing → Proxy enabled | Etkin |
| **Varsayılan belge** | httpdocs için index.html | Tanımlı (web.config’te) |

---

## 5. Önerilen Tek Seferlik Düzeltme (404 için)

1. **Belge kökünü zorla httpdocs yap:** (Yukarıdaki `set vdir` komutu.)
2. **Eksik dosya varsa:** İhtiyar paneli build çıktısını `httpdocs\ihtiyar\` içine kopyalayın (index.html ve assets).
3. **App pool recycle:** baskentenerji.com(domain)(4.0)(pool) → Recycle.
4. **Tarayıcıda test:** `https://baskentenerji.com/` → `/ihtiyar/` yönlenmeli; `https://baskentenerji.com/ihtiyar/` → panel açılmalı.

---

## 6. Yorum ve Özet

- **404.0** bu yapıda büyük olasılıkla **yanlış belge kökü** (örn. httpdocs yerine httpdocs\public) veya **eksik index.html / ihtiyar klasörü** kaynaklıdır.
- Önce **fiziksel yol** ve **dosya varlığı** kontrol edilip düzeltildikten sonra rewrite kuralları anlamlı çalışır.
- Tüm sistemi “en geniş perspektifte” taramak için: IIS (siteler, vdir, app pool), dosya sistemi (httpdocs, api, tg), web.config (rewrite, defaultDocument), API canlı dizini ve tg proxy ayarı tek tek doğrulanmalı; bu dokümandaki tablolar ve komutlar bu taramayı yapmanızı sağlar.

Hangi tam URL’de 404 aldığınızı (örn. `https://baskentenerji.com/ihtiyar`) paylaşırsanız, bir sonraki adımda o URL’ye özel kısa bir teşhis listesi çıkarılabilir.

---

## 7. 24 Şubat – “Sayfa daha açılmıyor” Kontrol Sonuçları

**Yapılan kontroller:**

| Kontrol | Sonuç |
|--------|--------|
| vdir physicalPath | `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` ✓ |
| index.html | Var ✓ |
| ihtiyar\index.html | Var (1315 byte) ✓ |
| ihtiyar\assets\index-CnURhCj4.js | Var ✓ |
| Site state | Started ✓ |
| Binding’ler | http/80 ve https/443 (baskentenerji.com, www, ipv4) ✓ |
| **Sunucuda localhost testi** | **http://127.0.0.1/ (Host: baskentenerji.com) → 200** ✓ |
| **Sunucuda /ihtiyar/** | **200, "İhtiyarın Mekanı" içerik** ✓ |
| **Sunucuda SPA JS** | **200, 551826 byte** ✓ |

**Sonuç:** IIS ve dosya yapısı doğru. Sunucu **kendi üzerinden** (127.0.0.1 + Host: baskentenerji.com) isteği alınca sayfa düzgün dönüyor. Yani **“sayfa açılmıyor” sorunu sunucu içi yapılandırmadan kaynaklanmıyor.**

**Olası nedenler (sunucu dışı):**

1. **DNS** – baskentenerji.com sizin kullandığınız ağdan (ev/ofis) bu sunucunun IP’sine çözülmüyor olabilir. Kontrol: `nslookup baskentenerji.com` veya tarayıcıda açtığınızda adres çubuğunda hangi IP’ye gidiyor?
2. **Firewall** – Sunucuda veya arada (Plesk, hoster) 80/443 dışarıya kapalı olabilir. Sunucudan dış IP ile test: `Invoke-WebRequest -Uri "http://SUNUCU_DIS_IP/" -Headers @{ Host = "baskentenerji.com" } -UseBasicParsing` (SUNUCU_DIS_IP = sunucunun gerçek IP’si).
3. **SSL** – https kullanıyorsanız sertifika geçersiz/otomatik reddediliyorsa tarayıcı sayfayı açmaz. http://baskentenerji.com ile deneyin.
4. **Tarayıcı / cache** – Eski 404 önbelleğe alınmış olabilir. Gizli pencere veya farklı tarayıcı ile deneyin; gerekirse önbelleği temizleyin.
5. **Tam URL** – Hangi adresi açıyorsunuz? (örn. https://baskentenerji.com/ veya https://www.baskentenerji.com/ihtiyar/) Bunu bilmek teşhis için önemli.

**Sizin yapmanız gerekenler:**

- Tarayıcıda **tam açamadığınız URL’yi** ve mümkünse **hata mesajını** (404, “Bağlantı yok”, sertifika uyarısı vb.) not edin.
- Sunucuya RDP/SSH ile bağlanabiliyorsanız, **sunucunun kendisinde** bir tarayıcı açıp `http://baskentenerji.com` veya `http://localhost` (varsayılan site baskentenerji ise) deneyin. Sunucuda açılıyorsa sorun büyük ihtimalle DNS veya firewall’dadır.
- Domain’in bu sunucuya işaret ettiğinden emin olun (Plesk’te domain DNS / A kaydı).
