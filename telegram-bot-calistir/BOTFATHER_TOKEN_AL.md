# Telegram BotFather'dan Token Alma — Bağlantı Rehberi

Bu projede **3 bot** var. Her biri için Telegram'da @BotFather üzerinden token almanız gerekir.

---

## 1. BotFather'a bağlan

- **Telegram'da:** [@BotFather](https://t.me/botfather) sohbetini açın (Telegram uygulaması veya web).
- Veya tarayıcıda: **https://t.me/botfather**

---

## 2. Yeni bot oluşturma (ilk kez)

1. BotFather'a şunu yazın: **`/newbot`**
2. Bot için **isim** girin (örn: `Başkent Enerji Ana Bot`).
3. **Kullanıcı adı** girin; `bot` ile bitmeli (örn: `BaskentEnerjiAnaBot`).
4. BotFather size **token** verecek; örnek: `7123456789:AAHxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
5. Bu token'ı kopyalayın — **`.env`** dosyasında ilgili yere yapıştıracaksınız.

---

## 3. Bu proje için 3 bot

| .env değişkeni      | Kullanım              | BotFather'da yapılacak |
|---------------------|------------------------|-------------------------|
| **MAIN_BOT_TOKEN**  | Ana müşteri botu       | `/newbot` → Ana bot     |
| **OPERATOR_BOT_TOKEN** | Operatör botu       | `/newbot` → Operatör botu |
| **RUBLE_BOT_TOKEN** | Ruble kanal botu       | `/newbot` → Ruble botu  |

Her biri için **ayrı** bot oluşturun; her botun **farklı** bir token'ı olacak.

---

## 4. Zaten bot varsa tokenı görme

1. BotFather'a **`/mybots`** yazın.
2. Listeden ilgili botu seçin.
3. **API Token** (veya "Token'ı göster") seçeneğine tıklayın.
4. Çıkan token'ı kopyalayıp `.env` içinde ilgili satıra yapıştırın.

---

## 5. Token'ı .env'ye yazma

**Seçenek A — Elle:**  
`telegram-bot` klasöründeki **`.env`** dosyasını açın ve şu satırları doldurun:

```env
MAIN_BOT_TOKEN=buraya_ana_bot_tokeni
OPERATOR_BOT_TOKEN=buraya_operatör_bot_tokeni
RUBLE_BOT_TOKEN=buraya_ruble_bot_tokeni
```

**Seçenek B — Script ile:**  
Aynı klasörde terminalde:

```powershell
python token_ekle.py
```

Script sırayla her token için sizden isteyecek; yapıştırıp Enter'a basın. Değerler `.env`'ye yazılır, ekranda gösterilmez.

---

## 6. Bağlantıyı test etme

Token'ları kaydettikten sonra:

```powershell
cd telegram-bot
python run_all.py
```

Hata almazsanız botlar Telegram'a bağlanmış demektir. Çıkmak için **Ctrl+C**.

---

**Özet:** Telegram'da @BotFather → `/newbot` veya `/mybots` → token'ı al → `.env`'ye yaz veya `python token_ekle.py` ile kaydet.
