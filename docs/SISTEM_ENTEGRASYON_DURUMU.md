# Sistem entegrasyon durumu

**Tarih:** 2026-02-24  
**Kontrol:** API, veritabanı, IIS, tg/Flask, botlar.

---

## Özet: Kısmen entegre

| Bileşen | Durum | Not |
|---------|--------|-----|
| **API (api.baskentenerji.com)** | Çalışıyor | Login (ihtiyar/owner1) → 200, JWT dönüyor |
| **Veritabanı** | Bağlı | mtt-moneyexchangeturkey, 9 kullanıcı |
| **IIS siteleri** | Açık | baskentenerji.com, api.baskentenerji.com, tg.moneytransferturkey.com Started |
| **İhtiyar paneli (baskentenerji.com/ihtiyar)** | Bağımlı | Site açıksa panel yüklenir; API’ye istek atar |
| **tg (Flask panel)** | Panel kapalı | Port 5000 dinlemiyor → tg.moneytransferturkey.com 502 verebilir |
| **Telegram botları** | .env’e bağlı | BASKENT_API_URL, DB, token’lar doğruysa API/DB ile entegre |

---

## Kontrol sonuçları

1. **API login:** `POST https://api.baskentenerji.com/api/v1/User/login` + `{"Mail":"ihtiyar","Password":"owner1"}` → **200**, apiToken ve userInfo döndü.
2. **DB:** SQL Server (localhost\SQLEXPRESS), mtt-moneyexchangeturkey, mtturkey_exchange login → sorgu başarılı.
3. **IIS:** baskentenerji.com (httpdocs\public), api.baskentenerji.com, tg.moneytransferturkey.com (public) hepsi Started.
4. **Flask:** Port 5000’de dinleyen süreç yok; tg için reverse proxy çalışsa bile panel cevap vermez.

---

## Tam entegrasyon için yapılacaklar

1. **tg paneli:** Flask’ı başlatın (örn. `Desktop\bot\autostart.bat` veya `bstart.bat` → web_operator_panel_v2). Böylece tg.moneytransferturkey.com çalışır.
2. **Botlar:** Aynı .env ile çalıştırıldığında API ve DB’ye bağlanır; `python health_check.py` ile ortamı doğrulayın.
3. **baskentenerji.com document root:** Kök yol şu an `httpdocs\public`. İhtiyar paneli `httpdocs\ihtiyar` altındaysa, kökün `httpdocs` olması gerekebilir (önceki kurulumda böyleydi). Ana sayfa veya /ihtiyar açılmıyorsa Plesk’te document root’u kontrol edin.

---

## Sonuç

- **API + DB + IIS:** Entegre ve çalışıyor.
- **Panel (ihtiyar):** Site ve document root doğruysa API ile entegre.
- **tg + botlar:** Flask çalıştırılıp .env güncel olduğunda tam entegre olur.
