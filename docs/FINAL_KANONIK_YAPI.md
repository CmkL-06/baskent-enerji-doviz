# Final Kanonik Yapı (Güçlü Sürüm)

Bu belge, projenin tek kaynaklı final yapısını ve bileşen entegrasyon sınırlarını tanımlar.

## 1) Tek kaynak kararı

- **Kanonik backend:** `BaskentEnerji.*`
- **Kanonik çözüm:** `BaskentEnerji.sln`
- **Legacy referans:** `MoneyTransferTurkey/` (karşılaştırma/geri dönüş amaçlı)
- **Eski sunucu verisi:** Silinmez; karşılaştırma ve bilgi çıkarımı için read-only tutulur

## 2) Bileşen entegrasyonu

- **API katmanı (`BaskentEnerji.API`)**
  - Auth/JWT, CORS, Swagger
  - Health/Diagnostics: `/health`, `/health/live`, `/health/ready`, `/api/v1/Diagnostics/*`
- **İş katmanı (`BaskentEnerji.Business`)**
  - ExchangeOffice (ofis/kasa/işlem/party/autorate)
  - Site (page/menu/theme/form/seo/media)
  - Blog, Coin, Email, Cache servisleri
- **Veri katmanı (`BaskentEnerji.Data`)**
  - `BaskentEnerjiDbContext`
  - `mtturkey_exchange` şema varsayılanı
- **Model katmanı (`BaskentEnerji.Entity`)**
  - Entity/DTO/Request/Response modelleri
- **Operasyon**
  - `scripts/single_project_audit.py` (yapısal/güvenlik drift)
  - `scripts/api_tooling_smoke.sh` (health + kritik route smoke)
  - `scripts/package_single_project.sh` (release paketleme)

## 3) Güçlü final kontrol kriterleri

1. Aktif referans dokümanlarda legacy isim kalıntısı bulunmaz.
2. API projesi `Release --no-restore` ile build alır.
3. Health endpoint `200` döner.
4. Yetki modeli: public endpoint’ler dışında `[Authorize]` korunumu sürer.
5. Secret değerler repo içinde düz metin tutulmaz.
6. Eski sunucu verisi sadece referanslanır; temizlikte otomatik silinmez.

## 4) Doğrulama komutları

```bash
python3 scripts/single_project_audit.py
dotnet build BaskentEnerji.API/BaskentEnerji.API.csproj -c Release --no-restore
curl http://localhost:5093/health
bash scripts/api_tooling_smoke.sh
```

## 5) Not

`MoneyTransferTurkey/` altında yer alan Vue ve dokümanlar legacy referanstır; yeni backend kararları yalnızca `BaskentEnerji.*` üzerinde uygulanır.
