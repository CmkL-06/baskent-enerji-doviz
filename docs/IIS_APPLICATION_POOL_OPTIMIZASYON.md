# IIS Uygulama Havuzu – plesk(varsayılan)(havuz) Optimizasyonu

**Tarih:** 25.02.2026

---

## Yapılan Ayarlar

| Ayar | Eski | Yeni |
|------|------|------|
| **queueLength** | 1000 | **2000** (daha fazla eşzamanlı istek) |
| **processModel.idleTimeout** | 00:05:00 | **00:20:00** (20 dk – daha az gereksiz kapanma) |
| **recycling.periodicRestart.time** | 1.05:00:00 | **1.00:00:00** (24 saatte bir düzenli yenileme) |
| **Havuz** | — | Yeniden başlatıldı (recycle) |

---

## Havuz Adı

- **IIS:** `plesk(default)(4.0)(pool)`
- **Plesk’te:** Varsayılan .NET 4.0 havuzu; birçok Plesk sitesi bu havuzu kullanır.

---

## Ek Öneriler (İsteğe Bağlı)

- **Plesk panel:** Araçlar → IIS Uygulama Havuzları → ilgili havuz → Gelişmiş → **Başlangıç modu:** “Her zaman çalışır” (ilk istek gecikmesini azaltır, bellek kullanımı artar).
- **Hata durumunda:** rapidFailProtection (5 çökme / 5 dk) açık; gerekirse Plesk üzerinden “Başarısızlık” ayarlarını kontrol edin.

Sistem bu ayarlarla optimize edildi.
