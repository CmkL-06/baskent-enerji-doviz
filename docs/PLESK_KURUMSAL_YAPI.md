# Plesk Panel – Kurumsal Yapı

**Amaç:** Plesk’te domain, belge kökü ve güvenlik ayarlarını kurumsal standarda göre düzenlemek.

---

## 1. Domain – klasör eşlemesi (standart)

| Domain / Subdomain | Belge kökü (Document root) | Fiziksel yol (IIS) |
|-------------------|----------------------------|---------------------|
| **baskentenerji.com** | `httpdocs` | `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` |
| **api.baskentenerji.com** | `api.baskentenerji.com` | `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com` |
| **moneytransferturkey.com** | `httpdocs` veya `httpdocs\public` | `C:\Inetpub\vhosts\moneytransferturkey.com\httpdocs` |
| **tg.moneytransferturkey.com** | `tg.moneytransferturkey.com` | `C:\Inetpub\vhosts\moneytransferturkey.com\tg.moneytransferturkey.com` |

**Kural:** Her domain/subdomain için Plesk’te **tek bir belge kökü**; yol adı mümkünse domain/subdomain adı ile tutarlı (kurumsal yapı).

---

## 2. Plesk’te yapılacaklar (kontrol listesi)

### 2.1 Genel

- [ ] **Web Sitesi ve Alan Adları** altında tüm domain’ler listelenmiş.
- [ ] Her domain için **Hosting ve DNS** → **Belge kökü** yukarıdaki tabloya uygun.
- [ ] **SSL/TLS:** Her domain için sertifika atanmış (Let’s Encrypt veya kurumsal sertifika).
- [ ] **Güvenlik duvarı (Plesk):** 80, 443, 8443 (panel) açık; gereksiz portlar kapalı.

### 2.2 baskentenerji.com

- [ ] Belge kökü: **httpdocs** (public değil).
- [ ] Varsayılan belge: `index.html`.
- [ ] HTTPS zorunlu / yönlendirme isteğe bağlı.

### 2.3 api.baskentenerji.com

- [ ] Belge kökü: **api.baskentenerji.com** klasörü.
- [ ] Uygulama havuzu (IIS): **Yönetilen Kod Yok** (ASP.NET Core).
- [ ] Deploy: `deploy-api-to-canli.ps1` ile publish çıktısı bu klasöre kopyalanır; **appsettings.json** ve **web.config** canlıda korunur.

### 2.4 tg.moneytransferturkey.com (subdomain)

- [ ] Belge kökü: **tg.moneytransferturkey.com**.
- [ ] **web.config** ile reverse proxy (127.0.0.1:5000); ARR Proxy IIS’te etkin.

### 2.5 Plesk panel erişimi

- [ ] **https://sunucu-ip:8443** veya **https://localhost:8443** erişilebilir.
- [ ] SMB Web View: **https://localhost:8443/smb/web/view** (gerekirse sertifika uyarısında devam edilir).

---

## 3. IIS ile uyum (Plesk Windows)

| Site | App Pool | .NET CLR |
|------|----------|----------|
| baskentenerji.com | Plesk atadığı havuz | 4.0 veya Yönetilen Kod Yok |
| api.baskentenerji.com | api.baskentenerji.com(domain)(pool) | **Yönetilen Kod Yok** |

**URL Rewrite** ve (tg için) **Application Request Routing (ARR)** modülleri yüklü ve proxy etkin olmalı.

---

## 4. Kurumsal yapı özeti

- **Tek kaynak:** Belgeler `docs/` altında; Plesk tarafında yapılan değişiklikler bu belge ve **PLESK_TUM_YAPILANDIRMA_TAMAMLAMA.md** ile uyumlu tutulur.
- **Güvenlik:** Hassas dosyalar (appsettings, web.config içindeki gizliler) repo’da yok; canlıda deploy sırasında korunur.
- **Erişim:** Panel https://localhost:8443; siteler https ile çalışır.

Bu yapı ile Plesk panel kurumsal standarda getirilir.
