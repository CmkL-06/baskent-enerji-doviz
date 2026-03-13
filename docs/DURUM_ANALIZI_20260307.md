# Proje Durum Analizi (Kanonik)

**Tarih:** 2026-03-13  
**Proje:** QuantaQuokka / Başkent Enerji Döviz Muhasebe Sistemi

---

## 1. Kanonik karar özeti

| Öğe | Durum |
|-----|-------|
| **Kanonik backend** | `BaskentEnerji.API`, `.Business`, `.Data`, `.Entity`, `.Tests` |
| **Kanonik çözüm** | `BaskentEnerji.sln` |
| **Legacy referans** | `MoneyTransferTurkey/` (yeni geliştirme hedefi değil) |
| **Politika** | Yeni backend değişiklikleri yalnızca `BaskentEnerji.*` üzerinde |

---

## 2. Derleme ve çalışma durumu

| Kontrol | Sonuç | Not |
|---------|-------|-----|
| `dotnet build BaskentEnerji.API/BaskentEnerji.API.csproj -c Release --no-restore` | ✅ Başarılı | API çıktısı üretildi |
| `dotnet build BaskentEnerji.sln -c Release --no-restore` | ⚠️ Ortam kısıtı | `BaskentEnerji.Tests` için NuGet erişim hatası (NU1301) |
| `GET /health` | ✅ 200 | API ayakta ve health endpoint erişilebilir |

---

## 3. Veritabanı referansı

| Öğe | Değer |
|-----|-------|
| **DB adı** | `mtt-moneyexchangeturkey` |
| **Şema** | `mtturkey_exchange` |
| **Bağlantı** | `ConnectionStrings:SQL` (runtime ortamında) |
| **Referans doküman** | `docs/REFERANS_VERITABANI_VE_YAPILANDIRMA.md` |

---

## 4. Yapısal durum

- API route ve yetkilendirme standartları `BaskentEnerji.API` üzerinde aktif olarak uygulanıyor.
- Operasyonel denetim için health/diagnostics endpoint’leri ve smoke test scripti mevcut.
- Legacy isim kalıntıları çoğunlukla doküman geçmişi ve bazı dosya adlarında (işlevsel sınıf adlarında değil) bulunur.

---

## 5. Sıradaki teknik odak

1. Aktif dokümanlarda legacy isim temizliğini sürdürmek.
2. Nullable uyarılarını modül bazında azaltmak.
3. Test katmanını network bağımlılığından izole edecek CI/yerel cache stratejisi eklemek.

---

*Bu dosya, kanonik kararların kısa durum özetidir. Nihai yön için `README.md` ve `docs/TEK_PROJE_STRATEJISI.md` esas alınır.*
