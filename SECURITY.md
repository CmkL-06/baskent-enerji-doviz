# Güvenlik Politikası

## Desteklenen Sürümler

Bu proje tek bir canlı ortamda (`BASKENT-DOVIZ` branch) çalışmaktadır. Yalnızca en güncel sürüm desteklenir.

| Branch | Durum |
| --- | --- |
| `BASKENT-DOVIZ` | :white_check_mark: Aktif |
| `main` | :x: Kullanılmıyor |

## Güvenlik Önlemleri

- JWT tabanlı kimlik doğrulama (Bearer token)
- Rol bazlı yetkilendirme (Rank: User → Staff → Admin → Owner)
- Ofis bazlı veri izolasyonu (Staff sadece kendi ofisini görür)
- API yanıtlarında hata detayı gizleme (ex.Message kullanılmaz)
- CORS kısıtlaması
- appsettings.json ve .env dosyaları git'e dahil edilmez

## Güvenlik Açığı Bildirimi

Bir güvenlik açığı tespit ederseniz lütfen doğrudan e-posta ile bildirin:

**E-posta:** kulemlakyatirim.as@gmail.com

- Açığı herkese açık issue olarak **açmayın**.
- 48 saat içinde geri dönüş yapılacaktır.
- Kabul edilen açıklar en kısa sürede düzeltilecektir.
