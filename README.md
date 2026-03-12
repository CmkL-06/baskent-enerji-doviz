# Başkent Enerji - Döviz Muhasebe Sistemi

Döviz alış-satış, kasa yönetimi, cari hesap takibi ve raporlama platformu.

## Teknoloji

| Katman | Teknoloji |
|--------|-----------|
| Backend | .NET 8, ASP.NET Core Web API, Entity Framework Core |
| Frontend | Vue 3, Pinia, Vue Router, Tailwind CSS |
| Veritabanı | SQL Server Express |
| Sunucu | IIS / Plesk Panel |

## Proje Yapısı

```
BaskentEnerji.API/           → Web API katmanı (Controller'lar, HostService) [KANONIK]
BaskentEnerji.Business/      → İş mantığı (Servisler, Validasyon) [KANONIK]
BaskentEnerji.Data/          → Veritabanı (DbContext, Migration) [KANONIK]
BaskentEnerji.Entity/        → Entity modelleri, Enum'lar, DTO'lar [KANONIK]
MoneyTransferTurkey/         → Legacy eşlenik backend (referans, release dışı)
docs/                        → Teknik dokümantasyon arşivi
scripts/                     → Operasyon, test ve deploy scriptleri
telegram-bot/                → Telegram bot entegrasyon servisleri
external/                    → Çekirdek proje dışı arşiv/veri klasörleri
```

## Tek ve Güçlü Proje Kararı

- **Kanonik backend:** `BaskentEnerji.*`
- **Legacy ağaç:** `MoneyTransferTurkey/` (geriye dönük referans; yeni geliştirme hedefi değil)
- **Release paketi:** `external/` ve legacy ağaç varsayılan olarak hariç tutulur

## Tek Depo Konsolidasyonu

Bu repo, proje parçalarını tek yerde toplar:

- Uygulama kaynak kodu (`BaskentEnerji.*` kanonik, `MoneyTransferTurkey/*` legacy referans)
- Operasyon ve test scriptleri (`scripts/`)
- Teknik dokümantasyon (`docs/`)
- Telegram bot bileşenleri (`telegram-bot/`)
- Harici arşiv verileri (`external/`) – çekirdek dağıtım paketine dahil edilmemeli

## Yetki Hiyerarşisi

| Rank | Rol | Yetki |
|------|-----|-------|
| 100 | Owner | Tam yetki (kullanıcı/şube CRUD dahil) |
| 99 | Admin | Döviz işlemleri, raporlar, cari (kullanıcı/şube CRUD hariç) |
| 2 | Operatör | Kendi şubesindeki döviz işlemleri |
| 1 | Müşteri | Sınırlı erişim |
| 0 | Banned | Engelli |

## API

Base URL: `https://api.baskentenerji.com/api/v1`

## Kurulum

```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./output
```

> `appsettings.json` dosyası git dışında tutulur. Sunucuda manuel yapılandırılmalıdır.
