# Başkent Enerji - Döviz Muhasebe Sistemi

Döviz alış-satış, kasa yönetimi, cari hesap takibi ve raporlama platformu.

## Teknoloji

| Katman | Teknoloji |
|--------|-----------|
| Backend | .NET 8, ASP.NET Core Web API, Entity Framework Core |
| Veritabanı | SQL Server |
| Sunucu | IIS / Plesk Panel |
| Yardımcı Servis | Telegram bot entegrasyonu |

## Proje Yapısı (Kanonik)

```
BaskentEnerji.API/           → Web API katmanı (Controller, middleware, host)
BaskentEnerji.Business/      → İş kuralları ve servisler
BaskentEnerji.Data/          → DbContext, migration, repository erişimi
BaskentEnerji.Entity/        → Entity, DTO, enum, request/response modelleri
BaskentEnerji.Tests/         → Test projeleri
BaskentEnerji.sln            → Ana çözüm dosyası
MoneyTransferTurkey/         → Legacy referans ağacı (yeni geliştirme hedefi değil)
docs/                        → Dokümantasyon
scripts/                     → Operasyon, test ve paketleme scriptleri
telegram-bot/                → Telegram entegrasyon kodları
external/README.md           → Repo dışı veri politikası notu
```

## Tek ve Güçlü Proje Kararı

- **Kanonik backend:** `BaskentEnerji.*`
- **Legacy backend:** `MoneyTransferTurkey/` (sadece referans)
- **Repo politikası:** Proje dışı kişisel/harici veri Git altında tutulmaz.

Detaylı strateji: `docs/TEK_PROJE_STRATEJISI.md`

## Güvenlik ve Yetkilendirme Prensibi

- Login/register/public endpoint dışındaki API endpoint'leri `[Authorize]` ile korunur.
- Public endpoint'ler açıkça `[AllowAnonymous]` ile işaretlenir.
- Secret değerler (`appsettings*.json`, firebase config vb.) repoya eklenmez.

## Hızlı Başlangıç

```bash
dotnet restore BaskentEnerji.sln
dotnet build BaskentEnerji.sln -c Release
dotnet test BaskentEnerji.sln -c Release --no-build
```

## Repo Sağlık Komutları

```bash
python3 scripts/single_project_audit.py
bash scripts/package_single_project.sh
```
