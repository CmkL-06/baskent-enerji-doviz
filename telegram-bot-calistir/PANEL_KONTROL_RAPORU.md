# Operator / Admin / USDT / Ruble Panel Kontrol Raporu

**Tarih:** 18.03.2026

---

## 1. Operator Panel (Operatör Paneli)

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| Giriş | ✅ | Operatör botunda `/start` → kayıt/onay sonrası ana menü |
| Ana menü | ✅ | Bekleyen müşteriler, İstatistiklerim, Müşteri al, İşlemi tamamla |
| Owner Panel butonu | ✅ | Sadece Owner (ADMIN_ID) görür; `callback_data="owner_panel"` |
| Admin Panel butonu | ✅ | Owner veya Admin yetkili operatör görür; `callback_data="admin_panel"` |
| Komutlar | ✅ | `/ownerpanel`, `/adminpanel`, `/approve`, `/reject`, `/operators`, `/viewchat`, `/end` kayıtlı |
| Callback işleyiciler | ✅ | `owner_panel`, `admin_panel`, `take_*`, `complete_*`, `adm_activate_*`, `adm_deactivate_*` bağlı |

**Sonuç:** Operator paneli kod tarafında eksiksiz; yetki kontrolü (`_is_owner`, `_can_manage_panel`) uygulanıyor.

---

## 2. Admin Panel

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| Erişim | ✅ | Owner veya `is_admin=True` operatör; `cmd_admin_panel` ve callback `admin_panel` |
| İçerik | ✅ | Aktif / onay bekleyen operatör sayısı, `/operators`, `/approve`, `/reject` bilgisi |
| Onay bekleyen listesi | ✅ | `pending[:15]` için Onayla / Reddet butonları; `adm_activate_*`, `adm_deactivate_*` |

**Sonuç:** Admin paneli tanımlı ve callback/komutlarla bağlı.

---

## 3. Owner Panel

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| Erişim | ✅ | Sadece `ADMIN_ID` (config’te 7462722250); `cmd_owner_panel` ve callback `owner_panel` |
| İçerik | ✅ | Son işlemler listesi, "Görüşmeyi Aç" (`owner_view_<tid>`), Owner menü |

**Sonuç:** Owner paneli tanımlı ve sadece Owner hesabına açılıyor.

---

## 4. USDT Paneli / Akışı (Ana bot)

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| Para birimi seçimi | ✅ | `currency_usdt` callback; kur, min tutar (MIN_USDT), örnek miktar |
| İşlem oluşturma | ✅ | `db.create_transaction(..., currency='USDT', ...)`; operatörlere bildirim |
| USDT gönderim bilgisi | ✅ | Dealer adresi, network, miktar; TXID bekleme |
| TXID doğrulama | ✅ | Confirmations, tutar kontrolü; tamamlama kodu |
| Web panel bildirimi | ✅ | `db.notify_web_panel(tid)` (WEB_PANEL_URL hazırsa) |

**Not:** Başkent API (BASKENT_USERNAME, BASKENT_PASSWORD, BASKENT_API_TOKEN) boşsa kurlar/entegrasyon sadece varsayılan (DEFAULT_USDT_RATE) ile çalışır.

**Sonuç:** USDT akışı kodda uçtan uca bağlı.

---

## 5. Ruble Paneli / Akışı (Ana bot + Ruble bot)

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| Para birimi seçimi | ✅ | `currency_ruble` callback; MIN_RUBLE, kur |
| İşlem oluşturma | ✅ | `db.create_transaction(..., currency='RUBLE', ...)`; operatörlere bildirim |
| Ruble kanala bildirim | ⚠️ | `Config.RUBLE_CHANNEL_ID` ile ruble_bot.send_message; **.env’de RUBLE_CHANNEL_ID boş** |
| Ruble bot (kanal) | ✅ | ruble_bot.py: banka bilgisi, dekont, onay/red; `notify_web_panel` çağrıları var |

**Eksik:** `.env` içinde **RUBLE_CHANNEL_ID** boş. Ruble işleminde “yeni müşteri” mesajı kanala gitmez; kanalı kullanmak için Ruble kanalının chat id’sini (örn. `-100xxxxxxxxxx`) ekleyip .env’yi güncellemek gerekir.

**Sonuç:** Ruble akışı kodda var; kanal bildirimi için **RUBLE_CHANNEL_ID** doldurulmalı.

---

## 6. Web Panel (Flask)

| Öğe | Durum | Açıklama |
|-----|--------|-----------|
| URL | ✅ | `WEB_PANEL_URL` (varsayılan: http://localhost:5000) |
| Bildirim | ✅ | `database.notify_web_panel(tid)` → POST `{WEB_PANEL_URL}/api/notify_message` |
| Proje içi Flask uygulaması | ❌ | Bu repoda ayrı bir Flask sunucusu / web panel kodu yok |

**Sonuç:** Botlar web panele HTTP ile bildirim gönderiyor; panelin kendisi bu projede yok (başka repo/serviste olabilir). Panel çalışmıyorsa bildirimler 404/bağlantı hatası verir; bot akışları yine çalışır.

---

## 7. Özet Tablo

| Panel / Akış | Kod | Config | Not |
|--------------|-----|--------|-----|
| Operator panel | ✅ | ✅ | Token, ADMIN_ID, DB gerekli |
| Admin panel | ✅ | ✅ | Operator tablosunda is_admin |
| Owner panel | ✅ | ✅ | ADMIN_ID = Owner |
| USDT akışı | ✅ | ⚠️ | Başkent API boşsa sadece varsayılan kur |
| Ruble akışı | ✅ | ⚠️ | **RUBLE_CHANNEL_ID** doldurulmalı |
| Web panel bildirimi | ✅ | ⚠️ | WEB_PANEL_URL dışarıda bir sunucu gerektirir |

---

## 8. Yapılacaklar (isteğe bağlı)

1. **RUBLE_CHANNEL_ID:** Ruble işlemlerinin kanala düşmesi için Telegram Ruble kanalının chat id’sini .env’ye ekleyin.
2. **Web panel:** `http://localhost:5000` veya farklı bir URL’de panel çalıştırılıyorsa, `WEB_PANEL_URL` buna göre ayarlanmalı.
3. **Başkent API:** Canlı kur ve entegrasyon için BASKENT_USERNAME, BASKENT_PASSWORD veya BASKENT_API_TOKEN doldurulabilir.

Bu rapor, operator panel, admin panel, owner panel ile USDT ve Ruble akışlarının kontrolüne göre hazırlanmıştır.
