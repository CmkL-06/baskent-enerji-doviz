# Master Durum Raporu — Başkent Enerji Döviz Projesi

**Tarih:** 21 Mayıs 2026
**Hazırlayan:** Claude (Anthropic) — Tüm sekmeler ve platformlar incelendi
**Kapsam:** GitHub, Replit, GoDaddy cPanel, GoDaddy Hosting, GoDaddy Airo Builder, cPanel Terminal

---

## 1. Platform Envanteri

### 1.1 GitHub — `CmkL-06/baskent-enerji-doviz`

**Ana Branch:** `BASKENT-DOVIZ` (Varsayılan, 480+ commit ahead of main)

| Branch | Durum | Notlar |
|---|---|---|
| `BASKENT-DOVIZ` | ✅ Varsayılan | Ana geliştirme branch'i |
| `main` | ⚠️ 480 commit geride | Sadece yerel import tarama dosyaları içeriyor |
| `claude/proje-yapısını-kur-UdAug` | Aktif | 1 commit öne |
| `imleç/bekleyen-iş-öğesi-4cb9` | Aktif | 6 commit öne |
| `dependabot/*` | Otomatik | NuGet güvenlik güncellemeleri |

**Dil Dağılımı:** C# %86.8, Python %7.7, JavaScript %2.3, HTML %1.4, Vue %1.0, PowerShell %0.5

**Açık Pull Request'ler:** 4 adet

---

### 1.2 Replit — `@kulemlakyatirim/baskent-enerji-doviz`

**URL:** https://replit.com/@kulemlakyatirim/baskent-enerji-doviz
**Durum:** ⚠️ Kredi bitti — "You're out of credits. Upgrade to Core"

**Son Agent Çalışması (7 saat önce):**
- Döviz kuru görüntüleme düzeltmeleri
- dealer-panel-enhance.js'den: Bayi yönetim paneli, QR kod oluşturma, admin sekmesi
- Exchange sayfasına otomatik tab sistemi (Operatör Paneli | Admin Panel | Bayi QR)
- loadExchangeRates() ve loadOffices() fonksiyonları exchange store'a eklendi
- HMR WebSocket Replit uyumlu hale getirildi
- "Exchange Office" → "Döviz Bürosu" başlık değişikliği (i18n)

**Canlı Preview:** https://*.replit.dev/ihtiyar/dashboard — "DÖVIZ İŞLEMLERİ" sayfası çalışıyor (USD, EUR, GBP, TRY)

**Çözüm:** Replit Core'a yükseltme yapılmalı VEYA geliştirme GitHub Codespaces/yerel ortama taşınmalı.

---

### 1.3 GoDaddy cPanel — `alanyayatirim.com`

**Sunucu:** sxb1plzcpnl490361.prod.sxb1.secureserver.net:2083
**Kullanıcı:** m04i2k3yhq9f

**Kaynak Kullanımı:**

| Kaynak | Kullanım | Limit | % |
|---|---|---|---|
| Disk | 4.05 GB | 50 GB | 8% |
| Veritabanı | 9 | 25 | 36% |
| Eklenti Domain | 6 | 10 | **60% — Dikkat!** |
| Alt Domain | 9 | 50 | 18% |
| FTP Hesabı | 3 | 25 | 12% |

**⚠️ Kritik Sorunlar:**
- SSL sertifikası yakında sona eriyor — otomatik yenileme başarısız
- Eklenti domain limiti %60 (6/10) — yeni domain eklerken dikkatli olunmalı

**Kurulu Uygulamalar (Installatron):**
- Kul Gayrimenkul (alanyayatirim.com)
- My blog

---

### 1.4 GoDaddy Hosting Yönetim Paneli

**URL:** host.godaddy.com

**Barındırılan Siteler:**

| Alan Adı | Durum | WordPress Versiyon |
|---|---|---|
| alanyayatirim.com | ✅ Birincil domain | 6.2.9 (Güncelleme gerekli!) |
| aleynaekincibeauty.com | ✅ Çalışıyor | 6.6.1 |
| baskentdanismanlik.com | ⚠️ Uygulama kurulmamış | — |
| baskentenerji.com | ⚠️ Uygulama kurulmamış | — |
| api.baskentenerji.com | ⚠️ Uygulama kurulmamış | — |

**⚠️ Önemli:** PHP Extended Support **İPTAL EDİLMİŞ**. PHP uyumluluk sorunlarına yol açabilir.

---

### 1.5 cPanel Terminal

**URL:** https://sxb1plzcpnl490361.prod.sxb1.secureserver.net:2083/.../terminal
**Durum:** ✅ Açık, bekleme modunda — komut çalıştırmaya hazır.

---

### 1.6 GoDaddy Airo AI Builder

**Site ID:** `z2sd91mef0`
**URL:** https://airo-builder.godaddy.com/develop/z2sd91mef0

**Proje Yapısı (Vite + Vue + Tailwind):**
```
src/
public/
index.html
package.json
vite.config.ts
tailwind.config.js
tsconfig.json
components.json
airo-media.json
```

**Durum:** AI Builder açık, "Sitemi yeniden oluştur" akışında — URL girilmesi bekleniyor.

---

## 2. Canlı Sistem Durumu (Plesk — Windows Sunucu)

| Servis | URL | Teknoloji | Durum |
|---|---|---|---|
| Yönetim Paneli | baskentenerji.com/ihtiyar | Vue 3 | ✅ Çalışıyor |
| REST API | api.baskentenerji.com/api/v1 | .NET 8 | ✅ Çalışıyor |
| API Health | api.baskentenerji.com/health | — | ✅ 200 OK |
| Operatör Paneli | tg.moneytransferturkey.com | Flask 5000 | ✅ Çalışıyor |
| Ana Site | moneytransferturkey.com | Nuxt.js | ✅ Çalışıyor |

**Veritabanı:**
- Sunucu: MSSQL Server SQLEXPRESS
- DB Adı: mtt-moneyexchangeturkey
- Şema: mtturkey_exchange

---

## 3. Açık Görevler (Öncelik Sırasına Göre)

### 🔴 ACİL (Kritik)

- [ ] **SSL Yenileme** — GoDaddy cPanel'de otomatik yenileme başarısız, manuel yenileme gerekli
- [ ] **WordPress Güncellemesi** — alanyayatirim.com WordPress 6.2.9 → 6.6.x güncellenmeli
- [ ] **PHP Extended Support** — İptal edilmiş, uyumluluk kontrol edilmeli

### 🟡 Yüksek Öncelik

- [ ] **baskentenerji.com deploy** — GoDaddy cPanel'e .NET API veya frontend kurulumu
- [ ] **api.baskentenerji.com deploy** — .NET 8 API GoDaddy'ye kurulumu (veya mevcut Plesk yapısı korunacak mı?)
- [ ] **JWT token süresi** — UserServiceCommand'da ≤24 saat olarak ayarlanmalı
- [ ] **Login rate limit** — Pencere/sınır gözden geçirilmeli
- [ ] **Replit kredi** — Upgrade to Core veya yerel/Codespaces'e geçiş
- [ ] **main branch** — BASKENT-DOVIZ ile birleştirilmeli veya açıkça deprecated işaretlenmeli

### 🟠 Orta Öncelik

- [ ] **BaskentEnerji.Tests** — NuGet CI cache (NU1301 hatası)
- [ ] **Panel deploy script** — `VITE_API_BASE_URL` env değişkeni, tek komutla build+deploy
- [ ] **Swagger** — Sadece dev/staging'de açık, canlıda kapalı veya IP kısıtlı
- [ ] **Migration güncelliği** — Dealer, Vault, Office, Party tablolarının EF migration uyumu
- [ ] **Eklenti domain limiti** — 6/10 — yeni eklemeden önce limit artırılmalı veya temizlenmeli

### 🟢 Düşük Öncelik

- [ ] **Refresh token / Beni Hatırla** özelliği
- [ ] **2FA (TOTP)** — Admin/Owner hesapları için isteğe bağlı
- [ ] **API key auth** — Bot/3. parti entegrasyon için ayrı auth yolu
- [ ] **Audit log** — Exchange/vault işlemleri için
- [ ] **GoDaddy Airo Builder** — Projeyi tamamlamak veya kullanmamak kararı verilmeli

---

## 4. Bağlantı Mimarisi Özeti

```
[Kullanıcı/Bayi]
      │
            ▼
            baskentenerji.com/ihtiyar  (Vue 3 Panel — Plesk)
                  │
                        ▼ API çağrıları
                        api.baskentenerji.com/api/v1  (.NET 8 — Plesk)
                              │
                                    ├── MSSQL: mtt-moneyexchangeturkey
                                          │
                                                └── tg.moneytransferturkey.com  (Flask + IIS Proxy)
                                                              │
                                                                            └── Telegram Botları (Python)
                                                                                                  └── BASKENT_API_URL → api.baskentenerji.com/api/v1
                                                                                                  ```

                                                                                                  **GoDaddy cPanel (alanyayatirim.com) ile ilişki:**
                                                                                                  - Şu an Plesk ile GoDaddy iki **ayrı** hosting ortamı
                                                                                                  - baskentenerji.com, api.baskentenerji.com için GoDaddy'de "Uygulama Kur" bekliyor
                                                                                                  - Karar: Bu domainler GoDaddy'ye mi taşınacak yoksa Plesk'te mi kalacak?

                                                                                                  ---

                                                                                                  ## 5. Önerilen Sonraki Adımlar

                                                                                                  1. **Acil:** GoDaddy cPanel SSL sertifikasını manuel yenile
                                                                                                  2. **Acil:** WordPress 6.2.9 → güncel versiyona yükselt
                                                                                                  3. **Bu hafta:** baskentenerji.com ve api.baskentenerji.com için hosting kararı ver (GoDaddy mi, Plesk mi?)
                                                                                                  4. **Bu hafta:** Replit için kredi satın al veya geliştirmeyi GitHub Codespaces'e taşı
                                                                                                  5. **Bu ay:** main branch'i BASKENT-DOVIZ ile birleştir veya deprecated işaretle
                                                                                                  6. **Bu ay:** JWT süresi ve rate limit ayarlarını canlıya uygula

                                                                                                  ---

                                                                                                  *Bu rapor 21 Mayıs 2026 tarihinde tüm açık sekmeler ve platformlar incelenerek hazırlanmıştır.*
                                                                                                  *Kaynak: GitHub, Replit, GoDaddy cPanel, GoDaddy Hosting, GoDaddy Airo Builder, cPanel Terminal*
