# Referans: Veritabanı ve Yapılandırma (Güncel)

**Son güncelleme:** 2026-02-24  
**Amaç:** Tek kaynak – veritabanı adı, bağlantı ve kullanıcı bilgileri. Eski/boş isimler kaldırıldı; tüm proje bu referansa göre güncellendi.

---

## Onaylanan doğru yapı

| Öğe | Onaylı değer |
|-----|----------------|
| Veritabanı adı | **mtt-moneyexchangeturkey** |
| SQL Login | mtturkey_exchange |
| Şifre (SQL) | *cc_pPMmHu79ka7q |
| Instance | localhost\SQLEXPRESS |
| Şema | mtturkey_exchange |

**Tek geçerli veri kaynağı:** Aşağıdaki §1–2 ve canlı `api.baskentenerji.com\appsettings.json`. Başka dosyalarda farklı DB adı veya `Database=mtturkey_exchange` görürseniz hatalıdır; **Initial Catalog=mtt-moneyexchangeturkey** ile güncelleyin.

**Güncellenen hatalı kaynak:** `Yedekler\eskiveri\api-publish\appsettings.json` — Database=mtturkey_exchange kaldırıldı, Initial Catalog=mtt-moneyexchangeturkey yapıldı.

---

## 1. Veritabanı (tek geçerli ad)

| Öğe | Değer |
|-----|--------|
| **Veritabanı adı** | **mtt-moneyexchangeturkey** |
| **Şema** | mtturkey_exchange |
| **SQL Server instance** | localhost\SQLEXPRESS |
| **SQL Login** | mtturkey_exchange |

**Not:** Veritabanı adı olarak artık sadece **mtt-moneyexchangeturkey** kullanılır. Eski ad **mtturkey_exchange** veritabanı adı olarak kullanılmaz (şema ve login adı olarak kalır).

---

## 2. Connection string (standart)

```
Server=localhost\SQLEXPRESS;Initial Catalog=mtt-moneyexchangeturkey;TrustServerCertificate=True;User Id=mtturkey_exchange;Password=*cc_pPMmHu79ka7q;Connection Timeout=60;Max Pool Size=200;Min Pool Size=10;Connection Lifetime=300;MultipleActiveResultSets=true
```

**Kullanıldığı yerler:** Canlı API (Inetpub), kaynak-kod (AnasıTAS_Deniz.API, AnasıTAS_Deniz.Data), Seed betikleri, publish-api.

---

## 3. Seed ve kullanıcılar

| Betik / Dosya | Veritabanı |
|---------------|------------|
| **SeedUsers_SHA256_Update.sql** | USE [mtt-moneyexchangeturkey] |
| **Calistir-SeedUsers_SHA256.ps1** | $database = "mtt-moneyexchangeturkey" |

**Giriş bilgileri (Mail veya Username + Şifre):**

| Rol | Username | Şifre |
|-----|----------|--------|
| Owner | ihtiyar | owner1 |
| Admin | Damat | admin1 |
| Personel | PERSONEL veya Perso | personel1 |

---

## 4. Canlı ve yerel yollar

| Bileşen | Yol / URL |
|---------|-----------|
| **Canlı API appsettings** | C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com\appsettings.json |
| **API login** | POST https://api.baskentenerji.com/api/v1/User/login |
| **Kaynak (API)** | BASKENT_PROJE\kaynak-kod\AnasıTAS_Deniz.API\appsettings.json |
| **Kaynak (Data)** | BASKENT_PROJE\kaynak-kod\AnasıTAS_Deniz.Data\appsettings.json |
| **Seed SQL** | BASKENT_PROJE\scripts\SeedUsers_SHA256_Update.sql |
| **Seed PowerShell** | BASKENT_PROJE\scripts\Calistir-SeedUsers_SHA256.ps1 |

---

## 5. Tablo (şema + tablo)

- **Tam nitelikli tablo:** `mtturkey_exchange.Users`
- **EF Core / DbContext:** Varsayılan şema `mtturkey_exchange` (HasDefaultSchema).

---

## 6. Bot ve Telegram veritabanı isimleri

| Kapsam | İsim | Açıklama |
|--------|------|----------|
| **Ruble** (botlar + veritabanı) | **mtr-moneytransferruble** | Ruble ile alakalı botların ve veritabanının resmî ismi |
| **Crypto** (bot) | **mtc-moneytransfercrypto** | Kripto ile alakalı botun resmî ismi |
| **Ana döviz API** | mtt-moneyexchangeturkey | Yukarıdaki §1–2 |

**Not:** Telegram botları ortak DB kullanıyorsa (örn. `DB_NAME`), Ruble için ayrı DB açıldığında bağlantıda **mtr-moneytransferruble** kullanılır. Bot isimlendirmesi (doküman ve .env) bu tabloya göre yapılır.

---

Bu dosya, veritabanı adı ve bağlantı için tek referanstır. Yeni eklemeler veya değişiklikler buraya işlenmeli; eski dağınık referanslar bu belgeyle birleştirilmiştir.
