# GitHub – Kurumsal Yapı

**Amaç:** Tek repo, standart dal ve klasör yapısı ile kurumsal GitHub kullanımı.

---

## 1. Repo ve dal standartları

| Öğe | Kurumsal değer | Açıklama |
|-----|----------------|----------|
| **Repo adı** | `baskent-proje` veya `baskentenerji-doviz-api` | Tek repo; tüm kaynak ve belgeler burada. |
| **Varsayılan dal** | `main` | Yayın dalı. Eski `master` varsa `main` ile değiştirilir. |
| **Kullanıcı/Organizasyon** | Örn. `CmkL-06` veya kurumsal org | Tüm projeler aynı hesap/org altında. |

**Repo URL örneği:** `https://github.com/CmkL-06/baskent-proje` (veya mevcut adınız).

---

## 2. Kurumsal klasör yapısı (repo kökü)

```
baskent-proje/          # veya baskentenerji-doviz-api
├── README.md           # Proje özeti, klasör yapısı, hızlı başlangıç
├── .gitignore          # appsettings*, publish-api/, secrets, build çıktıları
├── deploy-api-to-canli.ps1
├── docs/               # Tüm belgeler (Plesk, IIS, güvenlik, referans)
├── kaynak-kod/         # .NET çözümü (BaskentEnerji.*)
├── scripts/            # Seed, test, IIS, örnek config
└── telegram-bot/       # Bot kodları
```

- **publish-api/** yerelde olabilir; `.gitignore` ile repo’ya **eklenmez** (build çıktısı).
- **docs/** içindeki tüm .md dosyaları repo’da tutulur; güncel tek referans bu yapıdır.

---

## 3. Commit ve push kuralları

| Yapılır | Yapılmaz |
|---------|----------|
| Kaynak kod (kaynak-kod/), docs/, scripts/ | appsettings*.json, firebaseConfig*, .env, *.pfx |
| deploy-api-to-canli.ps1, .gitignore | publish-api/ içeriği, logs/, şifre içeren dosyalar |
| README ve docs güncellemeleri | Hassas bilgi içeren belgeler |

`.gitignore` bu kuralları zaten yansıtmalı; commit öncesi `git status` ile kontrol edin.

---

## 4. Yerel → GitHub bağlantısı (tek sefer)

```powershell
cd C:\Users\Administrator\Desktop\BASKENT_PROJE
git init -b main
git remote add origin https://github.com/CmkL-06/baskent-proje.git
git add .
git status   # appsettings, publish-api, .vs, obj gelmemeli
git commit -m "Kurumsal yapı: docs, kaynak-kod, scripts"
git push -u origin main
```

**Not:** BASKENT_PROJE şu an git repo değilse yukarıdaki `git init` gerekir. Zaten `kaynak-kod` içinde .git varsa, tek repo için ya kökte birleştirilir ya da sadece kök repo kullanılır (kaynak-kod içeriği köke taşınır veya submodule yapılır). Kurumsal tercih: **tek repo, kök = BASKENT_PROJE**, kaynak-kod bir klasör olarak içeride kalır; kaynak-kod içindeki .git kaldırılıp tüm proje tek repo yapılabilir.

---

## 5. Plesk ile entegrasyon

- Plesk **Git** eklentisi ile “Deploy from repository” kullanılıyorsa:
  - **Repo URL:** Yukarıdaki tek repo (örn. `https://github.com/CmkL-06/baskent-proje.git`).
  - **Dal:** `main`.
  - **Deploy yolu:** Sadece API için gerekliyse `kaynak-kod` veya publish çıktısı; canlıda **appsettings.json ve web.config** Plesk/sunucu tarafında korunur (üzerine yazılmaz).

Bu yapı ile GitHub kurumsal tek repo standardına getirilir.
