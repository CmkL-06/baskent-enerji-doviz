# MTT Paneller — Money Transfer Turkey HTML Arayüzleri

Bu klasör MoneyTransferTurkey sistemine ait dört HTML paneli içerir.

## Paneller

| Dosya | Açıklama | URL |
|-------|----------|-----|
| `login.html` | Panel giriş ekranı (tüm roller) | https://tg.moneytransferturkey.com/ |
| `dealer.html` | Bayi paneli (işlem takibi, müşteri listesi) | /dealer.html |
| `operator.html` | Operatör paneli (transfer onayı, durum) | /operator.html |
| `admin.html` | Yönetici paneli (sistem yönetimi, raporlar) | /admin.html |

## Teknoloji

- Bootstrap 5.3 (CDN)
- Font Awesome 6.5 (CDN)
- Vanilla JS + Fetch API
- JWT token tabanlı auth (LocalStorage)

## IIS Deploy

```powershell
# Panelleri deploy et
.\scripts\mtt\deploy-mtt-panels.ps1
```

IIS Fiziksel Yol: `C:\inetpub\mtt-panels\`
IIS Site Adı: `MoneyTransfer-Panels`

## API Bağlantısı

Paneller `https://api.moneytransferturkey.com` endpoint'ine bağlanır.
Yerel geliştirme: `http://localhost:5200`

## Kullanıcı Rolleri

| Rol | Panel | Yetki Seviyesi |
|-----|-------|----------------|
| owner/admin | admin.html | Tam yönetim |
| operator | operator.html | Transfer onayı |
| dealer | dealer.html | Kendi işlemleri |
