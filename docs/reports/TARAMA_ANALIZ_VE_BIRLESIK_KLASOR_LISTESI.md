# Yerel PC Tarama — Analiz ve Tek Klasör Birleştirme Listesi

**Tarih:** 18.03.2026  
**Önemli:** Bu dosya sadece LİSTE ve ÖNERİ içerir. **Onay verene kadar hiçbir taşıma veya birleştirme işlemi yapılmayacaktır.**

---

## 0. Baskent Enerji projesi ve özel isim taraması (ihtiyar, anasidas, avradiat)

### Baskent Enerji projesi — dahil
- **baskent-enerji-doviz** = Baskent Enerji ana projesi (API, panel, döviz, telegram bot). Birleşik klasörde **ana-repo** olarak yer alacak.
- **BASKENT_TELEGRAM_BOT** = Aynı projenin bot çalıştırma kopyası. Birleşik klasörde **telegram-bot-calistir** olarak yer alacak.

### İhtiyar
- **Projede kullanım:** Panel yolu `baskentenerji.com/ihtiyar/`, owner kullanıcı adı `ihtiyar`, deploy klasörü `httpdocs\ihtiyar\`, migration adları `mig_ihtiyar`, `mig_ihtiyar1` … `mig_ihtiyar8`.
- **Konum:** `baskent-enerji-doviz` ve yedeklerindeki **docs** (BELLEK-V4.md, ISIM_DEGISIKLIK_REFERANS.md, BASKENTENERJI_CALISMA_PRENSIPLERI.md) ve **BaskentEnerji.Data\Migrations** içinde geçiyor. Ayrı bir “ihtiyar” klasörü yok; hepsi Baskent Enerji projesi içinde.

### AnasıTAS / AnasıBerdus (anasidas benzeri isimler)
- **Dokümanlarda:** `AnasıTAS_Deniz.API`, `AnasıTAS_Deniz.Data`, `AnasıBerdus-Deniz` — BELLEK-V4.md ve REFERANS_VERITABANI_VE_YAPILANDIRMA.md içinde.
- **Belirtilen konum:** Sunucu tarafı `C:\Users\Administrator\AnasıBerdus-Deniz\` (yerel Projeler altında bu isimde klasör **yok**).
- **Yerel:** Bu isimde klasör Documents\Projeler’de taranmadı; sadece doküman referansı var.

### Avradiat / avradiyat
- **Tarama:** Projeler altında **avradiat** veya **avradiyat** geçen dosya/klasör **bulunamadı**. Farklı yazılış veya başka sürücü/konumda olabilir.

---

## 1. Taranan konumlar (Projeler altı)

| # | Klasör / dosya | Konum | Tahmini dosya | Tahmini alt klasör | Açıklama |
|---|----------------|--------|----------------|--------------------|----------|
| 1 | **baskent-enerji-doviz** | `Documents\Projeler\baskent-enerji-doviz` | ~2164 | ~267 | Ana proje: Git repo. BaskentEnerji.API, .Business, .Data, .Entity, .Tests, docs, scripts (dekont-referans-matcher + .venv), telegram-bot. |
| 2 | **BASKENT_TELEGRAM_BOT** | `Documents\Projeler\BASKENT_TELEGRAM_BOT` | ~43 | 1 | Telegram botlarının çalıştırma kopyası (.env, run_all.py, START_BOTLAR.*, rehberler). |
| 3 | **_yedek_baskent_enerji_doviz_20260318** | `Documents\Projeler\_yedek_baskent_enerji_doviz_20260318` | ~2143 | ~266 | baskent-enerji-doviz working tree yedeği (MoneyTransferTurkey yok; scripts, BaskentEnerji.*, telegram-bot kopyası). |
| 4 | **_yedek_MoneyTransferTurkey** | `Documents\Projeler\_yedek_MoneyTransferTurkey` | ~757 | ~135 | Eski proje yedeği: .NET (MoneyTransferTurkey.*), frontend, docs. Git repo değil. |
| 5 | **_yedek_telegram_bot_20260318** | `Documents\Projeler\_yedek_telegram_bot_20260318` | ~19 | 0 | Telegram bot klasörünün yedeği (.env dahil). |
| 6 | **PAT-depo-checklist-sablonu.md** | `Documents\Projeler\` | 1 | — | GitHub PAT checklist şablonu. |
| 7 | **RAPOR_Yerel_GitHub_Karsilastirma.md** | `Documents\Projeler\` | 1 | — | Yerel–GitHub karşılaştırma raporu. |

**Documents kökü (Projeler dışı):**

| # | Dosya | Konum | Açıklama |
|---|--------|--------|----------|
| 8 | **GitHub-PAT-Rehberi.md** | `Documents\GitHub-PAT-Rehberi.md` | GitHub PAT rehberi. |

---

## 2. Proje parçaları — ne nerede?

- **Baskent Enerji projesi (dahil):** Ana kaynak = `baskent-enerji-doviz` (API, Business, Data, Entity, Tests, docs, scripts, telegram-bot). İhtiyar panel yolu ve migration isimleri bu repo içinde.
- **Bot çalıştırma:** `BASKENT_TELEGRAM_BOT` (telegram-bot + .env + başlatma scriptleri).
- **Yedekler:** `_yedek_baskent_enerji_doviz_20260318`, `_yedek_telegram_bot_20260318`, `_yedek_MoneyTransferTurkey`.
- **Ortak dokümanlar:** Projeler kökündeki .md dosyaları + Documents\GitHub-PAT-Rehberi.md.
- **AnasıTAS/AnasıBerdus:** Sadece dokümanlarda referans; yerel klasör Projeler’de yok (sunucu yolu: Administrator\AnasıBerdus-Deniz).

---

## 3. Önerilen tek birleşik klasör yapısı

Aşağıdaki yapı **onayınızdan sonra** oluşturulacak; **onay vermeden hiçbir taşıma/kopyalama yapılmayacak.**

**Hedef kök:**  
`C:\Users\CmkL-Owner\Documents\Projeler\BASKENT_PROJE_TEKBIRLESIK`

```
BASKENT_PROJE_TEKBIRLESIK\
├── ana-repo\                    ← baskent-enerji-doviz TAM KOPYASI ( .git dahil )
│   ├── .git
│   ├── BaskentEnerji.API\
│   ├── BaskentEnerji.Business\
│   ├── BaskentEnerji.Data\
│   ├── BaskentEnerji.Entity\
│   ├── BaskentEnerji.Tests\
│   ├── docs\
│   ├── scripts\
│   ├── telegram-bot\
│   ├── .gitignore
│   └── ...
├── telegram-bot-calistir\       ← BASKENT_TELEGRAM_BOT içeriği ( bot çalıştırma )
│   ├── .env
│   ├── run_all.py
│   ├── START_BOTLAR.bat
│   ├── START_BOTLAR.ps1
│   └── ...
├── _yedekler\
│   ├── yedek_baskent_enerji_doviz_20260318\   ← _yedek_baskent_enerji_doviz_20260318
│   ├── yedek_telegram_bot_20260318\           ← _yedek_telegram_bot_20260318
│   └── yedek_MoneyTransferTurkey\             ← _yedek_MoneyTransferTurkey
└── dokumanlar\
    ├── PAT-depo-checklist-sablonu.md
    ├── RAPOR_Yerel_GitHub_Karsilastirma.md
    └── GitHub-PAT-Rehberi.md                   ← Documents\ kökünden kopya
```

**Not:** Taşıma yerine **kopyalama** yapılabilir (orijinaller Projeler altında kalır). İsterseniz “taşı” da seçilebilir; karar onayda belirtilmeli.

---

## 4. Yapılacak işlem listesi (onay sonrası)

| Sıra | İşlem | Kaynak | Hedef (birleşik klasör içi) |
|------|--------|--------|-----------------------------|
| 1 | Kopyala | `Projeler\baskent-enerji-doviz` (tümü) | `BASKENT_PROJE_TEKBIRLESIK\ana-repo\` |
| 2 | Kopyala | `Projeler\BASKENT_TELEGRAM_BOT` (tümü) | `BASKENT_PROJE_TEKBIRLESIK\telegram-bot-calistir\` |
| 3 | Kopyala | `Projeler\_yedek_baskent_enerji_doviz_20260318` | `BASKENT_PROJE_TEKBIRLESIK\_yedekler\yedek_baskent_enerji_doviz_20260318\` |
| 4 | Kopyala | `Projeler\_yedek_telegram_bot_20260318` | `BASKENT_PROJE_TEKBIRLESIK\_yedekler\yedek_telegram_bot_20260318\` |
| 5 | Kopyala | `Projeler\_yedek_MoneyTransferTurkey` | `BASKENT_PROJE_TEKBIRLESIK\_yedekler\yedek_MoneyTransferTurkey\` |
| 6 | Kopyala | `Projeler\PAT-depo-checklist-sablonu.md` | `BASKENT_PROJE_TEKBIRLESIK\dokumanlar\` |
| 7 | Kopyala | `Projeler\RAPOR_Yerel_GitHub_Karsilastirma.md` | `BASKENT_PROJE_TEKBIRLESIK\dokumanlar\` |
| 8 | Kopyala | `Documents\GitHub-PAT-Rehberi.md` | `BASKENT_PROJE_TEKBIRLESIK\dokumanlar\` |

- **.venv** ve **node_modules** gibi çok büyük klasörler kopyalanabilir veya “hariç tut” denebilir (onayda belirtin).
- **.git** ana-repo için dahil önerilir; böylece tek klasörde de Git geçmişi durur.

---

## 5. Onay gereksinimi

- Yukarıdaki **hedef klasör adı** ve **alt yapı** sizin için uygunsa,  
  **“Onaylıyorum, kopyala”** veya **“Onaylıyorum, taşı”** diye yazmanız yeterli.
- **Değişiklik isterseniz** (ör. farklı kök adı, yedekleri alma, .venv dahil etmeme):  
  Net yazın (örn. “Birleşik klasör adı X olsun”, “Yedekleri alma”, “.venv kopyalama”).

**Onay verene kadar hiçbir taşıma veya birleştirme işlemi yapılmayacaktır.**
