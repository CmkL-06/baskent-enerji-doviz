# Birleşik Klasör — GitHub Karşılaştırma Raporu

**Tarih:** 18 Mart 2026 (güncelleme: aynı gün)  
**Hedef klasör:** `M:\BASKENT_PROJE_TEKBIRLESIK` *(taşındı)*  
**Karşılaştırılan repo:** `ana-repo` (kaynak: baskent-enerji-doviz) ↔ GitHub

---

## 0. Güç karşılaştırması ve yapılan optimizasyon (18 Mart 2026)

| Kaynak | Durum |
|--------|--------|
| **GitHub** (origin/BASKENT-DOVIZ) | En güçlü yapı — canonical yapı, ihtiyar panel, API düzenlemeleri |
| **Yerel (M:\...\ana-repo)** | GitHub ile **senkron**; veri korundu, hata yapılmadı |

**Yapılanlar (test edilerek):**
- **Karşılaştırma:** `git fetch` + commit sayıları kontrol edildi; yerel ve uzak aynı commit’te (b53b4db) bulunuyordu.
- **Tek fark:** Yerelde `.gitignore` içinde `.env` / `.env.*` güvenlik kuralı vardı, GitHub’da yoktu.
- **Optimize:** Bu kural **veriyi korumak** için commit edilip GitHub’a pushlandı. HEAD artık `1ced5e7` (chore: add .env to gitignore for security). Yerel = GitHub; güncel ve güçlü yapı her iki tarafta.
- **Uyarı:** GitHub Dependabot 3 adet high güvenlik açığı bildirdi: [baskent-enerji-doviz/security/dependabot](https://github.com/CmkL-06/baskent-enerji-doviz/security/dependabot) — isteğe bağlı inceleme önerilir.

---

## 1. Birleşik klasör yapısı (güncel konum: M:)

| Bölüm | Konum (M: sürücüsü) |
|--------|----------------------|
| **ana-repo** | `M:\BASKENT_PROJE_TEKBIRLESIK\ana-repo` |
| **telegram-bot-calistir** | `M:\BASKENT_PROJE_TEKBIRLESIK\telegram-bot-calistir` |
| **_yedekler** | `M:\BASKENT_PROJE_TEKBIRLESIK\_yedekler` |
| **dokumanlar** | `M:\BASKENT_PROJE_TEKBIRLESIK\dokumanlar` |

Proje M: sürücüsüne taşındı; tek kaynak `M:\BASKENT_PROJE_TEKBIRLESIK`.

---

## 2. GitHub bağlantısı (ana-repo)

- **Remote:** `origin` → `https://github.com/CmkL-06/baskent-enerji-doviz.git`
- **Varsayılan branch:** `origin/HEAD` → `origin/BASKENT-DOVIZ`
- **Yerel branch:** `BASKENT-DOVIZ` (izlenen: `origin/BASKENT-DOVIZ`)

---

## 3. Yerel ↔ GitHub durumu (ana-repo) — güncel

| Özet | Değer |
|------|--------|
| **Yerel (M: ana-repo)** | GitHub ile **senkron** |
| GitHub’da olup yerelde olmayan commit | 0 |
| Yerelde olup GitHub’da olmayan commit | 0 |

### Yerel (HEAD) son commit
```
1ced5e7 chore: add .env to gitignore for security
b53b4db docs: align frontend route table with dashboard path
```

### GitHub (origin/BASKENT-DOVIZ) son 5 commit
```
1ced5e7 chore: add .env to gitignore for security
b53b4db docs: align frontend route table with dashboard path
627e465 fix: redirect root dashboard path to /ihtiyar entry
d030920 fix: lock panel navigation to named production routes
ed6c4d7 fix: enforce /ihtiyar entry contract and subpath auth routing
```

Yerel ve GitHub senkron. Güncel kalmak için `M:\BASKENT_PROJE_TEKBIRLESIK\ana-repo` içinde `git pull origin BASKENT-DOVIZ` kullanın.

---

## 4. Çalışma ağacı farkları (ana-repo) — güncel

Son kontrolde yerel çalışma ağacı **temiz** (`.gitignore` değişikliği commit edilip pushlandı). Yerel = GitHub. İleride değişiklik sonrası senkron için:

1. Güncel hali almak: `git pull origin BASKENT-DOVIZ` (çakışma çıkarsa çözülmeli).
2. Veya yerel değişiklikleri koruyup sadece raporlamak: Bu rapor yeterli.

---

## 5. Özet

- **Birleşik klasör:** `M:\BASKENT_PROJE_TEKBIRLESIK` — ana-repo, telegram-bot-calistir, _yedekler, dokumanlar.
- **GitHub karşılaştırması:** `ana-repo` = baskent-enerji-doviz; remote `origin`, branch `BASKENT-DOVIZ`. Yerel ve GitHub **senkron** (HEAD: 1ced5e7). `.env` güvenlik kuralı commit/push ile GitHub’a eklendi; veri korundu.
- **İleride:** `M:\BASKENT_PROJE_TEKBIRLESIK\ana-repo` içinde `git pull` / `git push` ile senkron kalın; periyodik kontrol için `Kontrol_GitHub_Senkron.ps1` kullanın.
