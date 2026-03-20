# Kanal ID Nasıl Oluşturulur / Alınır

Telegram'da **kanal ID'si sizin oluşturacağınız bir şifre değil**; kanalı oluşturduğunuzda Telegram otomatik verir. Adımlar:

---

## 1. Ruble kanalını oluşturun (henüz yoksa)

1. Telegram’ı açın (telefon veya masaüstü).
2. **Menü** → **Yeni Kanal** (veya New Channel).
3. Kanal adı girin (örn. "Money Transfer Ruble").
4. Kanalı **özel** veya **herkese açık** yapın; oluşturun.

---

## 2. Ruble botu kanala admin ekleyin

1. Kanalı açın → **Kanal bilgisi** (içerik alanında kanal adına tıklayın).
2. **Yöneticiler** → **Yönetici ekle**.
3. **@MTTOperatorBot** (Ruble botu) yazıp ekleyin.
4. Yetkiler: **Mesaj gönderme** ve **Mesajları düzenleme** (ve gerekirse diğerleri) verin; **Kaydet**.

---

## 3. Kanal ID’sini alın

### Yöntem A — Script (önerilen)

1. Kanala **bir mesaj yazın** (örn. "Test").
2. Bu klasörde terminal açıp:
   ```text
   python ruble_kanal_id_bul.py
   ```
3. Çıktıda **Chat ID** satırında `-100xxxxxxxxxx` gibi bir sayı görünecek. Bunu kopyalayın.
4. `.env` dosyasını açıp şu satırı güncelleyin:
   ```text
   RUBLE_CHANNEL_ID=-100xxxxxxxxxx
   ```
   (Sayıyı kendi aldığınız ID ile değiştirin.)

### Yöntem B — @userinfobot

1. Kanaldan **herhangi bir mesajı** Telegram’da **@userinfobot** sohbetine **iletin** (forward).
2. Bot size mesaj bilgisini gösterir; **Chat** kısmında **id: -100xxxxxxxxxx** yazar. Bu sizin kanal ID’niz.
3. Bu değeri `.env` içinde `RUBLE_CHANNEL_ID=` yanına yapıştırın.

---

## 4. .env’yi kaydedin

`.env` dosyasında:

```env
RUBLE_CHANNEL_ID=-1001234567890
```

Kaydedip botları yeniden başlatın. Bundan sonra yeni Ruble işlemleri bu kanala düşer.

---

**Özet:** Kanal ID’yi siz “üretmezsiniz”; önce kanalı oluşturup Ruble botu admin yapıyorsunuz, sonra script veya @userinfobot ile **mevcut kanalın ID’sini alıp** .env’ye yazıyorsunuz.
