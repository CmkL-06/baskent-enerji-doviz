# Eski Sunucu Verisi Referansı (Silinmez)

Bu belge, eski sunucu kaynaklarından alınan verilerin **okuma/referans amaçlı** kullanılacağını ve silinmeyeceğini tanımlar.

## 1) Politika

1. Eski sunucu verileri (`legacy`) silinmez; yalnızca analiz ve karşılaştırma için kullanılır.
2. Kanonik geliştirme `BaskentEnerji.*` üzerinde devam eder.
3. Legacy veriden alınan bilgiler, aktif kararlara dönüştürülerek kanonik dokümanlara işlenir.
4. Hassas bilgi (şifre/secret) düz metin olarak repo içinde tutulmaz.

## 2) Kullanılan legacy kaynak türleri

- Legacy backend/doküman klasörü: `MoneyTransferTurkey/`
- Arşiv analiz raporları:
  - `docs/hash_analysis_20260306.json`
  - `docs/hash_analysis_post_cleanup_20260306.json`
  - `docs/hash_cleanup_report_20260306.md`
  - `docs/repo_consolidation_check_20260306.txt`
- Legacy bakım scripti:
  - `scripts/Clean-SmileMedical-ExactHashDuplicates.ps1`

## 3) Legacy veriden alınan ve kullanılan bilgiler

- API adlandırma standardı (`/api/v1/User/login`, Exchange route standardı)
- Deploy dosya/klasör kontrol kriterleri (IIS/Plesk doğrulama maddeleri)
- Modül envanteri (ExchangeOffice, Site, Blog, Coin, Telegram/bot entegrasyon bağlantıları)
- Veritabanı ve seed akışı ile ilgili karşılaştırma notları

## 4) Uygulama yaklaşımı

- Legacy veri “read-only referans” olarak tutulur.
- Karar ve üretim değişiklikleri yalnızca kanonik ağaçta yapılır:
  - `BaskentEnerji.API`
  - `BaskentEnerji.Business`
  - `BaskentEnerji.Data`
  - `BaskentEnerji.Entity`
- Uyuşmazlık varsa kanonik karar belgeleri esas alınır:
  - `README.md`
  - `docs/TEK_PROJE_STRATEJISI.md`
  - `docs/FINAL_KANONIK_YAPI.md`
