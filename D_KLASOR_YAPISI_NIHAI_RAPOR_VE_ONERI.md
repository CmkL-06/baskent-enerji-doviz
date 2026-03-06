# D: Sürücüsü – Nihai Klasör Yapısı Raporu ve Öneri

**Tarih:** 2026-02-20  
**Referans alınan belgeler:** D_DERIN_TARAMA_VE_MUHASEBE_DISINDA_KALANLAR_RAPORU.md, D_YAPI_ASLA_BOZULMASIN.md, D_SURUCU_HARITASI.md (PC_Arsiv_Duzenli\09_Yedekler_Backup), D_SURUCU_SISTEM_KLASOR_ANALIZ.txt, D_SISTEM_TAŞIMA_DÜZELTME.md.

Bu rapor: D:\ sürücüsünün **derinlemesine okunması**, **mevcut durum**, **GitHub entegrasyonu** ve **Muhasebe Pro / SISTEM** bağlamında **klasör yapısının nasıl olması gerektiği** ile **tek bir öneri** sunar. Sadece rapor ve öneri; işlem adımı yok.

---

## 1. MEVCUT D:\ DURUMU (Derin Okuma Özeti)

### 1.1 Yapılan taşımalar sonrası D:\

- **SAHSI** ve **CLOUD** artık D:\ üzerinde değil; masaüstüne taşındı: **C:\Users\CmkL-Owner\Desktop\SAHSI** ve **Desktop\SAHSI\CLOUD**.
- **SISTEM** D:\ köküne geri alındı: **D:\SISTEM** (kullanıcı tarafından düzeltildi).
- **MUHASEBE** D:\ kökünde: **D:\MUHASEBE** (Muhasebe Pro veri/doküman kökü).

### 1.2 Güncel D:\ üst düzey (beklenen)

```
D:\
├── $RECYCLE.BIN
├── GECICI
├── MUHASEBE              ← Muhasebe Pro veri/doküman
├── SISTEM                ← Muhasebe Pro / MASTER-SYSTEM kodu (Git repo)
├── SISTEM_DISINA_CIKARILAN
├── System Volume Information
├── TICARI                ← Ticari evrak, şirket klasörleri
├── YEDEK
├── _SON_ONAY
├── BASLAT.bat
└── KONTROL_PANELI.bat
```

*CLOUD ve SAHSI D:\ üzerinde yok; masaüstünde.*

### 1.3 D:\SISTEM – ince ayrıntı (referans: D_SURUCU_HARITASI, D_SURUCU_SISTEM_KLASOR_ANALIZ)

| Öğe | Açıklama |
|-----|----------|
| **.git** | Git deposu; repo kökü **D:\SISTEM**. Clone/pull/push ve tüm görece yollar bu köke göre. |
| **.github/workflows** | codeql.yml, python-publish.yml, tests.yml – CI/CD; işler **D:\SISTEM** köküne göre tanımlı. |
| **API** | Flask backend, venv, ai/, routes/, VERITABANI yolu config’te (D:\SISTEM\VERITABANI). |
| **CORE** | MASTER-SYSTEM, MASTER.ps1, config.json – scriptler **D:\SISTEM** yolunu varsayıyor. |
| **VERITABANI** | MERKEZ_VERITABANI.db, MUHASEBE_DB.db, SQL scriptleri – sabit yol bekleniyor. |
| **SCRIPTLER, OTOMASYON** | BAT/PS1/Py; paths.ps1 ve config’ler **D:\SISTEM** veya altı referans alıyor. |
| **CONFIG** | .env, paths.ps1, MCP – içinde **D:\SISTEM** veya alt klasör yolları kullanılıyor. |

Sonuç: **D:\SISTEM** hem **Git/GitHub repo kökü** hem de **tüm script ve API’nin varsayılan çalışma kökü**. Yol değişirse GitHub Actions, config ve scriptler kırılır.

### 1.4 D:\MUHASEBE

- Muhasebe Pro’ya ait **veri ve doküman** alanı (Excel, yedek DB referansları, evrak listeleri).
- **Kod ve merkez veritabanı** D:\SISTEM’de; MUHASEBE **sadece veri/doküman** tutar.

### 1.5 D:\TICARI

- 04-EVRAKLAR (Excel, doküman, belgeler), şirket klasörleri (KUL_YATIRIM_GROUP, MONEY_TRANSFER vb.), MOKITA/MDF dosyaları.
- Muhasebe Pro ile ilgili olup **MUHASEBE dışında kalan** veriler bu raporda ve **D_DERIN_TARAMA_VE_MUHASEBE_DISINDA_KALANLAR_RAPORU.md** içinde listelenmiş durumda.

---

## 2. GITHUB ENTEGRASYONU – DİKKAT EDİLECEKLER

- **Repo kökü:** **D:\SISTEM**. Git remote (origin) bu klasöre bağlı; `.github/workflows` bu köke göre çalışır.
- **Yol sabit kalmalı:** SISTEM başka sürücüye veya D:\ altında farklı bir isme (örn. D:\MUHASEBE\SISTEM) taşınırsa:
  - Görece path’ler ve Actions doğru çalışmaz.
  - CONFIG, paths.ps1, .env ve scriptlerdeki **D:\SISTEM** referansları yanlış kalır.
- **.gitignore / hassas veri:** VERITABANI\.db, .env, log/rapor çıktıları gerekiyorsa .gitignore’da kalmalı; repo’ya sadece kod ve doküman girmeli.
- **Özet:** Klasör yapısı kararı, **D:\SISTEM’in D:\ kökünde ve isminin SISTEM olarak kalmasını** zorunlu kılar.

---

## 3. MUHASEBE PRO PROJESİ – DİKKAT EDİLECEKLER

- **SISTEM** = Uygulama: API, CORE, SCRIPTLER, OTOMASYON, VERITABANI (merkez DB), WEB. Hepsi **D:\SISTEM** altında.
- **MUHASEBE** = Veri/doküman: Excel’ler, muhasebe evrakı listesi, (isteğe bağlı) şirket DB yolları listesi, yedek referansları. **D:\MUHASEBE** altında.
- **Ayrım:** Kod ve merkez veritabanı SISTEM’de; iş verisi ve evrak referansları MUHASEBE’de. SISTEM’i MUHASEBE içine taşımak veya MUHASEBE’yi SISTEM ile birleştirmek **yapıyı bozar** ve raporlardaki tüm uyarılarla çelişir.

---

## 4. KLASÖR YAPISININ NASIL OLMASI GEREKTİĞİ (Hedef Yapı)

Eski raporlara (D_DERIN_TARAMA, D_YAPI_ASLA_BOZULMASIN, D_SURUCU_HARITASI) göre tek **hedef yapı** aşağıdaki gibi olmalıdır.

### 4.1 D:\ kökü

```
D:\
├── SISTEM                    ← Zorunlu: Git repo kökü, API/CORE/VERITABANI/SCRIPTLER (ASLA taşınmaz / adı değişmez)
├── MUHASEBE                  ← Muhasebe Pro veri/doküman kökü
├── TICARI                    ← Ticari evrak, şirket klasörleri (mevcut yapı korunur)
├── YEDEK
├── GECICI
├── SISTEM_DISINA_CIKARILAN
├── _SON_ONAY
├── BASLAT.bat
└── KONTROL_PANELI.bat
```

- **CLOUD / SAHSI:** Artık D:\’de değil; masaüstünde. Yeni yapıda D:\ kökünde **olmaması** kabul edilmiş durumda.
- **SISTEM** kesinlikle **D:\** kökünde; **MUHASEBE** veya başka bir klasörün **içinde** olmamalı.

### 4.2 D:\SISTEM iç yapısı (korunacak)

- **.git, .github**, .cursor, .vscode, .claude, .memory  
- **API, CORE, VERITABANI, SCRIPTLER, OTOMASYON**  
- **DOKUMAN, LOGLAR, RAPORLAR, WEB, CONFIG, BELLEK, BILGI_BANKASI**  
- docs, test, tests, _ARCHIVE, BACKUPS, CACHE, BULUT-SENKRON, FORENSIC_TOOLS  

*(Detaylı ağaç: D_SURUCU_HARITASI.md §3.)*

### 4.3 D:\MUHASEBE – önerilen alt yapı

| Alt klasör | Amaç |
|------------|------|
| **EXCEL_VERI** | TICARI\04-EVRAKLAR\... içinden taşınacak TICARI_MUHASEBE_*.xlsx, İN MUHASEBE ÇALIŞMA.xlsx vb. |
| **DOKUMANLAR** | Muhasebe dokümanları, Zirve/MOKITA kılavuzları; isteğe bağlı kısayol: Ofis_ Muhasebe.md |
| **YEDEK_VERITABANLARI** | (İsteğe bağlı) Arşiv/yedek DB listesi veya kopya (örn. eski CLOUD\TOPLU_VERI referansı) |
| **SIRKET_VERITABANLARI** | (İsteğe bağlı) MOKITA/MDF yolları listesi veya kopya – TICARI’deki şirket DB’leri için referans |

TICARI’deki evrak ve şirket klasörleri **yerinde kalabilir**; MUHASEBE tarafında sadece **liste / kısayol** tutulması yeterli.

---

## 5. REFERANS ALINAN ESKİ RAPORLAR – ÖZET

| Belge | Özet |
|-------|------|
| **D_DERIN_TARAMA_VE_MUHASEBE_DISINDA_KALANLAR_RAPORU** | Muhasebe Pro ile ilgili ama MUHASEBE dışında kalan konumlar (TICARI, eski CLOUD), Excel/DB önerileri, SISTEM’in D:\ kökünde olması. |
| **D_YAPI_ASLA_BOZULMASIN** | SISTEM ve D:\ üst yapısının korunması; SISTEM’in MUHASEBE içine taşınmaması. |
| **D_SURUCU_HARITASI** | D:\SISTEM ağaç yapısı, API/CORE/VERITABANI/SCRIPTLER, .github/workflows, config ve script listesi. |
| **D_SURUCU_SISTEM_KLASOR_ANALIZ** | D:\SISTEM boyut, dosya türü, değerlendirme ve öneriler. |
| **D_SISTEM_TAŞIMA_DÜZELTME** | SISTEM MUHASEBE içindeyse D:\ köküne nasıl geri taşınır (zaten uygulandı). |

---

## 6. TEK ÖNERİ

**Kabul edilmesi önerilen hedef:**

1. **D:\ klasör yapısını** yukarıdaki **§4** ile **sabitleyin**: **D:\SISTEM** (repo kökü, asla taşınmaz/adı değişmez), **D:\MUHASEBE** (veri/doküman), **D:\TICARI** (ticari evrak), YEDEK, GECICI, SISTEM_DISINA_CIKARILAN, _SON_ONAY. CLOUD ve SAHSI D:\’de olmayacak (masaüstünde kalmaya devam eder).
2. **GitHub ve Muhasebe Pro** için: Tüm geliştirme, CI/CD ve scriptler **D:\SISTEM** yolunu **varsayılan** kabul etsin; config ve dokümanlarda bu yol referans alınsın. MUHASEBE sadece veri/doküman; kod ve merkez DB SISTEM’de kalsın.
3. **İsteğe bağlı veri toplama:** TICARI\04-EVRAKLAR\EXCEL_DOSYALARI\... içindeki muhasebe Excel’lerini **D:\MUHASEBE\EXCEL_VERI** (veya tek bir evrak alt klasörü) altında toplayın; şirket veritabanı yollarını **D:\MUHASEBE** içinde bir liste dosyasında tutun. Bu adım zorunlu değil, ancak yedek ve referans açısından faydalıdır.
4. **Koruma kuralı:** Bundan sonra **D:\SISTEM** taşınmaz, yeniden adlandırılmaz; **D_YAPI_ASLA_BOZULMASIN.md** kuralları geçerli kalır.

Bu rapor sadece **analiz ve öneri** içerir; herhangi bir taşıma veya silme işlemi yapılmaz.
