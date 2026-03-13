# Localhost Geliştirme Kılavuzu

**Tarih:** 25.02.2026

---

## 1. API’yi localhost’ta çalıştırma

```powershell
cd C:\Users\Administrator\Desktop\BASKENT_PROJE\kaynak-kod\BaskentEnerji.API
dotnet run
```

- **HTTP:** http://localhost:5093  
- **Swagger:** http://localhost:5093/swagger  
- **HTTPS** (varsayılan profil): https://localhost:7197  

Development ortamında HTTPS yönlendirmesi kapalı; http://localhost:5093 doğrudan kullanılır.

---

## 2. Veritabanı (localhost)

- **Connection string** `localhost\SQLEXPRESS` ve **mtt-moneyexchangeturkey** kullanıyor (`appsettings.json`). Referans: REFERANS_VERITABANI_VE_YAPILANDIRMA.md.
- Girişin çalışması için kullanıcı şifreleri **SHA256** hash olmalı.  
  **Betik:** `scripts\SeedUsers_SHA256_Update.sql`  
  Bunu bu makinedeki SQL Server’da (veya API’nin bağlandığı sunucuda) çalıştırın.

---

## 3. Test scriptleri (localhost)

**PowerShell – API endpoint testi (localhost):**
```powershell
cd C:\Users\Administrator\Desktop\BASKENT_PROJE\scripts
.\Baskentenerji_API_Test.ps1 -LocalHost
```

**PowerShell – Canlı API (varsayılan):**
```powershell
.\Baskentenerji_API_Test.ps1
```

**Python – Giriş testi (localhost):**  
Ortam değişkeni ile:
```powershell
$env:BASKENT_API_URL="http://localhost:5093/api/v1"
$env:BASKENT_USERNAME="ihtiyar"
$env:BASKENT_PASSWORD="owner1"
python baskent_test.py
```

Veya `scripts` klasöründe `.env` oluşturup:
```env
BASKENT_API_URL=http://localhost:5093/api/v1
BASKENT_USERNAME=ihtiyar
BASKENT_PASSWORD=owner1
```
Sonra: `python baskent_test.py`

---

## 4. Panel (Vue / ihtiyar) localhost’ta

- **dealer-panel-enhance.js** artık sayfa `localhost` veya `127.0.0.1` üzerinde açıksa API adresi olarak **http://localhost:5093/api/v1** kullanır.
- Panel’i (ihtiyar) localhost’ta çalıştırıyorsanız (örn. `npm run dev` → http://localhost:5173), CORS’ta `http://localhost:5173`, `http://localhost:5093`, `https://localhost:7197` zaten tanımlı.

---

## 5. Yapılan localhost iyileştirmeleri

| Öğe | Değişiklik |
|-----|------------|
| **appsettings.Development.json** | Eklendi; `Site.Domain`: localhost, Development log seviyeleri. |
| **Program.cs CORS** | localhost:5093, localhost:7197, 127.0.0.1:5093, 127.0.0.1:5173 eklendi. |
| **Program.cs HTTPS redirect** | Development’ta kapatıldı; http://localhost:5093 sorunsuz çalışır. |
| **Baskentenerji_API_Test.ps1** | `-LocalHost` parametresi eklendi; localhost:5093’e istek atar. |
| **baskent_test.py** | `BASKENT_API_URL` ile localhost kullanımı; URL `/User/login` olacak şekilde düzeltildi. |
| **dealer-panel-enhance.js** | localhost/127.0.0.1’de API = http://localhost:5093/api/v1. |

---

## 6. Hızlı kontrol listesi

1. SQL Server’da **mtt-moneyexchangeturkey** veritabanı ve gerekirse `SeedUsers_SHA256_Update.sql` çalıştırıldı mı?  
2. `dotnet run` ile API http://localhost:5093’te açılıyor mu?  
3. Giriş: `POST http://localhost:5093/api/v1/User/login` → Body: `{"Mail":"ihtiyar","Password":"owner1"}` → 200 + token.  
4. Panel localhost’ta ise tarayıcıda sayfa adresi localhost/127.0.0.1 olmalı ki dealer-panel local API’yi kullansın.

---

## 7. Gelişmiş araç desteği (tooling)

API üzerinde operasyonel izleme için aşağıdaki endpoint’ler kullanılabilir:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /api/v1/Diagnostics/ping`
- `GET /api/v1/Diagnostics/version`
- `GET /api/v1/Diagnostics/system`
- `GET /api/v1/Diagnostics/db`

Tek komut smoke testi:

```bash
bash scripts/api_tooling_smoke.sh
```
