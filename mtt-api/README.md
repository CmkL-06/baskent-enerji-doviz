# MTT API — Money Transfer Turkey Minimal API

.NET 8 Minimal API · JWT Bearer Auth · EF Core · BCrypt · MSSQL

## Proje Yapısı

```
mtt-api/
├── Program.cs               ← Startup, middleware, EF, JWT, CORS
├── MoneyTransfer.API.csproj
├── appsettings.json         ← Yalnızca fallback değerler (gerçek değerler env'de)
├── Models/
│   ├── User.cs              ← Kullanıcı modeli (Role: admin/operator/dealer)
│   ├── Dealer.cs            ← Bayi
│   ├── Operator.cs          ← Operatör (IsAdmin flag)
│   ├── Transaction.cs       ← Para transferi
│   ├── ExchangeRate.cs      ← Döviz kuru
│   ├── ChatMessage.cs       ← Mesajlaşma
│   ├── CryptoDeposit.cs     ← Kripto yatırım
│   ├── LoginLog.cs          ← Giriş log
│   └── ErrorLog.cs          ← Hata log
├── Data/
│   └── MttDbContext.cs      ← EF Core DbContext
├── Helpers/
│   └── JwtHelper.cs         ← JWT token üretici
└── Endpoints/
    ├── AuthEndpoints.cs     ← POST /api/auth/login, /api/auth/change-password
    ├── DealerEndpoints.cs   ← /api/dealer/*
    ├── OperatorEndpoints.cs ← /api/operator/*
    └── AdminEndpoints.cs    ← /api/admin/*
```

## Ortam Değişkenleri (Machine Level)

| Değişken | Açıklama |
|----------|----------|
| `MTT_DB_CONNECTION` | MSSQL bağlantı dizesi |
| `MTT_JWT_SECRET` | JWT imzalama anahtarı (min 32 karakter) |

## Endpoint'ler

### Auth (Anonim)
- `POST /api/auth/login` — `{username, password, panelType}` → `{token, role, user}`
- `POST /api/auth/change-password` — `{oldPassword, newPassword}` (JWT gerekli)

### Dealer (JWT: dealer/admin)
- `GET /api/dealer/dashboard`
- `GET /api/dealer/transactions`

### Operator (JWT: operator/admin)
- `GET /api/operator/transactions`
- `PUT /api/operator/transactions/{id}/status`

### Admin (JWT: admin)
- `GET /api/admin/dashboard`
- `GET /api/admin/users`

## Build ve Deploy

```powershell
# Env ayarla
.\scripts\mtt\set-mtt-env-vars.ps1

# Build + Deploy
.\scripts\mtt\deploy-mtt-api.ps1

# Test
.\scripts\mtt\quick-api-test.ps1
```

## IIS Yapılandırması

- Site Adı: `MoneyTransfer-API`
- App Pool: `MoneyTransfer-API-Pool` (identity: **LocalSystem**)
- Port: 5200 (dış) + 5000 (localhost)
- Deploy Path: `C:\inetpub\moneytransfer-api\`
- IIS Hosting: aspNetCore module, stdoutLogEnabled

## Veritabanı

- Server: `localhost\SQLEXPRESS`
- Database: `mtt-moneyexchangeturkey` (mevcut exchange DB)
- Auth: Integrated Security=True (LocalSystem identity gerektirir)
- Schema: Ayrı schema (Users, Dealers, Operators, Transactions, ...)
