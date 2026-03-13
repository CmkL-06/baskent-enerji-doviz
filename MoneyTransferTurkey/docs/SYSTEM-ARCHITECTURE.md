# MoneyTransferTurkey (Legacy) — Sistem Mimarisi

## Katmanlı yapı

```mermaid
flowchart TB
  subgraph API["MoneyTransferTurkey.API"]
    C[Controllers]
    H[Hosted Services]
  end

  subgraph Business["MoneyTransferTurkey.Business"]
    U[User Services]
    E[ExchangeOffice Services]
    S[Site Services]
    B[Blog / Coin / Cache]
  end

  subgraph Data["MoneyTransferTurkey.Data"]
    DBctx[MoneyTransferTurkeyDbContext]
  end

  subgraph Entity["MoneyTransferTurkey.Entity"]
    Ent[Entities]
  end

  subgraph DB[(SQL Server)]
  end

  API --> Business
  Business --> Data
  Data --> Entity
  Data --> DB
```

## Ana modüller

```mermaid
flowchart LR
  subgraph ExchangeOffice["Döviz Bürosu"]
    Office[Office / Vault / Transaction]
    Party[Party / Hesap / Kredi]
    AutoRate[Otomatik Kur]
    Expense[Gider]
    Blockchain[Binance / TRC20]
  end

  subgraph Site["Site Yönetimi"]
    Page[Sayfa / Menü / Slider]
    Theme[Tema / Ayarlar]
    SEO[SEO / Analytics / Media]
    Form[Formlar]
  end

  subgraph Content["İçerik"]
    Blog[Blog]
    Coin[Coin]
  end

  User[Kullanıcı] --> ExchangeOffice
  User --> Site
  User --> Content
```

## Veri akışı

```mermaid
sequenceDiagram
  participant Client
  participant API
  participant Business
  participant Data
  participant DB

  Client->>API: HTTP Request
  API->>Business: Service çağrısı
  Business->>Data: DbContext
  Data->>DB: SQL
  DB-->>Data: Sonuç
  Data-->>Business: Entity
  Business-->>API: DTO / Sonuç
  API-->>Client: JSON Response
```

## Proje referansları

| Proje | Bağımlılıklar |
|-------|----------------|
| MoneyTransferTurkey.API | Business, Data, Entity |
| MoneyTransferTurkey.Business | Data, Entity |
| MoneyTransferTurkey.Data | Entity |
| MoneyTransferTurkey.Entity | — |

## Frontend

Admin arayüzü ayrı bir Vue 3 + Vite projesidir (`frontend/`). API’ye CORS ile bağlanır (localhost:5173, 5174, 5175 vb.).

```mermaid
flowchart LR
  User[Kullanıcı] --> Vue[Vue 3 + Vite]
  Vue -->|REST + JWT| API[MoneyTransferTurkey.API]
  Vue -->|SignalR| Hub[/coinPriceHub]
```

- **Giriş:** `POST /api/v1/User/login`
- **Sayfalar:** Dashboard, User, Exchange, Vault, Party, Rates, Site, Blog, Coin
- Ayrıntı: [frontend/README.md](../frontend/README.md)

---

## Dokümantasyon indeksi

- **Görsel özet:** Tarayıcıda [docs/system-overview.html](system-overview.html) — tüm katmanlar ve bileşenler.
- **Frontend önizleme:** [docs/frontend-preview.html](frontend-preview.html) — ekran/API eşlemesi.
- **Döküman listesi:** [docs/README.md](README.md).
