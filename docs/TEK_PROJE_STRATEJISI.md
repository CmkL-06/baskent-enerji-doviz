# Tek Proje Stratejisi (Kanonik Kaynak)

Bu depoda kod tabanı iki benzer backend ağacı içerdiği için operasyonel belirsizlik oluşmaması adına tek kaynak kararı alınmıştır.

## Karar

- **Kanonik proje:** `BaskentEnerji.*`
- **Legacy referans:** `MoneyTransferTurkey/`
- **Release kapsamı dışında:** `external/` ve legacy ağaç

## Uygulama Kuralları

1. Yeni backend geliştirmeleri yalnızca `BaskentEnerji.*` üzerinde yapılır.
2. `MoneyTransferTurkey/` yalnızca geçmiş karşılaştırma ve rollback referansı için tutulur.
3. Paketleme adımında legacy ve `external/` hariç tutulur.
4. Güvenlik için mutating endpoint içeren controller'lar sınıf seviyesinde `[Authorize]` ile korunur; public endpoint'ler açıkça `[AllowAnonymous]` alır.

## Operasyon Komutları

- Tek proje audit:
  - `python3 scripts/single_project_audit.py`
- Temiz tek proje paketi:
  - `bash scripts/package_single_project.sh`

## Not

Bu strateji, bakım maliyetini ve yanlış hedefe deploy riskini düşürmek için uygulanır.
