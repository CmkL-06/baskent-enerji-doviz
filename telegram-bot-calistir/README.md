# Telegram Bot - Başkent Enerji Döviz

Döviz kurları, kripto borsaları ve operatör yönetimi için Telegram bot sistemi.

## Dosyalar

| Dosya | Açıklama |
|-------|----------|
| main_bot.py | Ana bot başlatıcı |
| baskent_api.py | Başkent Enerji API entegrasyonu |
| config.py | Bot yapılandırma dosyası |
| crypto_exchanges.py | Kripto borsa entegrasyonu |
| database.py | Veritabanı yönetimi |
| exchange_rates.py | Döviz kuru servisi |
| operator_bot.py | Operatör botu |
| ruble_bot.py | Ruble botu |
| run_all.py | Tüm botları başlat |
| translations.py | Çoklu dil desteği |
| requirements.txt | Python bağımlılıkları |

## Kurulum

```bash
cd telegram-bot
pip install -r requirements.txt
python run_all.py
```

## Kaynak

Bu dosyalar `baskentenerji-doviz-api/telegram-bot` klasöründen birleştirilmiştir.
