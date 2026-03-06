# Baskentenerji / Baskent Enerji – Tek Proje: BASKENT_PROJE

**Tarih:** 2026-02-26  
**Amaç:** Tüm ilgili klasörlerin BASKENT_PROJE altında tek yapıda toplanması.

---

## Birleştirilen yapılar

| Eski / diğer ad | Konum (birleştirme öncesi) | Durum |
|-----------------|----------------------------|--------|
| **AnasıBerdus-Deniz** (baskentenerji / Deniz projesi) | `C:\Users\Administrator\AnasıBerdus-Deniz` | Kaynak kod **BASKENT_PROJE\kaynak-kod** ile aynı proje (isim: AnasıTAS_Deniz). Eski klasör yedeklendi; tek kaynak bu repo. |
| **Baskent Enerji / BASKENT_PROJE** | `C:\Users\Administrator\Desktop\BASKENT_PROJE` | **Tek proje klasörü.** Tüm belgeler, API, bot, script’ler burada. |

---

## Tek klasör yapısı (BASKENT_PROJE)

```
BASKENT_PROJE/
├── README.md
├── deploy-api-to-canli.ps1
├── docs/              # Tüm belgeler (bu dosya dahil)
├── kaynak-kod/        # .NET API (AnasıTAS_Deniz.*) – tek kaynak
├── scripts/
├── cursor-agent/
├── telegram-bot/
└── publish-api/       # Build çıktısı (git’e eklenmez)
```

**Eski AnasıBerdus-Deniz klasörü** (`C:\Users\Administrator\AnasıBerdus-Deniz`) 2026-02-26’da yedekle hash %100 doğrulanıp silindi. Yedek: `Desktop\Yedekler\AnasiBerdus-Deniz_birlesik_20260226` (1079 dosya, SHA256 uyumlu).

---

## Özet

- **Baskentenerji** ve **Baskent Enerji** ile ilgili tek proje klasörü: **BASKENT_PROJE**.
- Kaynak kod: **kaynak-kod/** (AnasıTAS_Deniz.API, .Business, .Data, .Entity).
- Deploy ve belgeler: bu klasör üzerinden yürütülür.
