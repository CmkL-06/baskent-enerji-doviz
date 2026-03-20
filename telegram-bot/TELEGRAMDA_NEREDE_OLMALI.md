# Telegram'da Botların Nerede Olması Gerekiyor

Bu dosya, her botun **hangi grup/kanalda** bulunması gerektiğini açıklar.

---

## 1. Ana bot (Main bot) — Müşteri botu

**Bot:** `@MoneyExchangeRubleBot` (MAIN_BOT_TOKEN)

**Nerede olmalı:**

| Yer | Zorunlu | Açıklama |
|-----|--------|----------|
| **Zorunlu grup** | Evet | **REQUIRED_GROUP_ID** = `-1003021754065` (şu an .env’de bu) |
| Ruble kanalı | Hayır | Ana bot kanala eklenmez; Ruble bildirimini **Ruble bot** kanala yazar. |

**Ne yapmalısınız:**

1. Ana botu **bu gruba üye ekleyin** (grup linki: örn. @MoneyTransferTurkeyOfficial veya grubun davet linki).
2. Botu grupta **yönetici (admin) yapın** — en azından “Üyeleri gör” / “Add new admins” gibi yetki gerekebilir; çünkü bot `getChatMember` ile kullanıcının grupta olup olmadığını kontrol eder. Bot grupta **admin değilse** bu kontrol bazen hata verir.
3. İsterseniz grupta mesaj da yazdırabilirsiniz (grup mesajları `handle_group_message` ile işlenir).

**Özet:** Ana bot **mutlaka** REQUIRED_GROUP_ID grubunda **üye + tercihen admin** olmalı. Müşteri gruba üye değilse bot “Önce gruba katılın” der.

---

## 2. Operatör botu (Operator bot)

**Bot:** `@MoneyExchangeTurkeyBot` (OPERATOR_BOT_TOKEN)

**Nerede olmalı:**

| Yer | Zorunlu | Açıklama |
|-----|--------|----------|
| Grup / kanal | Hayır | Sadece operatörlerle **özel sohbet (private)** kullanılır. |

**Ne yapmalısınız:**

- Operatör botu **hiçbir gruba veya kanala eklemeniz gerekmez**.
- Operatörler botu Telegram’da bulup **özelden** `/start` ile kullanır (Owner panel, Admin panel, müşteri alma vb.).

**Özet:** Operatör botu **sadece özel sohbette** kullanılır; Telegram’da ekstra grup/kanal gerekmez.

---

## 3. Ruble botu (Ruble kanal botu)

**Bot:** `@MTTOperatorBot` (RUBLE_BOT_TOKEN)

**Nerede olmalı:**

| Yer | Zorunlu | Açıklama |
|-----|--------|----------|
| **Ruble kanalı** | Evet | Yeni Ruble müşterileri bu kanala yazılır; operatörler kanalda yanıt verir. |

**Ne yapmalısınız:**

1. Ruble işlemleri için bir **Telegram kanalı** (veya süper grup) açın.
2. **Ruble botu bu kanala ekleyin** ve **yönetici (admin)** yapın — mesaj gönderme ve mesajları yönetme yetkisi olmalı.
3. `.env` dosyasında **RUBLE_CHANNEL_ID** değişkenine bu kanalın chat id’sini yazın (örn. `-1001234567890`).  
   Chat id’yi almak için: `python ruble_kanal_id_bul.py` (bot kanala ekliyken kanalda bir mesaj attıktan sonra) veya @userinfobot ile kanaldan bir mesaj iletin.

**Özet:** Ruble botu **Ruble kanalında admin** olmalı ve `.env`’de **RUBLE_CHANNEL_ID** dolu olmalı.

---

## 4. Kısa tablo

| Bot | Telegram’da nerede olmalı |
|-----|---------------------------|
| **Ana bot** | REQUIRED_GROUP_ID grubunda **üye + tercihen admin** (müşteri gruba üye kontrolü için). |
| **Operatör botu** | Sadece özel sohbet; **grup/kanal gerekmez**. |
| **Ruble botu** | **Ruble kanalında admin**; `.env`’de RUBLE_CHANNEL_ID bu kanalın id’si olmalı. |

---

## 5. Kontrol

- Ana bot gruba eklendikten sonra grupta bir kullanıcı için “üye mi?” kontrolü çalışır.
- Ruble kanalı ve RUBLE_CHANNEL_ID doğruysa yeni Ruble işlemleri kanala düşer.
- Operatör botu için sadece operatör hesaplarının özelden yazması yeterlidir.
