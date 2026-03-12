# Proje Durum Analizi

**Tarih:** 2026-03-07  
**Proje:** QuantaQuokka / Başkent Enerji Döviz Muhasebe Sistemi

---

## 1. Repo ve branch

| Öğe | Değer |
|-----|--------|
| **Branch** | BASKENT-DOVIZ (origin ile senkron) |
| **Son commit** | 9744072 – Update development documentation and fix login field mapping |
| **Staged değişiklik** | 1 dosya silindi: `D_DERIN_TARAMA_VE_MUHASEBE_DISINDA_KALANLAR_RAPORU.md` |
| **Untracked** | (görünür yok; external/desktop_import vb. .gitignore’da olabilir) |

**Branches:** BASKENT-DOVIZ (aktif), main, dependabot/nuget/...

---

## 2. Derleme

| Çözüm | Durum | Not |
|--------|--------|-----|
| **AnasıTAS_Deniz.sln** | ✅ Başarılı (Release) | 0 hata, ~980 uyarı (çoğu nullable reference) |
| **SmileMedical.sln** | ⚠️ Test projesi bilinen hata | AGENTS.md: SmileMedical.API.Tests build hatası (Program internal) |

Ana uygulama (AnasıTAS_Deniz) derlenebilir durumda; deploy için `dotnet publish -c Release -o ./output` kullanılabilir.

---

## 3. Veritabanı ve lokal SQL

| Öğe | Değer |
|-----|--------|
| **DB adı** | mtt-moneyexchangeturkey |
| **Şema** | mtturkey_exchange |
| **Instance (lokal)** | localhost\SQLEXPRESS |
| **Bağlantı** | appsettings.json → ConnectionStrings:SQL (repo’da yok) |
| **Referans** | docs/REFERANS_VERITABANI_VE_YAPILANDIRMA.md |

**SQL dosyaları:** scripts/SeedUsers_SHA256_Update.sql, AnasıTAS_Deniz.Data/Migrations/CustomMigration_AddOfficeIdToExchangeRates.sql. Seed çalıştırma: scripts/Calistir-SeedUsers_SHA256.ps1.

---

## 4. Çalışma planı özeti (CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md)

- **Faz 1 (Güvenlik):** Çoğu madde ☐ – test dosyalarının kaldırılması, port/firewall, secret’ların env’e taşınması, [Authorize] kontrolü, JWT süresi, login rate limit. Tamamlanan: .gitignore, BCrypt+SHA256, seed SQL, rol/şube filtreleme.
- **Faz 2 (Döviz uyumu):** API route dokümantasyonu, panel testi, enhancement script’ler, exchange payload doğrulama ☐; CORS, bot path, User_Office ☑.
- **Faz 3 (Operasyonel):** Plesk dokümantasyonu, log, yedekleme, deploy geri alma ☐; deploy script canlı ayarları koruyor ☑.
- **Faz 4:** Refresh token, 2FA, API key, frontend env, Swagger kısıtı (orta/düşük öncelik).

**Kontrol listesi:** Test dosyaları kaldırıldı mı, DB portları kapalı mı, secret’lar env’de mi, [Authorize] tüm kritik endpoint’lerde mi, JWT süresi, rate limit, panel/tg testi, Plesk dokümantasyonu – büyük çoğunluğu henüz ☐.

---

## 5. Kod içi açık noktalar

| Konum | Konu |
|--------|------|
| ExchangeController | weeklyProfit = 0 (TODO: haftalık kâr hesaplanacak) |
| VaultSnapshotController (SmileMedical) | userId placeholder (JWT’den alınacak) |
| MediaController | TODO: System.Drawing.Common ile görsel boyutları |

---

## 6. Proje yapısı (kısa)

- **Backend:** AnasıTAS_Deniz.API, .Business, .Data, .Entity, .Tests.
- **Diğer:** SmileMedical/ (ikinci backend + Vue frontend), docs/, scripts/, telegram-bot/, external/ (desktop_import, local_import).
- **Canlı:** api.baskentenerji.com, baskentenerji.com/ihtiyar; DB mtt-moneyexchangeturkey.

---

## 7. Önerilen sıradaki adımlar

1. **Commit:** Staged silinen dosya için commit mesajı verip push (veya restore ile geri al).
2. **Güvenlik (Faz 1):** Sunucudan test/debug dosyalarını kaldırma, appsettings’in web’den okunmaması, gerekirse secret’ları env’e taşıma.
3. **API yetkilendirme:** Exchange/Vault/Office/Dealer/Party/Expense/AutoRate/VaultSnapshot controller’larda [Authorize] ve [AllowAnonymous] netleştirme.
4. **İsteğe bağlı:** weeklyProfit hesaplama, VaultSnapshot userId (JWT), MediaController görsel boyutu; uyarı sayısını azaltacak nullable düzeltmeleri.

---

*Bu dosya otomatik üretilmiş durum özetidir; güncel karar için CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md ve TEK_PROJE_STRATEJISI.md esas alınmalıdır.*
