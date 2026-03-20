# QR Kodu ile Neden Açılmadı? — Telegram / BotFather

## 1. QR kodun ne için olduğu

- **Tarayıcıda** `web.telegram.org` veya `t.me` açıldığında gördüğünüz QR kodu çoğu zaman **“Telegram Web’e giriş”** içindir.
- Bu QR’ı **telefonunuzdaki Telegram uygulamasıyla** taradığınızda: Telefondaki hesabınız, **tarayıcıdaki Telegram Web** oturumunu açar. Yani QR, **BotFather sohbetini açmaz**; sadece tarayıcıyı giriş yapmış hale getirir.
- Bazen sayfa **BotFather sohbeti** değil, sadece “Giriş yap” ekranı açıyor olabilir; bu yüzden QR taradıktan sonra hâlâ sohbet açılmamış gibi görünür.

**Özet:** QR ile “açılmadı” derseniz, büyük ihtimalle QR sadece giriş içindi; BotFather’a **elle** gitmeniz gerekir.

---

## 2. BotFather’ı kesin açma yolları

### A) Telefonda (en garantisi)

1. **Telegram** uygulamasını açın (telefon).
2. Arama / arama çubuğuna **`@BotFather`** veya **`BotFather`** yazın.
3. Mavi tikli **BotFather** hesabına tıklayın.
4. Sohbet açılır; buradan `/newbot` veya `/mybots` yazın.

QR’a hiç gerek yok; doğrudan Telegram içinden BotFather’a gidiyorsunuz.

### B) Tarayıcıda (QR’dan sonra)

1. Tarayıcıda **https://t.me/botfather** adresine gidin.
2. QR ile **zaten giriş yaptıysanız**, sayfa doğrudan BotFather sohbetini açmalı.
3. Açılmıyorsa:
   - Önce **https://web.telegram.org** → QR ile giriş yapın.
   - Sonra aynı sekmede veya yeni sekmede **https://t.me/botfather** adresine gidin.

### C) Bilgisayarda Telegram masaüstü varsa

1. **Telegram Desktop**’u açın.
2. Üstteki arama çubuğuna **`@BotFather`** yazın.
3. Sohbete girip `/newbot` veya `/mybots` yazın.

Yine QR gerekmez.

---

## 3. “QR ile açılmadı” için sık nedenler

| Neden | Ne yapmalı |
|------|------------|
| QR sadece “Web’e giriş” için | Giriş yaptıktan sonra **t.me/botfather** sayfasına tekrar gidin veya telefonda @BotFather açın. |
| Telefonda Telegram yok / giriş yok | Telefona Telegram kurun, numara ile giriş yapın; sonra @BotFather’ı uygulama içinde arayın. |
| Tarayıcı pop-up / yeni sekme engelliyor | Tarayıcı ayarlarından pop-up’a izin verin veya **t.me/botfather** linkini elle adres çubuğuna yazın. |
| Link farklı bir uygulama açıyor | `t.me` linkine sağ tıklayıp “Chrome / Edge / Firefox ile aç” deyin veya linki kopyalayıp tarayıcıya yapıştırın. |

---

## 4. Token almak için (QR’a gerek yok)

1. **Telefonda veya bilgisayarda** Telegram’ı açın.
2. **@BotFather** sohbetine gidin (yukarıdaki yollardan biriyle).
3. **`/newbot`** (yeni bot) veya **`/mybots`** (mevcut bot token’ı) yazın.
4. Gelen **token**’ı kopyalayıp `token_ekle.py` veya `.env` dosyasına yapıştırın.

QR kodu sadece “Web’e giriş” içindir; BotFather’ı açmak ve token almak için QR şart değildir.
