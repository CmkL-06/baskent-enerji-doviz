# QuantaQuokka_ – Çalışma Prensipleri

Bu belge, bu projede **dosya taşıma / tarama / geri yükleme** ile ilgili script’lerin ve işlemlerin **nasıl yürütüleceğini** tanımlar. Hem insan hem de otomasyon (AI/agent) bu prensiplere uyar.

---

## 1. Otomatik çalıştırma yasağı

- **Test edilmemiş veya sonucu bilinmeyen kod çalıştırılmaz.**
- **Silme, taşıma, üzerine yazma** gibi geri alınamaz işlemler, yalnızca aşağıdaki adımlar tamamlandıktan sonra ve **kullanıcı onayı** ile yapılır.

---

## 2. İşlem sırası (zorunlu)

### Adım 1: Sanal / kuru çalışma (dry-run)

- Önce **sadece raporlama veya simülasyon** yapılır:
  - `-WhatIf` ile çalıştırma, veya
  - Sadece liste üretme, dosya yazmama / taşımama / silmeme.
- Amaç: Hangi dosyalara hangi işlemin uygulanacağını **görmek**, gerçekte hiçbir değişiklik yapmamak.

### Adım 2: Sonuçları gösterme ve onay alma

- Dry-run çıktısı (liste, sayı, hedef yollar) kullanıcıya **açıkça** sunulur.
- **“Bu listeye göre gerçek işlemi yap”** şeklinde **açık onay** alınmadan gerçek taşıma/silme/kopyalama **yapılmaz**.

### Adım 3: Yerel PC’de uygulama (onay sonrası)

- Onay alındıktan sonra, gerçek işlem **yerel PC’de** (kullanıcı ortamında) uygulanır.
- İsterseniz kullanıcı script’i kendisi çalıştırır; isterseniz talimat verilir, onaylı adım tekrarlanır.

---

## 3. Kopya / yedek istemiyoruz

- **Amaç:** Veriyi tek yerde toplamak; dağınık kopya ve yedek oluşmasın.
- **Tercih:** Dosya bir yere **taşınsın** (move), gereksiz **kopya (copy)** veya ek yedek oluşturulmasın.
- Geri yükleme gerekmedikçe “kopyala, sonra sil” yerine **doğrudan taşıma** kullanılır. Script’ler buna göre yazılır (taşıma = tek nüsha).

---

## 4. Script’lere uygulanacak kurallar

| Kural | Açıklama |
|--------|----------|
| **Kaynak silinmez** | MoveMatches (ve benzeri) hedef zaten doluysa **kaynak dosyayı silmez**; sadece atlar. |
| **Önce WhatIf** | Taşıma/silme script’leri `-WhatIf` destekler; ilk çalıştırma WhatIf ile yapılır. |
| **Rapor, sonra onay** | Tarama/tespit sonuçları raporlanır; gerçek işlem kullanıcı onayından sonra yapılır. |
| **Taşıma, kopya değil** | Konsolidasyon için **Move** kullanılır; gereksiz copy/backup oluşturulmaz. |

---

## 5. Özet

1. **Sanal gerçeklik testi:** Önce dry-run / WhatIf / liste – hiçbir dosya değişmez.  
2. **Sonucu bilmek:** Çıktı kullanıcıya gösterilir, ne yapılacağı netleşir.  
3. **Onay:** Açık onay alınmadan gerçek taşıma/silme/kopyalama yapılmaz.  
4. **Yerel uygulama:** Onay sonrası işlem yerel PC’de, onay alarak uygulanır.  
5. **Kopya yok:** Taşıma yapılır; gereksiz kopya ve yedek birikmez.

Bu prensipler, proje kökündeki `CALISMA_PRENSIPLERI.md` dosyasında kalıcıdır; script yazarken ve çalıştırma talimatı verirken buna uyulur.
