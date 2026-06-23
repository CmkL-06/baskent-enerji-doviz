# Frontend Patches

Bu klasör, baskentenerji.com Vue SPA'ya doğrudan uygulanan hotfix/enhancement scriptlerini içerir.

## Dosyalar

### ana-kasa-enhance.js (87.8 KB)
Vault (kasa) sayfalarına kapsamlı geliştirmeler ekler:
- **window.fetch interceptor**: Vault ID tutarsızlığı bug'ını düzeltir (404 → retry with vault list)
- **Sermaye takibi**: Kasa bazında sermaye girişi/çıkışı kaydı (API ile)
- **VaultDetail enhancements**: P&L, Z-rapor, transfer özeti
- **ExchangeSidebar**: Ruble döviz butonu
- **Navigation watch**: Vue Router geçişlerinde otomatik yeniden uygulama

**KRİTİK**: Bu dosya kaldırılırsa vault detay sayfaları bozulur.

### i18n-fix.js (9.9 KB)
Türkçe çeviri tutarsızlıklarını düzeltir.

## Deploy Talimatı
index.html içinde body kapanmadan önce şu satırlar mevcut olmalıdır:
```html
<script src="/assets/i18n-fix.js"></script>
<script src="/assets/ana-kasa-enhance.js"></script>
```
Yeni build sonrası bu dosyaları assets/ klasörüne kopyalayın ve index.html'e ekleyin.
