# Baskentenerji / Baskent Enerji – Tek Proje: BASKENT_PROJE

**Tarih:** 2026-02-26  
**Amaç:** Tüm ilgili klasörlerin BASKENT_PROJE altında tek yapıda toplanması.

---

## Birleştirilen yapılar

| Eski / diğer ad | Konum (birleştirme öncesi) | Durum |
|-----------------|----------------------------|--------|
| **Eski proje klasörü** | `C:\Users\Administrator\...` | Kaynak kod tek standarda çekildi: `BaskentEnerji.*`. Eski klasör yalnızca tarihsel referanstır. |
| **Baskent Enerji / BASKENT_PROJE** | `C:\Users\Administrator\Desktop\BASKENT_PROJE` | **Tek proje klasörü.** Tüm belgeler, API, bot, script’ler burada. |

---

## Tek klasör yapısı (BASKENT_PROJE)

```
BASKENT_PROJE/
├── README.md
├── deploy-api-to-canli.ps1
├── docs/              # Tüm belgeler (bu dosya dahil)
├── kaynak-kod/        # .NET API (`BaskentEnerji.*`) – tek kaynak
├── scripts/
├── telegram-bot/
└── publish-api/       # Build çıktısı (git’e eklenmez)
```

**Eski proje klasörü** 2026-02-26’da yedekle hash %100 doğrulanıp silindi. Yedek klasör adı tarihsel kayıtlarda tutulur.

---

## Özet

- **Baskentenerji** ve **Baskent Enerji** ile ilgili tek proje klasörü: **BASKENT_PROJE**.
- Kaynak kod: **kaynak-kod/** (`BaskentEnerji.API`, `.Business`, `.Data`, `.Entity`).
- Deploy ve belgeler: bu klasör üzerinden yürütülür.
