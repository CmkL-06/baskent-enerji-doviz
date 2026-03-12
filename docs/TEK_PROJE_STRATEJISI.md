# Tek Proje Stratejisi (Kanonik Kaynak)

Bu depoda benzer iki backend ağacı bulunduğu için operasyonel belirsizliği azaltmak adına tek kaynak kararı uygulanır.

## Karar

- **Kanonik proje:** `BaskentEnerji.*`
- **Legacy referans:** `MoneyTransferTurkey/`
- **Release kapsamı dışında veri:** Proje dışı arşiv/kişisel içerikler

## Uygulama Kuralları

1. Yeni backend geliştirmeleri yalnızca `BaskentEnerji.*` üzerinde yapılır.
2. `MoneyTransferTurkey/` sadece geçmiş karşılaştırma ve rollback referansı olarak kalır.
3. Public olmayan endpoint dışındaki controller'lar sınıf seviyesinde `[Authorize]` ile korunur.
4. Repo kökünde proje dışı dosya tutulmaz; harici veri Git'e eklenmez.
5. Paketleme adımı sadece kanonik proje ve gerekli operasyon dosyalarını kapsar.

## Operasyon Komutları

- Yapı ve güvenlik drift denetimi:
  - `python3 scripts/single_project_audit.py`
- Dağıtım için tek proje paketi:
  - `bash scripts/package_single_project.sh`
