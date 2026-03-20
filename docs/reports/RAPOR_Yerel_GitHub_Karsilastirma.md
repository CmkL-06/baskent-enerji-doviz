# Yerel Projeler – GitHub Depoları Karşılaştırma Raporu

**Tarih:** 18 Mart 2026  
**Referans:** En güncel veri = GitHub (origin). Eski/yerelde farklı veriler yedeklendi; hiçbir dosya silinmedi.

---

## 1. Özet

| Proje | Git repo | GitHub remote | Yerel HEAD | Remote HEAD | Durum |
|-------|----------|---------------|------------|-------------|--------|
| **baskent-enerji-doviz** | Evet | `https://github.com/CmkL-06/baskent-enerji-doviz.git` | `5e38ec9` | `b53b4db` | Yerel geride; çok sayıda silme (D) ve ekleme (A) |
| **_yedek_MoneyTransferTurkey** | Hayır | — | — | — | Sadece yerel yedek klasörü |

---

## 2. baskent-enerji-doviz – Derinlemesine İnceleme

### 2.1 Commit farkı

- **Yerel branch:** `BASKENT-DOVIZ` (HEAD = `5e38ec97bc8e1b90b03765a0524dc93ed98da92f`)
- **GitHub (referans):** `origin/BASKENT-DOVIZ` = `b53b4dbcf85a6893a3532b253438b41db0825625`
- **Sonuç:** Yerel, GitHub’dan **1 commit geride**. En güncel veri = GitHub.

### 2.2 Dosya durumu (git status – porcelain)

| Tür | Açıklama | Adet | Anlamı |
|-----|----------|------|--------|
| **D** (Deleted) | Yerelde silinmiş, GitHub’da var | **1092** | GitHub’daki en güncel veride bu dosyalar var. Yerelde yok. |
| **A** (Added) | Yerelde eklenmiş, GitHub’da yok | **741** | Sadece yerelde (staged). Yedeklendi. |

### 2.3 Silinen (D) – GitHub’da olan, yerelde olmayan

Tamamı **MoneyTransferTurkey/** altında:

- **MoneyTransferTurkey/** – API, Business, Data, testler, migration’lar, wwwroot uploads, vb.
- Örnek yollar:  
  `MoneyTransferTurkey/.gitattributes`, `MoneyTransferTurkey/MoneyTransferTurkey.API/`,  
  `MoneyTransferTurkey/MoneyTransferTurkey.Business/`, `MoneyTransferTurkey/MoneyTransferTurkey.Data/`,  
  `MoneyTransferTurkey/MoneyTransferTurkey.API.Tests/`, vb.

**Referans:** Bu dosyaların en güncel hali **GitHub’da** (origin/BASKENT-DOVIZ). Yerelde silinmiş oldukları için “eski” olan yerel durum; “güncel” olan GitHub’daki bu dosyalar.

### 2.4 Eklenen (A) – Sadece yerelde olan (yedeklenen)

- **scripts/dekont-referans-matcher/** – Dekont eşleştirme script’i ve **tüm .venv** (Python sanal ortamı, site-packages dahil).
- **telegram-bot/.env** – Ortam değişkenleri (muhtemelen gizli bilgi içerir).

Bu dosyalar GitHub’da yok; sadece yerelde. “Eski/yerelde kalan” veri olarak **yerel yedek klasörüne** kopyalandı; silinmedi.

### 2.5 GitHub’daki dosya sayısı (referans)

- **origin/BASKENT-DOVIZ** üzerinde toplam **1210** dosya (en güncel referans).

---

## 3. Yapılan İşlemler (asla silme yok)

1. **GitHub referans alındı:** `git fetch origin` ile `origin/BASKENT-DOVIZ` güncellendi.
2. **Farklar çıkarıldı:** Yerel vs origin durumu (D ve A) raporlandı.
3. **Yerel yedek oluşturuldu:** Mevcut yerel çalışma kopyası (working tree) aynen yedeklendi:
   - **Klasör:** `C:\Users\CmkL-Owner\Documents\Projeler\_yedek_baskent_enerji_doviz_20260318`
   - İçerik: O andaki tüm dosyalar (MoneyTransferTurkey/ hariç, çünkü yerelde zaten yok; scripts/, telegram-bot/.env ve diğer her şey dahil).
   - `.git` klasörü yedeklenmedi (sadece çalışma kopyası; tarih için bu rapor kullanılabilir).

---

## 4. Sonraki Adımlar (isteğe bağlı)

Yereli GitHub ile aynı hale getirmek isterseniz (en güncel veri = GitHub):

```powershell
cd "C:\Users\CmkL-Owner\Documents\Projeler\baskent-enerji-doviz"
git reset --hard origin/BASKENT-DOVIZ
```

- Bu komut: yereldeki tüm değişiklikleri (silinen + eklenen) kaldırır ve working tree’yi `origin/BASKENT-DOVIZ` ile aynı yapar.
- **MoneyTransferTurkey/** geri gelir; **scripts/.venv** ve **telegram-bot/.env** gibi sadece yerelde olan eklemeler working tree’den gider (ama zaten **_yedek_baskent_enerji_doviz_20260318** içinde kopyaları var).

Önce yedek klasörünü kontrol edip gerekli dosyaları (ör. `.env`, script’ler) oradan geri kopyalayabilirsiniz.

---

## 5. Özet tablo

| Veri türü | Konum | Adet / Açıklama |
|-----------|--------|-------------------|
| En güncel (referans) | GitHub `origin/BASKENT-DOVIZ` | 1210 dosya |
| Yerelde silinmiş (D) | GitHub’da mevcut | 1092 dosya (MoneyTransferTurkey/) |
| Yerelde eklenen (A) | Yedekte saklandı | 741 dosya (scripts/.venv, telegram-bot/.env vb.) |
| Yerel yedek | `_yedek_baskent_enerji_doviz_20260318` | O anki working tree ( .git hariç ) |

Bu rapor, yereldeki projeler ile GitHub depolarının karşılaştırılması ve en güncel verinin referans alınması amacıyla hazırlanmıştır. Eski ve işe yaramaz veriler yerelde yedeklenmiş olup hiçbir dosya silinmemiştir.
