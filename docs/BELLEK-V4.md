# BaskentEnerji – Döviz Muhasebe V4 – Proje Belleği

**Versiyon:** V4  
**Tarih:** 2026-02-24  
**Kaynak:** Cursor sohbetinden derlendi (V3 + yeni geliştirmeler)

---

## 1. Proje Kimliği

| Alan | Değer |
|------|-------|
| Proje adı | Başkent Enerji – Döviz Muhasebesi |
| Agent adı | `baskent-enerji-doviz-muhasebesi` |
| Domain | baskentenerji.com |
| API domain | api.baskentenerji.com |
| Uygulama adı (UI) | Döviz Bürosu – Döviz İşlemleri Yönetim Sistemi |
| UI başlığı | İhtiyarın Mekanı |
| Arayüz seçenekleri | İhtiyar Arayüz (aktif), Normal Arayüz (devre dışı) |

---

## 2. Teknoloji Yığını

### Backend (.NET)
- **Kaynak kod:** `C:\Users\Administrator\AnasıBerdus-Deniz\`
- **Proje adı:** AnasıTAS-Deniz (eski: SmileMedical), klasör: AnasıBerdus-Deniz (eski: QuantaQuokka_)
- **Framework:** .NET 8 (ASP.NET Core)
- **ORM:** Entity Framework Core + SQL Server provider
- **DB:** SQL Server Express (`localhost\SQLEXPRESS`), veritabanı: **mtt-moneyexchangeturkey** (şema: mtturkey_exchange). Referans: REFERANS_VERITABANI_VE_YAPILANDIRMA.md
- **Auth:** JWT Bearer (issuer: `baskentenerji`, audience: `baskentenerji`)
- **Password hash:** BCrypt.Net-Next 4.1.0 (+ legacy SHA256 desteği, auto-rehash)
- **E-posta:** MailKit (`info@baskentenerji.com`)
- **Push:** Firebase Admin SDK
- **API docs:** Swashbuckle (Swagger)
- **Diğer:** AutoMapper, HtmlAgilityPack, BouncyCastle, Azure.Identity
- **Publish komutu:** `dotnet publish -c Release -o "C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-output"`
- **Deploy:** Stop app pool → Copy files → Start app pool
- **App Pool:** `api.baskentenerji.com(domain)(pool)`

### Frontend (Web)
- **URL:** https://baskentenerji.com
- **Giriş:** https://baskentenerji.com/login
- **Dashboard:** https://baskentenerji.com/ihtiyar/dashboard
- **Arayüz:** İhtiyar (aktif) / Normal (devre dışı) seçimi login ekranında
- **Framework:** Vue 3 + Pinia + Vue Router (derlenmiş, minified)
- **Kaynak (eski/skeleton):** `C:\Users\Administrator\Desktop\QuantaQuokka\SmileMedical\frontend\`
- **Deploy path:** `C:\Inetpub\vhosts\baskentenerji.com\httpdocs\ihtiyar\`
- **Main bundle:** `assets/index-CnURhCj4.js`
- **Enhancement scripts** (ana bundle'dan SONRA yüklenen):
  1. `security-guard.js` - DevTools koruması, sağ tık engelleme
  2. `i18n-fix.js` - Türkçe çeviri düzeltmeleri (DOM üzerinden)
  3. `ana-kasa-enhance.js` - Ana kasa API interceptor, bakiye düzeltmeleri
  4. `dealer-panel-enhance.js` - Bayi panel, QR kod, admin sekmeler (**YENİ**)

### Telegram Bot (Python)
- **Python:** 3.12
- **Botlar:** main_bot (USDT), ruble_bot (Ruble; **mtr-moneytransferruble**), operator_bot (operatör), crypto bot (**mtc-moneytransfercrypto**)
- **Eski bot:** `C:\Users\Administrator\Desktop\bot\` (Flask web panel dahil)
- **Yeni bot:** `C:\Users\Administrator\Desktop\yeni_bot\`

---

## 3. Dosya Yapısı

### Backend Entity/Controller Dosyaları
```
AnasıBerdus-Deniz/
├── AnasıTAS-Deniz.API/
│   ├── Controllers/
│   │   ├── ExchangeOffice/
│   │   │   ├── ExchangeController.cs
│   │   │   ├── ExchangeAutoRateController.cs
│   │   │   ├── VaultSnapshotController.cs
│   │   │   └── DealerController.cs        ← YENİ (Bayi CRUD + QR)
│   │   ├── UserController.cs
│   │   └── ...
│   ├── appsettings.json (prod: gerçek conn string var)
│   └── web.config (stdoutLogEnabled=true)
├── AnasıTAS-Deniz.Entity/
│   └── Entities/ExchangeOffice/
│       ├── Dealer/Dealer.cs               ← YENİ
│       └── Office/Office.cs
├── AnasıTAS-Deniz.Data/
│   └── Contexts/AnasıTAS-DenizDbContext.cs (Dealers DbSet ekli)
└── AnasıTAS-Deniz.Business/
    └── Services/User/UserServiceCommand.cs (BCrypt + SHA256 dual verify)
```

### Frontend Enhancement Scripts
```
C:\Inetpub\vhosts\baskentenerji.com\httpdocs\ihtiyar\
├── index.html (4 script yüklüyor)
└── assets/
    ├── index-CnURhCj4.js (main Vue bundle)
    ├── index-rfaceH9f.css
    ├── security-guard.js
    ├── i18n-fix.js
    ├── ana-kasa-enhance.js
    ├── dealer-panel-enhance.js        ← YENİ
    ├── ModernExchange-DTr3LdCV.js
    ├── ModernExchangeV2-57LPlErx.js
    ├── ModernExchangeRates-UbOk9MpZ.js
    ├── VaultManagement-DKQAAqEB.js
    ├── VaultView-DXZsyW_I.js
    ├── VaultSnapshot-CvjXcCGs.js
    ├── VaultCountManagement-IX7g2OG1.js
    ├── ModernZReport-Cw3sNoTo.js
    ├── ModernZReportV2-5Bi3ideo.js
    ├── ModernUserManagementV2-CSj5oCU9.js
    ├── ModernSettings-YX1yUOg-.js
    ├── ModernPartyAccounts-C_wyPak8.js
    ├── ModernGhostParty-DOLmbKt2.js
    ├── AutoRateManagement-D7fzKJ-r.js
    ├── ExpensesManagement-rFh21D6k.js
    ├── CurrencyManagement-9EewMFzY.js
    └── BinanceDeposits-B-iKI9db.js
```

### Desktop Referans Dosyaları
```
C:\Users\Administrator\Desktop\
├── QuantaQuokka/SmileMedical/frontend/  (eski Vue kaynak, skeleton)
├── BASKENT_PROJE/                       (bellek, güvenlik raporu, publish çıktısı)
├── bot/                                 (eski Telegram bot + Flask web panel)
│   ├── web_operator_panel_v2.py         (Flask dealer/operator panel)
│   └── templates/
│       ├── admin_panel_v2.html          (büyük admin panel HTML)
│       ├── dealer_panel.html            (bayi paneli)
│       └── login.html
├── yeni_bot/                            (yeni Telegram bot sürümü)
├── exchange-office-fix/                 (deploy/fix scriptleri)
├── Scriptler/
├── Yedekler/
└── CLAUDE BELLEK/
```

---

## 4. API Endpoint Haritası

### Mevcut (önceden var olan)
| Yol | Açıklama |
|-----|----------|
| POST /api/v1/User/login | Giriş (Mail + Password) |
| GET /api/v1/Exchange/currency | Para birimleri |
| GET /api/v1/Exchange/offices/summary | Ofis/şube özet |
| GET /api/v1/exchange/dashboard | Dashboard verileri |
| POST /api/v1/Exchange/exchange | İşlem oluştur |
| GET /api/v1/Exchange/exchange-by-vault/{id} | Kasa işlemleri |
| GET /api/v1/Exchange/vault/{id} | Kasa detay |
| POST /api/v1/Exchange/vault/transfer | Kasalar arası transfer |
| GET /api/v1/Exchange/exchange-rate | Kurlar |
| PUT /api/v1/Exchange/exchange-rate | Kur güncelle |
| GET /api/v1/Exchange/auto-rate/* | Otomatik kur |
| GET /api/v1/Exchange/party | Cari hesaplar |
| POST /api/v1/Exchange/party/account/entry | Cari giriş |
| GET /api/v1/Exchange/vault-snapshot | Kasa kaydı |
| GET /api/v1/Exchange/vault-count | Sayım |
| GET /api/v1/Exchange/expense/* | Gider |
| GET /api/v1/User | Kullanıcılar |

### YENİ (Bu oturumda eklenen)
| Yol | Açıklama |
|-----|----------|
| GET /api/v1/exchange/dealer | Bayi listesi (officeId filtre) |
| GET /api/v1/exchange/dealer/{id} | Bayi detay |
| POST /api/v1/exchange/dealer | Bayi oluştur |
| PUT /api/v1/exchange/dealer/{id} | Bayi güncelle |
| DELETE /api/v1/exchange/dealer/{id} | Bayi sil (soft) |
| POST /api/v1/exchange/dealer/{id}/generate-qr | QR kod oluştur |
| GET /api/v1/exchange/dealer/{id}/qr | QR veri al |
| GET /api/v1/exchange/dealer/check-role | Rol kontrol (admin/user, offices, tabs) |

---

## 5. Veritabanı

### Tablo: Dealers (YENİ)
```sql
CREATE TABLE Dealers (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    DealerName nvarchar(200) NOT NULL,
    DealerCode nvarchar(50) NOT NULL UNIQUE,
    ContactPerson nvarchar(200) NULL,
    Phone nvarchar(50) NULL,
    Email nvarchar(200) NULL,
    Address nvarchar(500) NULL,
    Description nvarchar(1000) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    OfficeId uniqueidentifier NOT NULL REFERENCES Offices(Id),
    QrData nvarchar(max) NULL,
    QrGeneratedDate datetime2 NULL,
    CreatedDate datetime2 NOT NULL,
    UpdatedDate datetime2 NULL,
    DeletedDate datetime2 NULL
);
```

### Sistem sahibi (Owner)
| Alan | Değer |
|------|-------|
| Nick (username) | **ihtiyar** |
| Gerçek isim | **Cem Kul** |

### Kullanıcılar (Rank enum)
| Rank | Değer | Açıklama |
|------|-------|----------|
| 100 | Owner | Sistem sahibi (ihtiyar / Cem Kul) |
| 99 | Admin | Tüm yetkiler, tüm bayiler için QR |
| 2 | User/Operatör | Şube bazlı, sadece kendi bayileri |
| 1 | Customer | Müşteri |
| 0 | Banned | Engellenmiş |

### Giriş Bilgileri
- **Owner:** username=`ihtiyar`, gerçek isim **Cem Kul**, şifre: `owner1`
- **Admin:** username=`Damat`, şifre: `admin1`
- **Personel:** username=`PERSONEL`, şifre: `personel1`
- Panel girişi: **Mail** veya **Username** + **Şifre** (API hem `Mail` hem `email` alanını kabul eder).
- Giriş sorunu / canlı site / Plesk güncelleme: **CANLI_SITE_PLESK_GUNCELLEME.md**

---

## 6. Dashboard Kartları (Admin Görünümü)

Dashboard'da 6 ana kart var (admin için):

1. **DÖVİZ İŞLEMLERİ** → `/ihtiyar/exchange-v2`
   - Badges: USD, EUR, GBP, TRY
   - Subtitle: "Döviz alış-satış işlemleri" (önceki hatalı metin: "Buna bascan mahooo" düzeltildi)
2. **Z RAPORU** → `/ihtiyar/z-report`
   - Badges: Günlük, Aylık, Yıllık, Özet
3. **CARİLER** → `/ihtiyar/parties`
   - Badges: Alacak, Borç, Bakiye, Tahsilat
4. **ŞUBELER** → `/ihtiyar/vaults`
   - Badges: Nakit, Döviz, Çek, Pos
5. **GİDERLER** → `/ihtiyar/expenses`
   - Badges: Fatura, Kira, Maaş, Diğer
6. **KULLANICILAR** → `/ihtiyar/users`
   - Badges: Admin, User, Yetki, Aktif

**Şube kullanıcıları (non-admin)** sadece 1 kart görüyor: DÖVİZ İŞLEMLERİ

Ayrıca ANA KASA bölümü var:
- Toplam Varlık, Günlük K/Z, Aylık K/Z, İşlem Sayısı
- Şube Performans Karşılaştırması
- Hızlı İşlemler (Kurlar, Cari, Kasa, K/Z Raporu, Z Raporu, Transfer, Sermaye, Son İşlemler)

---

## 7. Enhancement Script Detayları

### dealer-panel-enhance.js (YENİ - v20260224a)
- **Dashboard düzeltmesi:** "Buna bascan mahooo" → "Döviz alış-satış işlemleri"
- **Döviz İşlemleri sekmeler:**
  - Operatör Paneli (mevcut exchange UI)
  - Admin Panel (sadece admin) - ofis/bayi istatistikleri, bayi CRUD
  - Bayi QR Oluştur - bayi seçip QR kod oluşturma/indirme
- **Rol bazlı:** Admin tüm bayileri görür, şube kullanıcısı sadece kendi bayilerini
- **Bayi CRUD modal:** Yeni bayi ekle/düzenle formu
- **QR kodu:** qrcode.js CDN ile canvas üzerinde render
- **Toast bildirimleri:** Başarı/hata mesajları
- **CSS:** Tamamen self-contained (bke- prefix)

### ana-kasa-enhance.js
- Vault ID düzeltme (fetch interceptor)
- 404 retry logic
- Bakiye formatlama

### i18n-fix.js
- DOM-based Türkçe çeviri
- MutationObserver ile SPA navigasyon desteği
- Kasa → Şube terminoloji değişikliği
- Vault status badge çevirisi
- Kullanıcı rol badge çevirisi

### security-guard.js
- DevTools engelleme (F12, Ctrl+Shift+I)
- Sağ tık context menu engelleme
- Debugger tuzakları
- Console uyarısı

---

## 8. Bilinen Sorunlar ve Yapılacaklar

### TAMAMLANAN
- [x] Dealer entity + migration + DB tablo oluşturma
- [x] DealerController (CRUD + QR + rol kontrolü)
- [x] dealer-panel-enhance.js script oluşturuldu
- [x] "Buna bascan mahooo" metin hatası düzeltildi
- [x] API deploy edildi (app pool restart)
- [x] Login çalışıyor (SHA256 hash ile)
- [x] Dashboard yükleniyor (ANA KASA + kartlar görünüyor)

### TAMAMLANAN (2026-02-24 devam)
- [x] Sağ tık + zorla yenileme "banner"ı → security-guard.js'de mevcut ("Sayfa korunuyor. Yenilemek için F5 kullanın.")
- [x] Admin paneli kartları: Dashboard + 6 kart (Döviz, Z Raporu, Cariler, Şubeler, Giderler, Kullanıcılar) açıldı, Türkçe başlık/alt metinler düzgün
- [x] Döviz İşlemleri sekmeleri: Admin/Owner için 3 sekme (Operatör, Yönetici Paneli, Bayi QR); çalışıyor
- [x] Şube kullanıcıları: Döviz İşlemleri sayfasında artık 2 sekme (Operatör Paneli + Bayi QR Oluştur); Yönetici Paneli sadece admin/owner görür (dealer-panel-enhance.js v20260224m)
- [x] Owner yetkisi: Backend Rank enum'a Owner=100 eklendi, ValidationService IsAdminAsync rank 99 ve 100 kabul ediyor; API deploy edildi

### TAMAMLANAN (görev sırası – son oturum)
- [x] **Bayi dashboard:** Vue tarafında tamamlandı. Döviz İşlemleri → Bayi QR sekmesi (admin/şube) + Dashboard'da şube kullanıcısı için "Bayi QR Oluştur" kartı (dealer-panel-enhance.js). Eski Flask bot paneli artık opsiyonel; bayi işlemleri Vue panelden yapılıyor.
- [x] **Tüm kartlardaki görsel bozuklukları:** Z Raporu sayfasında Exchange Office→Döviz Bürosu, OFİS SEÇİMİ→ŞUBE SEÇİMİ, Tüm Ofisler→Tüm Şubeler (i18n-fix.js + fixHeaderTitle genişletildi).
- [x] **Şube paneli kartları:** Şubeler sayfası incelendi; metinler tutarlı (Şube İşlemleri, Yeni Şube, TOPLAM ŞUBE). Düzenleme gerekmedi.
- [x] **i18n-fix.js eksik çeviriler:** Rapor tipi, Tarih aralığı, All offices, No data available, Generate report, Apply, Clear, Today/This week/month/year, Custom, Start/End date, Search, New, Edit, Remove, Submit, Loading data..., Save changes, silme onay metni vb. eklendi.
- [x] **appsettings güvenlik:** API `C:\ProgramData\BaskentEnerji\api-secrets.json` okuyor; klasör ACL (sadece App Pool + Admin); api-secrets.example.json ve README.txt eklendi.

### TAMAMLANAN (Flask bayi yönlendirme)
- [x] **Eski Flask bayi paneli yönlendirme:** `/dealer`, `/dealer/login`, `/dealer/panel` artık `dealer_redirect.html` gösteriyor (5 sn sonra baskentenerji.com/ihtiyar/exchange-v2). Bot projesi: `Desktop\bot\` — `web_operator_panel_v2.py` ve `templates\dealer_redirect.html` güncellendi.

### DEVAM EDECEK (İsteğe bağlı / sonraki oturum)
- [ ] Yeni sayfalarda çıkan İngilizce metinler için i18n-fix.js'e ek çeviri ekleme.

### BİLİNEN HATALAR
- **IIS env vars:** `ConnectionStrings__SQL` machine env var IIS app pool'dan okunamıyor → api-secrets.json veya appsettings kullanın.
- **console.debug("API Error"):** Dashboard yüklenirken bazen API hatası (exchange-rate veya geçici); retry mevcut.
- **i18n:** Nadiren "common.loading" vb. key'ler kısa süre görünebilir; erken fixAll ile azaltıldı.

---

## 9. Deploy Prosedürü

### Backend API Deploy
```powershell
# 1. Build
cd C:\Users\Administrator\AnasıBerdus-Deniz
dotnet publish AnasıTAS-Deniz.API\AnasıTAS-Deniz.API.csproj -c Release -o "C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-output"

# 2. Stop app pool
Import-Module WebAdministration
Stop-WebAppPool -Name "api.baskentenerji.com(domain)(pool)"
Start-Sleep -Seconds 5

# 3. Copy files
Copy-Item -Path "C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-output\*" -Destination "C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com\" -Recurse -Force

# 4. Start app pool
Start-WebAppPool -Name "api.baskentenerji.com(domain)(pool)"
```

### Frontend Enhancement Deploy
Doğrudan dosyaları düzenle:
```
C:\Inetpub\vhosts\baskentenerji.com\httpdocs\ihtiyar\assets\<script>.js
C:\Inetpub\vhosts\baskentenerji.com\httpdocs\ihtiyar\index.html
```
Cache-busting: `?v=` parametresini güncelle.

---

## 10. Pinia Store Erişimi (Enhancement Script'lerden)

```javascript
// Token al
const app = document.getElementById('app').__vue_app__;
const token = app.config.globalProperties.$pinia._s.get('auth')?.token;

// User bilgisi
const user = app.config.globalProperties.$pinia._s.get('auth')?.user;
const isAdmin = user?.rank === 99;

// Router
const router = app.config.globalProperties.$router;
router.push('/ihtiyar/exchange-v2');
```

---

## 11. Önceki Oturum Transkripti

Tam sohbet transkripti: `e8eba63d-2d5b-4277-98bd-bd1fa27a8c38`
