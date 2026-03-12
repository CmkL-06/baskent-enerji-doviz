# Başkent Enerji – Yerel PC’de Ulaşılan Veri Listesi

Bu belge, yerel bilgisayarda (workspace) **baskentenerji** / **Başkent Enerji** ile ilgili tespit edilen dosya, klasör ve referansların özet listesidir. Hassas değerler (şifre, token, connection string) **gösterilmedi** veya maskelendi.

---

## 1. Yerel tarama kapsamı

Bu listedeki kayıtlar, yerel makinede Başkent Enerji döviz sistemiyle doğrudan ilişkili teknik dosyaları ve operasyon referanslarını kapsar.

---

## 2. Masaüstü – API Test Script

| Özellik | Değer |
|--------|--------|
| **Dosya** | `C:\Users\Administrator\Desktop\Baskentenerji_API_Test.ps1` |
| **Dil** | PowerShell |
| **Amaç** | API ve web uç noktalarının erişilebilirliğini test etmek |

**Test edilen adresler:**

- **API (api.baskentenerji.com):**
  - `/`
  - `/api/v1/Exchange`
  - `/api/v1/Exchange/currency`
  - `/api/v1/Exchange/dashboard`
  - `/api/v1/Exchange/vaults`
- **Web:** `https://baskentenerji.com`

---

## 3. Masaüstü – yeni_bot (Telegram + Başkent API)

**Klasör:** `C:\Users\Administrator\Desktop\yeni_bot\`

### 3.1 Başkent API istemcisi

| Dosya | Açıklama |
|-------|----------|
| `baskent_api.py` | BaşkentEnerji API client: login, döviz alış/satış gönderimi, işlem kaydından API’ye aktarım |

**Önemli fonksiyonlar:**

- `login()` – API’ye giriş, token alma (`/User/login`, mail + password)
- `send_exchange(...)` – Döviz işlemi gönderimi (vaultId, currency, amount, is_buy, rate, notes)
- `send_exchange_for_transaction(transaction_id)` – DB’den işlem okuyup API’ye gönderme

**Desteklenen para birimleri (config’den):** USDT, RUBLE → TRY; `BASKENT_TRY_CURRENCY_ID` sabit (cd817762-...).

**Kullanım yerleri:** `main_bot.py`, `ruble_bot.py` → `send_exchange_for_transaction` import edilmiş.

### 3.2 Konfigürasyon

| Dosya | Açıklama |
|-------|----------|
| `config.py` | Merkezi config; Başkent API ayarları `.env` üzerinden |

**Başkent ile ilgili env değişkenleri:**

| Değişken | Varsayılan / Açıklama |
|----------|------------------------|
| `BASKENT_API_URL` | `https://api.baskentenerji.com/api/v1` |
| `BASKENT_USERNAME` | (boş / .env) |
| `BASKENT_PASSWORD` | (boş / .env) |
| `BASKENT_API_TOKEN` | (boş / .env) |
| `BASKENT_DEFAULT_VAULT_ID` | (boş / .env) |
| `BASKENT_USDT_CURRENCY_ID` | (boş / .env) |
| `BASKENT_RUBLE_CURRENCY_ID` | (boş / .env) |
| `BASKENT_TRY_CURRENCY_ID` | Sabit: `cd817762-d3f6-4df2-83bc-8a8f9cb476a2` |

---

## 4. Documents – Scriptler

| Dosya | Açıklama |
|-------|----------|
| `C:\Users\Administrator\Documents\Scriptler\baskent_test.py` | Basit login testi: `BASKENT_API_URL` + `/auth/login`, `BASKENT_USERNAME` / `BASKENT_PASSWORD` ile POST (`.env` kullanıyor) |

**Not:** Bu script’te endpoint `/auth/login`; `baskent_api.py` ise `/User/login` kullanıyor. Farklı API sürümleri veya farklı auth yolları olabilir.

---

## 5. publish_api_baskent (Yayınlanmış API / AnasıTAS_Deniz)

**Klasör:** `C:\Users\Administrator\publish_api_baskent\`

Bu klasör, **AnasıTAS_Deniz.API** projesinin publish çıktısı gibi görünüyor; Başkent Enerji ile ilgili yapılandırma içeriyor.

### 5.1 appsettings.json (Başkent ile ilgili alanlar)

| Anahtar | Değer (hassas olanlar maskeli) |
|--------|---------------------------------|
| `JwtIssuer` | `baskentenerji` |
| `JwtAudience` | `baskentenerji` |
| `EmailSettings.Username` | `info@baskentenerji.com` |
| `Site.Domain` | `baskentenerji.com` |
| `ConnectionStrings.SQL` | *(var; bu listede gösterilmedi)* |
| `JwtSecretKey` | *(var; bu listede gösterilmedi)* |
| `EmailSettings.Password` | *(var; bu listede gösterilmedi)* |
| `Firebase` | *(private_key vb. alanlar var; bu listede gösterilmedi)* |

### 5.2 Klasör yapısı (özet)

- `AnasıTAS_Deniz.API.exe` / `.dll` – Ana uygulama
- `appsettings.json` – Yukarıdaki ayarlar
- `web.config` – IIS/hosting
- `wwwroot\wwwroot\uploads\` – Yüklenen dosyalar (ör. PNG’ler)
- `firebaseConfig.json` – Firebase yapılandırması
- `runtimes\` – Platforma özel kütüphaneler

---

## 6. QuantaQuokka_ (Kaynak proje)

**Klasör:** `C:\Users\Administrator\QuantaQuokka_\`

Bu workspace’te **Exchange Office / döviz** ile ilgili kod bu çözümde de geçiyor; doğrudan “baskentenerji” metni sınırlı. İlgili dosyalar (referans amaçlı):

- `AnasıTAS_Deniz.API\Controllers\ExchangeOffice\ExchangeController.cs`
- `AnasıTAS_Deniz.Business\Services\ExchangeOffice\` (ör. DovizComProvider.cs)
- `AnasıTAS_Deniz.API\appsettings.json`, `Program.cs`
- `AnasıTAS_Deniz.Business\Services\Email\EmailSender.cs`, `Coin\CoinPriceService.cs`

*(Bu proje, publish_api_baskent’in kaynak projesi olabilir; Başkent Enerji domain’i appsettings’te baskentenerji.com / JWT issuer olarak kullanılıyor.)*

---

## 7. Özet tablo – Başlıca teknik dosyalar

| # | Tam yol | Tür |
|---|--------|-----|
| 1 | `C:\Users\Administrator\Desktop\Baskentenerji_API_Test.ps1` | PowerShell test |
| 2 | `C:\Users\Administrator\Desktop\yeni_bot\baskent_api.py` | Python API client |
| 3 | `C:\Users\Administrator\Desktop\yeni_bot\config.py` | Config (BASKENT_* env) |
| 4 | `C:\Users\Administrator\Documents\Scriptler\baskent_test.py` | Python login test |
| 5 | `C:\Users\Administrator\publish_api_baskent\appsettings.json` | API/JWT/Email/Domain ayarları |

---

## 8. URL’ler (özet)

| Amaç | URL |
|------|-----|
| Web uygulama | https://baskentenerji.com |
| Giriş sayfası | https://baskentenerji.com/login |
| İhtiyar dashboard | https://baskentenerji.com/ihtiyar/dashboard |
| API base (script’lerde) | https://api.baskentenerji.com |
| API v1 (yeni_bot) | https://api.baskentenerji.com/api/v1 |

---

## 9. Güvenlik notu

- Şifre, token, connection string, JWT secret, Firebase private key gibi alanlar bu MD’de **yazılmadı** veya maskelendi.
- Gerçek değerler: `appsettings.json`, `.env` ve ortam değişkenlerinde duruyor; bu dosyaları paylaşırken veya sürüm kontrolüne koyarken hassas alanları kaldırın veya gizleyin.

---

*Belge, yerel workspace taramasıyla oluşturuldu. Tarih: 2025.*
