# D:\ Derin Tarama Raporu – Muhasebe Pro Dışında Kalan Veriler ve Klasör Yapısı Analizi

**Tarih:** 2026-02-20  
**Amaç:** D:\ sürücüsünün derin taraması; Muhasebe Pro ile ilgili olup **D:\MUHASEBE** dışında kalan verilerin listesi; çalışma için **klasör yapısının nasıl olması gerektiği** analizi.

---

## 1. MEVCUT D:\ ÜST DÜZEY YAPI (Taranan)

```
D:\
├── $RECYCLE.BIN
├── CLOUD
├── GECICI
├── MUHASEBE              ← Muhasebe Pro ana klasörü (içinde SISTEM var)
├── SAHSI
├── SISTEM_DISINA_CIKARILAN
├── System Volume Information
├── TICARI
├── YEDEK
├── _SON_ONAY
├── BASLAT.bat
└── KONTROL_PANELI.bat
```

**Güncel durum:** SISTEM **D:\ köküne geri taşındı**; yol **D:\SISTEM**. (Düzeltme kullanıcı tarafından yapıldı.)

---

## 2. MUHASEBE PRO İLE İLGİLİ AMA D:\MUHASEBE DIŞINDA KALAN VERİLER

Aşağıdaki konumlar Muhasebe Pro / Zirve / MOKITA / veritabanı / muhasebe evrakı ile ilgilidir ancak **D:\MUHASEBE** altında değildir.

### 2.1 D:\CLOUD

| Konum | Açıklama | Öneri |
|-------|----------|--------|
| **D:\CLOUD\TOPLU_VERI\DIGER\** | **296+ adet** .MDF / .LDF dosyası (2020, 2021, 2021T, 2022, 2022T, 2023, 2023T vb. – SQL Server / Muhasebe dönem veritabanları) | Muhasebe Pro ile ilişkili yedek/arşiv veritabanları. İstenirse **D:\MUHASEBE\YEDEK_VERITABANLARI\CLOUD_TOPLU_VERI** benzeri tek yerde toplanabilir veya referans listesi MUHASEBE dokümanında tutulabilir. |

*(OneDrive/Google Drive altında da Zirve/Muhasebe/MERKEZ/MOKITA içerikli klasörler olabilir; tam liste için D:\CLOUD üzerinde ayrı bir “muhasebe” anahtar kelime taraması yapılabilir.)*

### 2.2 D:\TICARI

| Konum | İçerik tipi | Öneri |
|-------|-------------|--------|
| **D:\TICARI\04-EVRAKLAR\BELGELER_ARSIV\...** | muhasebe yol parası.pdf, Muhasebe tayyar faaliyet belgesi.pdf, ZirvenetAyar.txt, Zirve_Entegratör_Klavuz.pdf | Evrak arşivi; muhasebe dokümanı. MUHASEBE’ye taşımak zorunlu değil; **D:\TICARI\04-EVRAKLAR** “ticari evrak” alanı olarak kalabilir, ancak **D:\MUHASEBE\DOKUMANLAR\REFERANS** gibi bir kısayol veya liste ile Muhasebe Pro tarafından bilinmesi sağlanabilir. |
| **D:\TICARI\04-EVRAKLAR\DOKUMANLAR\...** | MUHASEBE_FINANS (genel-muhasebe-ders-notlari.pdf, KUL İNŞAAT MUHASEBE_Error.txt, Vergi Levhası Tasdik-Muhasebeci.rtf), PDF/RTF/TXT kopyaları | Aynı şekilde ticari evrak alanında; referans listesi MUHASEBE’de tutulabilir. |
| **D:\TICARI\04-EVRAKLAR\EXCEL_DOSYALARI\...** | TICARI_MUHASEBE_*.xlsx (hesap hareketleri, HESAPLAR, YATMAZ HESAP, 01.01.2025-31.12.2025), İN MUHASEBE ÇALIŞMA.xlsx | **Muhasebe Pro ile doğrudan ilgili Excel verileri.** İdeal olarak **D:\MUHASEBE\EXCEL_VERI** veya **D:\MUHASEBE\04_EVRAKLAR_KOPYASI** gibi tek bir alt klasörde toplanması işlem ve yedek açısından faydalıdır. |
| **D:\TICARI\KUL_YATIRIM_GROUP\02-MUHASEBE\MOKITA-DB\** | GENEL.MDF | MOKITA veritabanı – şirket muhasebe verisi. **D:\MUHASEBE\SIRKET_VERITABANLARI\KUL_YATIRIM_MOKITA** veya mevcut yapıda “tek kaynak” olarak bırakılıp MUHASEBE tarafında yol dokümante edilebilir. |
| **D:\TICARI\KUL_YATIRIM_GROUP\KUL_INSAAT\...\KUL_İNŞAAT\** | 2021.MDF, 2021T.MDF, 2022, 2023, 2024 .MDF/.LDF | KUL İnşaat muhasebe dönem veritabanları. Şirket verisi; ya mevcut yapıda kalır ya da **D:\MUHASEBE\SIRKET_VERITABANLARI** altında toplanır. |
| **D:\TICARI\KUL_YATIRIM_GROUP\PROJELER\...** | KUL EMLAK / TURKEY SİGORTA vb. altında muhasebe/evrak | Proje bazlı; TICARI yapısında kalıp MUHASEBE’de sadece yol/liste tutulabilir. |
| **D:\TICARI\KUL_YATIRIM_GROUP\** | Ofis_ Muhasebe.md | Muhasebe dokümanı; **D:\MUHASEBE\DOKUMAN** veya DOKUMANLAR’a kopyalanabilir / kısayol verilebilir. |
| **D:\TICARI\MONEY_TRANSFER\MOKITA\** | 2023.MDF, 2023T.MDF, 2024.MDF, 2024T.MDF, GENEL.MDF | MOKITA veritabanları – Muhasebe Pro ile ilgili. **D:\MUHASEBE\SIRKET_VERITABANLARI\MONEY_TRANSFER_MOKITA** veya mevcut konumda bırakılıp yol dokümante edilebilir. |

### 2.3 Özet – Muhasebe Pro İlgili Ama MUHASEBE Dışında Olanlar

| Kategori | Konum | Aksiyon önerisi |
|----------|--------|-------------------|
| Veritabanları (.MDF/.LDF) | D:\CLOUD\TOPLU_VERI\DIGER (296+ dosya) | Yedek/arşiv; liste MUHASEBE’de; isteğe bağlı tek klasörde toplama. |
| Veritabanları | D:\TICARI\KUL_YATIRIM_GROUP\02-MUHASEBE\MOKITA-DB, KUL_INSAAT\...\KUL_İNŞAAT\yyyy | Şirket kaynağı; ya olduğu yerde dokümante et ya da D:\MUHASEBE\SIRKET_VERITABANLARI altında topla. |
| Veritabanları | D:\TICARI\MONEY_TRANSFER\MOKITA | Aynı şekilde. |
| Excel (hesap hareketleri, HESAPLAR) | D:\TICARI\04-EVRAKLAR\EXCEL_DOSYALARI\...\TICARI_MUHASEBE_*.xlsx, İN MUHASEBE ÇALIŞMA.xlsx | **D:\MUHASEBE\EXCEL_VERI** veya **04_EVRAKLAR** altında toplanması önerilir. |
| Doküman (PDF, RTF, TXT) | D:\TICARI\04-EVRAKLAR\DOKUMANLAR\... (Zirve, muhasebe belgeleri) | TICARI’de kalabilir; MUHASEBE’de referans listesi. |
| Doküman | D:\TICARI\KUL_YATIRIM_GROUP\Ofis_ Muhasebe.md | D:\MUHASEBE\DOKUMAN’a kopyala veya kısayol. |

---

## 3. KLASÖR YAPISININ NASIL OLMASI GEREKTİĞİ ANALİZİ

### 3.1 Genel prensip

- **D:\** = Veri sürücüsü; **MUHASEBE** = Muhasebe Pro’nun tek ana kökü (yazılım + veri + doküman).
- **SISTEM** = Uygulama/kod (API, CORE, VERITABANI, SCRIPTLER). Yol **mutlaka D:\SISTEM** olmalı; **D:\MUHASEBE\SISTEM** olmamalı (script/API hepsi D:\SISTEM varsayıyor).
- Muhasebe Pro ile **doğrudan** ilgili veriler (Excel, yedek DB, şirket MOKITA/MDF) mümkün olduğunca **D:\MUHASEBE** altında toplanmalı; ticari/şirket evrakı **D:\TICARI**’de kalabilir ama MUHASEBE tarafında **liste / kısayol / referans** tutulmalı.

### 3.2 Önerilen çalışma yapısı

```
D:\
├── SISTEM                    ← Zorunlu: D:\ kökünde (MUHASEBE içinde değil)
│   ├── API
│   ├── CORE
│   ├── VERITABANI            ← MERKEZ_VERITABANI.db, MUHASEBE_DB.db
│   ├── SCRIPTLER
│   ├── OTOMASYON
│   ├── DOKUMAN
│   └── ...
├── MUHASEBE                  ← Muhasebe Pro veri + doküman kökü
│   ├── EXCEL_VERI            ← TICARI\04-EVRAKLAR’daki TICARI_MUHASEBE_*.xlsx vb. buraya
│   ├── DOKUMANLAR            ← Muhasebe dokümanları, Zirve/MOKITA kılavuzları
│   ├── YEDEK_VERITABANLARI   ← İstenirse CLOUD\TOPLU_VERI vb. arşiv kopyası
│   ├── SIRKET_VERITABANLARI  ← (İsteğe bağlı) MOKITA/MDF referans veya kopya
│   └── (mevcut diğer alt klasörler)
├── CLOUD                     ← Bulut senkron; muhasebe yedekleri “referans” listelenir
├── TICARI                    ← Ticari evrak; 04-EVRAKLAR kalır, MUHASEBE’de liste
├── YEDEK
├── GECICI
├── SAHSI
├── SISTEM_DISINA_CIKARILAN
├── _SON_ONAY
└── (diğerleri)
```

### 3.3 Yapılması gerekenler (kısa)

1. ~~**SISTEM’i D:\ köküne al**~~ **Yapıldı:** SISTEM D:\ kökünde (**D:\SISTEM**).
2. **Muhasebe Pro verilerini topla:**  
   - **D:\TICARI\04-EVRAKLAR\EXCEL_DOSYALARI\...** içindeki TICARI_MUHASEBE_*.xlsx ve İN MUHASEBE ÇALIŞMA.xlsx → **D:\MUHASEBE\EXCEL_VERI** (veya tek bir **04_EVRAKLAR_KOPYASI** klasörü).  
   - İstenirse **D:\TICARI\...\Ofis_ Muhasebe.md** → **D:\MUHASEBE\DOKUMAN**.
3. **Veritabanı konumları:**  
   - **D:\CLOUD\TOPLU_VERI\DIGER:** Arşiv olarak kalabilir; **D:\MUHASEBE\DOKUMAN** veya ayrı bir liste dosyasında “Muhasebe Pro ile ilgili D:\MUHASEBE dışı veritabanları” olarak listele.  
   - **D:\TICARI\...\MOKITA-DB, MOKITA, KUL_İNŞAAT\...\yyyy:** Ya mevcut yerde bırakıp **D:\MUHASEBE** içinde yol listesi tut ya da **D:\MUHASEBE\SIRKET_VERITABANLARI** altında topla.
4. **Yapıyı sabitle:** Bundan sonra SISTEM taşınmaz; muhasebe verisi mümkün olduğunca D:\MUHASEBE altında tutulur (referans: **D_YAPI_ASLA_BOZULMASIN.md**).

---

## 4. ÖZET TABLO – MUHASEBE DIŞINDA KALAN VERİLER

| # | Konum | İçerik | Önerilen aksiyon |
|---|--------|--------|-------------------|
| 1 | D:\CLOUD\TOPLU_VERI\DIGER | 296+ .MDF/.LDF | Liste MUHASEBE’de; isteğe bağlı D:\MUHASEBE\YEDEK_VERITABANLARI |
| 2 | D:\TICARI\04-EVRAKLAR\EXCEL_DOSYALARI\... | TICARI_MUHASEBE_*.xlsx, İN MUHASEBE ÇALIŞMA.xlsx | **D:\MUHASEBE\EXCEL_VERI** veya benzeri tek klasöre taşı |
| 3 | D:\TICARI\04-EVRAKLAR\DOKUMANLAR\... | Zirve/muhasebe PDF, RTF, TXT | TICARI’de kal; MUHASEBE’de referans listesi |
| 4 | D:\TICARI\KUL_YATIRIM_GROUP\02-MUHASEBE\MOKITA-DB\GENEL.MDF | MOKITA DB | Yol listesi veya D:\MUHASEBE\SIRKET_VERITABANLARI |
| 5 | D:\TICARI\KUL_YATIRIM_GROUP\KUL_INSAAT\...\KUL_İNŞAAT\yyyy\ | .MDF/.LDF | Aynı şekilde |
| 6 | D:\TICARI\MONEY_TRANSFER\MOKITA\ | .MDF | Aynı şekilde |
| 7 | D:\TICARI\KUL_YATIRIM_GROUP\Ofis_ Muhasebe.md | Doküman | D:\MUHASEBE\DOKUMAN’a kopyala/kısayol |

Bu rapor, D:\ derin taramasına dayanır; Muhasebe Pro ile ilgili olup **D:\MUHASEBE** dışında kalan verilerin listesi ve **çalışırken klasör yapısının nasıl olması gerektiği** analizini içerir. Güncellemek için taramayı periyodik tekrarlayabilirsiniz.
