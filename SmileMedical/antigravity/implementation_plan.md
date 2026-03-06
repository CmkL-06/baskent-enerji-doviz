# SmileMedical Projesi - Birleştirme ve Düzenleme Planı

## Durum Analizi

Yapılan incelemeler sonucunda 3 farklı konum tespit edildi:

1.  **`Desktop\QuantaQuokka\SmileMedical`**: [ANA KAYNAK]
    - Projenin gerçek kaynak kodları (`.sln`, `.csproj` dosyaları) buradadır.
    - Tüm geliştirme burada yapılmalıdır.

2.  **`Desktop\app\SmileMedical.API`**: [BUILD ÇIKTISI]
    - Burası sadece derlenmiş dosyalardan (`.dll`, `.exe`) oluşmaktadır.
    - Kaynak kod içermez, geliştirme yapılamaz.
    - Güvenle arşivlenebilir veya silinebilir.

3.  **`Desktop\QuantaQuokka_`**: [BOŞ KLASÖR]
    - İçeriği boştur.
    - Silinebilir.

## Uygulama Adımları

Bu plan, **hiçbir kaynak kodunu kaybetmeden** projeyi tek bir klasörde (`Desktop\QuantaQuokka`) toplamayı hedefler.

### 1. Hazırlık ve Yedekleme
- [ ] `Desktop\QuantaQuokka` klasörünün `Desktop\QuantaQuokka_YEDEK` adıyla bir kopyasını oluştur. (Güvenlik önlemi)

### 2. Temizlik (Cleanup)
- [ ] `Desktop\app` klasörünü `Desktop\OLD_APP_BUILD` olarak yeniden adlandır (Silmek yerine önce taşıyoruz).
- [ ] `Desktop\QuantaQuokka_` (boş klasör) sil.

### 3. Antigravity Dosyaları
- [ ] `Desktop\QuantaQuokka\SmileMedical` içinde `antigravity` klasörü oluştur.
- [ ] Mevcut analiz raporlarını (`system_analysis.md`, `walkthrough.md`) bu klasöre taşı.

### 4. Proje Adı Düzenlemesi (Opsiyonel)
- [ ] Kullanıcı isterse `QuantaQuokka` klasörünün adını daha anlamlı olan `SmileMedical_Project` veya benzeri bir isme çevirebiliriz. (Şimdilik olduğu gibi bırakıyoruz).

## Doğrulama
1. `Desktop\QuantaQuokka\SmileMedical\SmileMedical.sln` dosyasının varlığını teyit et.
2. `antigravity` klasöründeki raporların okunabilir olduğunu kontrol et.
