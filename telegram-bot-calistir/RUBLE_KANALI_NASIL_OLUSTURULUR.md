# Ruble Kanalı Nasıl Oluşturulur?

Ruble işlemleri için Telegram’da bir kanal açıp Ruble botunu ekleme adımları.

---

## 1. Yeni kanal oluşturun

1. **Telegram**’ı açın (telefon veya bilgisayar).
2. Sol menüden **☰** → **Yeni Kanal** (veya **New Channel**).
3. **Kanal adı** girin, örn: `Money Transfer Ruble` veya `MTT Ruble İşlemleri`.
4. İsterseniz **açıklama** yazın (örn: "Ruble işlem bildirimleri").
5. **Kanal türü:**
   - **Herkese açık** → Kullanıcı adı verirsiniz (örn. @MTTRuble).
   - **Özel** → Sadece davet linki ile girilir.
6. **Oluştur** deyin.

---

## 2. Ruble botunu kanala admin ekleyin

1. Açtığınız kanala girin.
2. **Kanal adına** (üstte) tıklayın → **Kanal bilgisi** açılır.
3. **Yöneticiler** (veya **Administrators**) → **Yönetici ekle**.
4. **@MTTOperatorBot** yazıp seçin (Ruble botu).
5. Yetkileri verin:
   - **Mesaj gönderme** (Post messages) — Açık.
   - **Mesajları düzenleme** (Edit messages) — İsterseniz açık.
   - Gerekirse **Mesajları silme** — İsterseniz açık.
6. **Kaydet** / **Done**.

---

## 3. Kanala bir mesaj atın

Kanala **ilk mesajı siz atın**, örn: `Kanal açıldı. Ruble işlemleri burada.`  
(Böylece bot kanalı “görür” ve script ile ID alabilirsiniz.)

---

## 4. Kanal ID’sini alıp .env’ye yazın

### Yöntem A — Script (otomatik .env güncelleme)

1. Bilgisayarda şu klasöre gidin:
   `C:\Users\CmkL-Owner\Documents\Projeler\BASKENT_TELEGRAM_BOT`
2. Terminalde:
   ```text
   python kanal_id_al_ve_env_yaz.py
   ```
3. Script kanalı bulunca numarayı seçin; **RUBLE_CHANNEL_ID** .env’ye yazılır.

### Yöntem B — Sadece ID’yi görmek

1. Aynı klasörde:
   ```text
   python ruble_kanal_id_bul.py
   ```
2. Çıktıdaki **Chat ID** (örn. `-1001234567890`) kopyalayın.
3. **.env** dosyasını açın, şu satırı bulun:
   ```text
   RUBLE_CHANNEL_ID=
   ```
4. Eşittir işaretinden sonra ID’yi yapıştırın:
   ```text
   RUBLE_CHANNEL_ID=-1001234567890
   ```
5. Dosyayı kaydedin.

### Yöntem C — @userinfobot ile

1. Telegram’da **@userinfobot** sohbetini açın.
2. **Ruble kanalından bir mesajı** bu bota **iletin** (forward).
3. Bot **Chat** kısmında **id: -100xxxxxxxxxx** gösterir; bu sizin kanal ID’niz.
4. Bu sayıyı .env’de **RUBLE_CHANNEL_ID=** yanına yazın.

---

## 5. Kontrol

1. Botları yeniden başlatın: `START_BOTLAR.bat` veya `python run_all.py`.
2. Ana bottan bir **Ruble** işlemi başlatın (test).
3. Ruble kanalında “Yeni müşteri” / işlem mesajı görünmeli.

---

## Kısa özet

| Adım | Ne yapılır? |
|------|------------------|
| 1 | Telegram → Yeni Kanal → İsim ver → Oluştur |
| 2 | Kanal bilgisi → Yöneticiler → @MTTOperatorBot ekle (admin) |
| 3 | Kanala bir mesaj at |
| 4 | `python kanal_id_al_ve_env_yaz.py` veya @userinfobot ile ID al → .env’de RUBLE_CHANNEL_ID yaz |
| 5 | Botları yeniden başlat, test et |

Bu adımlarla Ruble kanalını oluşturup sisteme bağlayabilirsiniz.
