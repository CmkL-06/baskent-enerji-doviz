# Başkent Enerji Döviz Yönetim Sistemi

Döviz alım-satım, kasa yönetimi, ofislerarası para transferi ve Telegram bot entegrasyonlarını kapsayan kurumsal finans yönetim platformu.

**Branch:** `BASKENT-DOVIZ` | **Sunucu:** 159.195.55.1 (Windows Server 2022 / IIS 10)

---

## Canlı Ortam

| Servis | URL |
|---|---|
| Yönetim Paneli | https://baskentenerji.com/ihtiyar/ |
| REST API | https://api.baskentenerji.com/api/v1 |
| API Health (public) | https://api.baskentenerji.com/api/v1/diagnostics/ping |

---

## Mimari

```
baskentenerji.com/ihtiyar/   → Vue 3 + Vite (IIS sanal dizin)
api.baskentenerji.com        → .NET 8 Web API (IIS + ARR reverse proxy)
.\SQLEXPRESS                 → SQL Server Express (mtturkey_exchange)
Telegram Bots                → Python (main_bot, operator_bot, ruble_bot)
```

---

## Proje Yapısı

```
├── BaskentEnerji.API/        .NET 8 Web API
│   ├── Controllers/
│   │   ├── ExchangeOffice/   Exchange, Vault, AutoRate, OfficeTransfer
│   │   ├── Site/             Sayfa, Menü, Tema, Medya
│   │   ├── Blog/
│   │   ├── Coin/             Kripto takip
│   │   ├── DiagnosticsController.cs
│   │   └── UserController.cs  JWT auth
│   └── Program.cs
├── BaskentEnerji.Data/       EF Core DbContext + Migrations
├── BaskentEnerji.Business/   İş mantığı servisleri
├── BaskentEnerji.Entity/     Entity / DTO modelleri
├── frontend/                 Vue 3 + Vite SPA
│   ├── src/views/            Sayfa bileşenleri
│   ├── src/services/apiservice.ts   API istemcisi
│   └── src/stores/           Pinia state yönetimi
├── telegram-bot/             Python Telegram botları
│   ├── main_bot.py
│   ├── operator_bot.py
│   └── ruble_bot.py
└── scripts/                  Yardımcı PowerShell/bash scriptleri
```

---

## Kurulum (Yerel Geliştirme)

### Gereksinimler

- .NET 8 SDK
- Node.js 18+
- SQL Server Express
- Python 3.10+

### 1. Repository

```bash
git clone https://github.com/CmkL-06/baskent-enerji-doviz.git
cd baskent-enerji-doviz
git checkout BASKENT-DOVIZ
```

### 2. API Konfigürasyonu

`BaskentEnerji.API/appsettings.json` oluşturun (git'e eklenmez):

```json
{
  "ConnectionStrings": {
    "SQL": "Server=.\\SQLEXPRESS;Database=mtturkey_exchange;User Id=...;Password=...;TrustServerCertificate=True"
  },
  "JwtSecretKey": "...",
  "JwtIssuer": "baskentenerji",
  "JwtAudience": "baskentenerji",
  "JwtExpiryHours": 8
}
```

### 3. Veritabanı

```bash
cd BaskentEnerji.Data
dotnet ef database update --startup-project ../BaskentEnerji.API
```

### 4. API Başlatma

```bash
cd BaskentEnerji.API
dotnet run
# http://localhost:5093/swagger
```

### 5. Frontend

```bash
cd frontend
npm install
# Opsiyonel: .env.local dosyası oluştur
echo "VITE_API_BASE_URL=http://localhost:5093/api/v1" > .env.local
npm run dev
# http://localhost:3000
```

### 6. Telegram Botları

```bash
cd telegram-bot
pip install python-telegram-bot requests
# .env dosyasını oluştur (bkz. telegram-bot/.env.example)
python run_all.py
```

---

## Üretim Deployment (Windows Server / IIS)

### API

```powershell
cd BaskentEnerji.API
dotnet publish -c Release -r win-x64 --self-contained false -o ../deploy/publish-api-windows
# IIS uygulama havuzunu recycle edin
```

### Frontend

```bash
cd frontend
npm run build
# dist/ klasörünü IIS sanal dizinine kopyalayın
# Örn: C:\inetpub\wwwroot\ihtiyar\
```

> **Not:** `appsettings.json` ve `web.config` üretimde ayrı yönetilir, deploy sırasında üzerine yazılmaz.

---

## API Endpoint Özeti

| Endpoint | Auth | Açıklama |
|---|---|---|
| `POST /api/v1/user/login` | Hayır | JWT token al |
| `GET /api/v1/diagnostics/ping` | Hayır | Sağlık kontrolü |
| `GET /api/v1/exchange/dashboard` | Evet | Gösterge paneli |
| `GET /api/v1/exchange/rates` | Evet | Döviz kurları |
| `GET /api/v1/exchange/vaults` | Evet | Kasa listesi |
| `GET /api/v1/exchange/offices/summary` | Evet | Ofis özetleri |
| `GET /api/v1/exchange/transactions` | Evet | İşlem geçmişi |
| `POST /api/v1/exchange/exchange` | Evet | Yeni işlem |
| `GET /api/v1/exchange/office-transfer/pending` | Evet | Bekleyen transferler |

---

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Backend | .NET 8 / ASP.NET Core |
| ORM | Entity Framework Core 8 |
| Auth | JWT Bearer |
| Veritabanı | SQL Server Express |
| Frontend | Vue 3 + Vite + Pinia |
| Botlar | Python 3 + python-telegram-bot |
| Hosting | Windows Server 2022 / IIS 10 |

---

## Güvenlik

- `/api/v1/diagnostics/ping` herkese açıktır; diğer tüm endpoint'ler `[Authorize]` gerektirir
- Rol tabanlı yetkilendirme: `Owner`, `Admin`, `Personel`
- Kullanıcılar yalnızca atandıkları şubeyi (`User_Offices`) görebilir
- `appsettings.json` ve `.env` dosyaları `.gitignore`'dadır — production'a manuel kopyalanmalıdır
