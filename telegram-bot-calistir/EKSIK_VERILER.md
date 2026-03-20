# Eksik Veriler — Bulma ve Güncelleme Rehberi

**Tarih:** 18.03.2026

---

## 1. Eksik alanlar (.env)

| Degisken | Durum | Nereden bulunur / nasil guncellenir |
|----------|--------|--------------------------------------|
| **RUBLE_CHANNEL_ID** | Bos | Asagida "Ruble kanal ID bulma" adimlari |
| **BASKENT_USERNAME** | Bos | Bashent Enerji API / panel: giris e-postasi |
| **BASKENT_PASSWORD** | Bos | Bashent Enerji API: sifre |
| **BASKENT_API_TOKEN** | Bos | API panelden token veya login ile otomatik alinir |
| **BASKENT_DEFAULT_VAULT_ID** | Bos | API: Vault listesi (UUID) |
| **BASKENT_USDT_CURRENCY_ID** | Bos | API: Para birimi listesi – USDT (UUID) |
| **BASKENT_RUBLE_CURRENCY_ID** | Bos | API: Para birimi listesi – Ruble (UUID) |

---

## 2. RUBLE_CHANNEL_ID bulma ve .env guncelleme

### Yontem A — Script (Ruble bot kanalda ise)

1. Ruble botu (@MTTOperatorBot) Ruble kanalina **admin** olarak ekleyin.
2. Kanala bir mesaj atin (veya bir mesaji kanala iletin).
3. Asagidaki scripti calistirin:

```powershell
cd telegram-bot
python ruble_kanal_id_bul.py
```

4. Ciktida gorunen `Chat ID` (ornegin `-1001234567890`) degerini kopyalayin.
5. `.env` dosyasinda su satiri guncelleyin:

```env
RUBLE_CHANNEL_ID=-1001234567890
```

### Yontem B — @userinfobot

1. Ruble kanalinda bir mesaji **@userinfobot** sohbetine iletin (forward).
2. Bot size o mesajin chat id’sini gosterir (kanal icin `-100...` seklinde).
3. Bu degeri `.env` icinde `RUBLE_CHANNEL_ID=` yanina yazin.

### Guncelleme sonrasi

- Ruble islemlerinde “yeni musteri” bildirimi bu kanala gider.
- `panel_kontrol.py` tekrar calistirildiginda RUBLE_CHANNEL_ID “Tanimli” gorunur.

---

## 3. Bashent Enerji API verileri

Bu degerler **proje veya yedekte yok**; sadece Bashent Enerji tarafindaki panel/API uzerinden alinir.

- **BASKENT_API_URL:** Zaten dolu: `https://api.baskentenerji.com/api/v1`
- **BASKENT_USERNAME / BASKENT_PASSWORD:** Panel giris bilgileriniz (mail + sifre).  
  API token yoksa `baskent_api.login()` mail/sifre ile token alir.
- **BASKENT_API_TOKEN:** Panelden dogrudan token veriliyorsa buraya yapistirilabilir; yoksa login yeterli.
- **BASKENT_DEFAULT_VAULT_ID, BASKENT_USDT_CURRENCY_ID, BASKENT_RUBLE_CURRENCY_ID:**  
  API’deki vault ve para birimi listelerinden UUID olarak alinmali (genelde panel veya API dokumantasyonunda yer alir).

Bos birakilirsa bot varsayilan kurlarla (DEFAULT_USDT_RATE, DEFAULT_RUB_RATE) calisir; sadece Bashent’e islem gonderimi yapilmaz.

---

## 4. Script ile eksikleri kontrol etme

```powershell
cd telegram-bot
python panel_kontrol.py
```

Bu script hangi alanlarin dolu/bos oldugunu gosterir. Eksikleri doldurdukca tekrar calistirarak dogrulayabilirsiniz.

---

## 5. Ozet

- **RUBLE_CHANNEL_ID:** `ruble_kanal_id_bul.py` veya @userinfobot ile bulunup `.env` guncellenir.
- **BASKENT_*:** Bashent Enerji panel/API’den alinir; projede mevcut degil.
- Guncellemeler sonrasi `panel_kontrol.py` ve gerekirse botlari yeniden baslatarak dogrulayin.
