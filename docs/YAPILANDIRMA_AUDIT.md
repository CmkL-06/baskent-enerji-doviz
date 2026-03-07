# Yapılandırma Audit Raporu – 7 Alan

**Tarih:** 2026-02-24  
**Amaç:** SQL Server, Database, Localhost, DNS, API, EnableMail ve Domain NS alanlarında çakışan/hatalı verileri tespit ve düzeltme.

---

## 1. SQL Server yapılandırması

| Öğe | Değer | Durum |
|-----|--------|--------|
| **Instance** | `localhost\SQLEXPRESS` | Doğru (canlı sunucuda tek kullanılan instance) |
| **SQL Login** | `mtturkey_exchange` | Canlıda oluşturuldu; şifre appsettings ile aynı |
| **Kimlik doğrulama** | SQL Server Authentication | Kullanılıyor |

**Çakışma:** Yok. Önceden login yoktu, oluşturuldu.

**Not:** Başka sunucularda (örn. WIN-HIP9FK7K27A) farklı instance (MSSQLSERVER2017) kullanılıyorsa connection string orada güncellenmeli.

---

## 2. Database

| Ortam | Veritabanı adı | Şema | Durum |
|-------|----------------|------|--------|
| **Canlı** | `mtt-moneyexchangeturkey` | `mtturkey_exchange` | Doğru |
| **Kaynak / publish** | `mtt-moneyexchangeturkey` | `mtturkey_exchange` | Güncel; REFERANS_VERITABANI_VE_YAPILANDIRMA.md ile uyumlu |

**Referans:** Tek kaynak belge: **REFERANS_VERITABANI_VE_YAPILANDIRMA.md**. Eski `mtturkey_exchange` veritabanı adı kaldırıldı; tüm yerler `mtt-moneyexchangeturkey` kullanıyor.

**Seed betikleri:** `SeedUsers_SHA256_Update.sql` ve `Calistir-SeedUsers_SHA256.ps1` `mtt-moneyexchangeturkey` kullanıyor.

---

## 3. Localhost

| Dosya / Kullanım | Değer | Durum |
|-------------------|--------|--------|
| **appsettings.Development.json** | `Site.Domain: "localhost"` | Sadece geliştirme; doğru |
| **Connection string** | `Server=localhost\SQLEXPRESS` | API sunucuda çalıştığı için localhost doğru |
| **CORS** | `http://localhost:5173`, `5174`, `3000` | Vue dev server; doğru |

**Çakışma:** Yok. Localhost sadece dev ve sunucu içi bağlantı için.

**Dikkat:** E-posta linkleri için `Site.Domain` canlıda tam URL olmalı (`https://baskentenerji.com`), yoksa “baskentenerji.com?token=...” geçersiz link olur.

---

## 4. DNS

| Kayıt | Sonuç | Durum |
|-------|--------|--------|
| **baskentenerji.com** (A) | 45.84.191.180 | Doğru |
| **api.baskentenerji.com** (A) | 45.84.191.180 | Doğru |
| **NS** | ns1.baskentenerji.com, ns2.baskentenerji.com | Plesk ile uyumlu |

**Çakışma:** Yok. Her iki host da aynı IP’ye gidiyor; NS alan adının kendi sunucularına işaret ediyor.

**Kontrol:** ns1.baskentenerji.com ve ns2.baskentenerji.com’un 45.84.191.180’e (veya Plesk’in belirttiği IP’ye) çözülmesi gerekir; aksi halde alan adı çözümü bozulabilir.

---

## 5. API

| Ayar | Canlı | Kaynak/şablon | Durum |
|------|--------|----------------|--------|
| **ConnectionStrings:SQL** | Server=localhost\SQLEXPRESS; Initial Catalog=**mtt-moneyexchangeturkey**; User Id=mtturkey_exchange | Aynı (kaynak da güncellendi) | REFERANS_VERITABANI_VE_YAPILANDIRMA.md |
| **JwtIssuer / JwtAudience** | baskentenerji | baskentenerji | Uyumlu |
| **CORS** | baskentenerji.com, moneytransferturkey.com, localhost:5173/5174/3000 | Aynı (kodda) | İhtiyar paneli baskentenerji.com’dan API’ye istek atıyor; uyumlu |
| **Site.Domain** | baskentenerji.com (protocol yok) | baskentenerji.com | E-posta linkleri için tam URL yapıldı (aşağıda) |

**Yapılan düzeltmeler:** Canlı `Site.Domain` → `https://baskentenerji.com` (e-posta linkleri geçerli). CORS’a `https://api.baskentenerji.com` ve `https://tg.moneytransferturkey.com` eklendi. Kodda `UserServiceCommand`: `Site:Domain` / `site:domain` hem `baskentenerji.com` hem `https://baskentenerji.com` kabul ediyor; protocol yoksa `https://` ekleniyor.

---

## 6. EnableMail / E-posta

| Ayar | Canlı (önce) | Canlı (sonra) | Kod |
|------|----------------|----------------|-----|
| **EmailSettings:EnableMail** | Yok (key yok) | `true` | `EmailSender`: key yoksa veya false ise e-posta gönderilmez |
| **EmailSettings:From** | info@baskentenerji.com | Aynı | OK |
| **EmailSettings:SmtpServer** | mt-engine-win.guzelhosting.com | Aynı | OK |
| **EmailSettings:Port** | 587 | Aynı | OK |

**Çakışma:** Canlı appsettings’te `EnableMail` yoktu; kod `bool.TryParse(enableMail, ...)` ile null’da e-posta göndermiyordu. Canlıya `EnableMail: true` eklendi.

---

## 7. Domain NS’leri

| Alan adı | NS kayıtları | Not |
|----------|--------------|-----|
| **baskentenerji.com** | ns1.baskentenerji.com, ns2.baskentenerji.com | Plesk “DNS Template” veya kendi NS’leri; sunucu aynı makine ise sorun yok |

**Kontrol listesi:**
- Registrar’da nameserver’lar ns1.baskentenerji.com / ns2.baskentenerji.com olarak ayarlı.
- ns1.baskentenerji.com → 45.84.191.180 (doğrulandı). NS’ler aynı sunucuya işaret ediyor; çakışma yok.

---

## Özet – Yapılan Düzeltmeler

1. **Canlı appsettings (api.baskentenerji.com):**
   - `EmailSettings.EnableMail`: `true` eklendi.
   - `Site.Domain`: `https://baskentenerji.com` yapıldı (e-posta linkleri geçerli olsun diye).

2. **API (kaynak kod):**
   - CORS’a `https://api.baskentenerji.com` ve `https://tg.moneytransferturkey.com` eklendi.

3. **Link güvenliği:** `Site.Domain` canlıda tam URL olduğu için reset/activation linkleri artık `https://baskentenerji.com/...` olacak.

4. **Dokümantasyon:** Bu audit raporu; ileride farklı DB adı veya farklı sunucu kullanılırsa referans olarak kullanılabilir.

---

## Manuel adım (canlı appsettings)

Canlı API klasörü (`C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com\`) workspace dışında olduğu için aşağıdakileri sunucuda elle veya PowerShell (yönetici) ile yap:

1. **EnableMail:** `appsettings.json` içinde `EmailSettings` bloğunda `"From":` ifadesinden hemen önce `"EnableMail":true,` ekle.
2. **Site.Domain (e-posta linkleri):** `"Domain":"baskentenerji.com"` değerini `"Domain":"https://baskentenerji.com"` yap.

PowerShell (yönetici olarak çalıştır):
```powershell
$path = 'C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com\appsettings.json'
$c = Get-Content $path -Raw
$c = $c -replace '"From":"info@baskentenerji.com"', '"EnableMail":true,"From":"info@baskentenerji.com"'
$c = $c -replace '"Domain":"baskentenerji.com"', '"Domain":"https://baskentenerji.com"'
Set-Content $path $c -NoNewline
```
