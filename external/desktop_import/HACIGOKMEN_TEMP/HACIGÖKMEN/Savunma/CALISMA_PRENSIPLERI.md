# Çalışma Prensibi — Kapsamlı Rehber

Bu doküman, projede AI asistanı ile çalışırken uyulacak prensipleri ve beklentileri tanımlar.

---

## 1. İletişim ve Dil

| Prensip | Açıklama |
|--------|----------|
| **Dil** | Kullanıcı Türkçe yazıyorsa yanıtlar Türkçe verilir. Kod, komut ve teknik terimler (örn. `useState`, `API`) gerektiğinde İngilizce bırakılabilir. |
| **Netlik** | Açıklamalar kısa ve anlaşılır olmalı; gereksiz jargon kullanılmaz. |
| **Belirsizlik** | İstek belirsizse veya birden fazla yorum mümkünse, kısa sorularla netleştirilir; varsayımla ilerlenmez. |

---

## 2. Kod ve Değişiklik Prensipleri

### 2.1 Kapsam
- **Tek odak**: Bir kullanıcı isteği = tek mantıksal değişiklik (örn. sadece bir özellik veya bir hata düzeltmesi).
- **Mevcut kodu oku**: Değişiklik yapmadan önce ilgili dosya(lar) okunur; gereksiz veya kapsam dışı düzenleme yapılmaz.
- **Proje yapısı**: Yeni dosya/klasör eklerken mevcut proje düzeni (örn. `src/`, `components/`, `utils/`) korunur.

### 2.2 Kalite
- **DRY**: Tekrarlayan mantık fonksiyon/hook/modül olarak çıkarılır.
- **Okunabilirlik**: Anlamlı isimler, kısa fonksiyonlar, gerekirse yorum (özellikle karmaşık iş mantığı için).
- **Tutarlılık**: Projedeki mevcut stil (indent, tırnak, import sırası vb.) mümkün olduğunca korunur.

### 2.3 Güvenlik ve Performans
- **Güvenlik**: Kullanıcı girdisi render edilirken XSS’e karşı dikkat; SQL/komut birleştirmede injection riski alınmaz; hassas veri loglanmaz.
- **Performans**: Gereksiz re-render, ağır hesaplama veya büyük bundle’lar fark edilirse önerilir veya iyileştirilir.
- **Erişilebilirlik**: Arayüz değişikliklerinde temel a11y (semantik HTML, etiketler, klavye) gözetilir.

---

## 3. Değişiklik Sonrası

- **Lint / format**: Değiştirilen dosyalarda lint veya format hatası varsa düzeltilir.
- **Test**: Projede test varsa, ilgili değişiklikten sonra testlerin çalıştığı kontrol edilir; kullanıcı isterse test önerilir veya yazılır.
- **Özet**: Yapılan değişiklikler kısa bir özetle (ne değişti, neden) açıklanır.

---

## 4. Dosya ve Proje Yapısı

- Yeni bileşen/script/utility eklerken proje kökündeki veya `docs/` içindeki yapıya uyulur.
- Varsa `README.md`, `CONTRIBUTING.md` veya benzeri dokümanlardaki kurallar dikkate alınır.
- Gerekmedikçe yeni konfigürasyon dosyası (örn. ekstra `.env`) eklenmez; mevcut config genişletilir.

---

## 5. Hata ve Edge Case

- **Hata mesajları**: Kullanıcı bir hata paylaştığında, önce hata metni ve mümkünse ilgili kod bölümü incelenir; sonra olası neden ve çözüm önerilir.
- **Edge case**: Sınır durumlar (boş liste, null, çok uzun girdi vb.) kodda ele alınacaksa kısa açıklama ile eklenir veya kullanıcıya sorulur.

---

## 6. Ne Yapılmaz

- Kullanıcı açıkça istemedikçe büyük refaktör veya mimari değişiklik yapılmaz.
- Hassas bilgi (API anahtarı, şifre) koda veya commit mesajına yazılmaz; `.env` veya ortam değişkeni kullanımı önerilir.
- Lisansı belirsiz veya projeyle uyumsuz üçüncü parti kod doğrudan kopyalanmaz; kullanım koşulları hatırlatılır.

---

## 7. Güncelleme

Bu prensip dokümanı proje ihtiyaçlarına göre güncellenebilir. Değişiklik önerileri `CALISMA_PRENSIPLERI.md` üzerinde yapılır; Cursor kuralı (`.cursor/rules/calisma-prensibi.mdc`) ile çakışmamasına dikkat edilir.

---

*Son güncelleme: Proje çalışma prensibi ilk sürüm.*
