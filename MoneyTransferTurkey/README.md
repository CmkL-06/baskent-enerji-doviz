# SmileMedical

Döviz bürosu, site yönetimi, blog ve coin modüllerini kapsayan .NET 8 API + Vue 3 frontend projesi.

## Proje yapısı

```
SmileMedical/
├── SmileMedical.API/          # Web API (Controllers, Swagger, JWT, SignalR)
├── SmileMedical.Business/     # İş mantığı (Services)
├── SmileMedical.Data/         # EF Core, DbContext, Migrations
├── SmileMedical.Entity/       # Entity ve DTO modelleri
├── frontend/                  # Vue 3 + Vite admin arayüzü
├── docs/                      # Dokümantasyon ve görselleştirmeler
└── README.md                  # Bu dosya
```

## Gereksinimler

- **Backend:** .NET 8 SDK, SQL Server (veya yapılandırmaya göre SQLite)
- **Frontend:** Node.js 18+

## Backend (API) çalıştırma

1. `appsettings.json` içinde `ConnectionStrings:SQL` değerini kendi veritabanı bağlantınıza göre ayarlayın.
2. Çözümü Visual Studio ile açıp SmileMedical.API’yi başlatın veya:

   ```bash
   cd SmileMedical.API
   dotnet run
   ```

3. Geliştirme ortamında Swagger: `https://localhost:7xxx/swagger` (port launchSettings’e göre değişir).
4. API varsayılan olarak CORS ile `http://localhost:5173` (Vite) ve diğer yapılandırılmış origin’lere izin verir.

## Frontend çalıştırma

```bash
cd frontend
npm install
npm run dev
```

- Uygulama: **http://localhost:5173**
- Giriş: API’de kayıtlı kullanıcı ile **Giriş** veya API kapalıyken **Demo giriş (API yok)**.

Backend farklı portta ise `frontend/.env` oluşturup örneğin:

```env
VITE_API_BASE_URL=http://localhost:5000
```

## Dokümantasyon

| Dosya | Açıklama |
|-------|----------|
| [docs/README.md](docs/README.md) | Dokümantasyon indeksi |
| [docs/system-overview.html](docs/system-overview.html) | Katman ve bileşen görselleştirmesi (tarayıcıda açın) |
| [docs/SYSTEM-ARCHITECTURE.md](docs/SYSTEM-ARCHITECTURE.md) | Sistem mimarisi (Mermaid diyagramları) |
| [docs/frontend-preview.html](docs/frontend-preview.html) | Frontend ekran önizlemesi (statik) |
| [frontend/README.md](frontend/README.md) | Frontend kurulum ve kullanım |

## Ana modüller

- **Kullanıcı:** Giriş, kayıt, şifre sıfırlama (JWT).
- **Döviz bürosu:** Ofis, kasa (vault), işlem, kur, otomatik kur (TCMB, Doviz.com, Binance), taraflar (party), hesap, gider.
- **Site:** Sayfa, menü, slider, tema, form, SEO, analytics, medya.
- **Blog:** Kategori, makale, tag.
- **Coin:** Coin CRUD, canlı fiyat (SignalR hub: `/coinPriceHub`).

## Testler

- **API (entegrasyon):** `SmileMedical.API.Tests` — xUnit, WebApplicationFactory, in-memory DB.
  - Login (geçerli/geçersiz kimlik bilgileri), GET Theme (AllowAnonymous).
  - Çalıştırma: `dotnet test SmileMedical.API.Tests/SmileMedical.API.Tests.csproj`
- **Frontend:** Vitest (birim), Playwright (E2E). Bkz. [frontend/README.md](frontend/README.md#testler).

## Teknoloji özeti

- **API:** ASP.NET Core 8, EF Core, JWT, SignalR, Swagger.
- **Veritabanı:** SQL Server (production); yapılandırmaya göre SQLite kullanımı mümkün).
- **Frontend:** Vue 3, Vue Router, Vite, Axios.
