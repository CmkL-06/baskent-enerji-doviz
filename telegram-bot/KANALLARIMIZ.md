# Bizim Kanallarımız — Hangi Kanal Ne?

| Kanal / Grup | İsim | Amaç | Hangi bot burada? | .env değişkeni |
|--------------|------|------|-------------------|-----------------|
| **Ana sohbet kanalımız** | **@MoneyTransferTurkeyOfficial** | Müşterilerin katılması gereken ana grup; bot “önce gruba katıl” kontrolü burada yapar. | **Ana bot** (@MoneyExchangeRubleBot) — üye + admin olmalı | **REQUIRED_GROUP_ID** = `-1003021754065` |
| **Operatör kanalımız** | **@MoneyTransferTurkey** (veya MoneyTransferTurkey grubu) | Operatör ekibinin kullandığı grup. | **Operatör botu** (@MoneyExchangeTurkeyBot) — isteğe bağlı eklenebilir; asıl kullanım özel sohbet | (ayrı bir ID yok; bot özelden çalışır) |
| **Ruble kanalı** | (sizin açtığınız Ruble kanalı) | Yeni Ruble işlemleri burada; operatörler banka bilgisi / onay burada verir. | **Ruble botu** (@MTTOperatorBot) — admin olmalı | **RUBLE_CHANNEL_ID** = (kanalın chat id’si) |

---

## Özet

- **Ana sohbet kanalımız** = **MoneyTransferTurkeyOfficial** → Ana bot burada, REQUIRED_GROUP_ID.
- **Operatör kanalımız** = **MoneyTransferTurkey** → Operatör grubu; operatör botu isteğe bağlı burada, asıl panel özel sohbetten.
- **Ruble kanalı** = Ayrı bir kanal; Ruble botu orada admin, ID’si RUBLE_CHANNEL_ID ile .env’de.
