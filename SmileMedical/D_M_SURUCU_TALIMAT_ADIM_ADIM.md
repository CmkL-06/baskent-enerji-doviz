# D: → M: (50 GB) ve Yeni D: Oluşturma – Adım Adım Talimat (Prompt)

Bu belge, işlemi yapacak kişi veya otomasyon için **en ince ayrıntısına kadar** yazılmış talimattır. Sırayla uygulayın.

---

## GENEL HEDEF

- Mevcut **D:** sürücüsü **50 GB** kalacak şekilde küçültülecek ve harfi **M:** yapılacak (içinde sadece SISTEM + MUHASEBE kalacak).
- Küçültmeden kalan alandan **yeni D:** sürücüsü oluşturulacak (TICARI, YEDEK vb. buraya taşınacak).
- Tüm işlemler **Windows Disk Yönetimi** veya **diskpart** ile yapılacak.

---

## ÖN HAZIRLIK (Zorunlu)

### 1. Yedek
- D:\ üzerindeki kritik verileri (TICARI, YEDEK, MUHASEBE, SISTEM’deki VERITABANI vb.) harici diske veya C:\ üzerine yedekleyin.
- En azından TICARI ve YEDEK klasörlerinin kopyasını alın.

### 2. D:\ kullanan programları kapat
- Cursor, VS Code, Excel, OneDrive, Google Drive senkron, tüm D:\ açan uygulamaları kapatın.
- Görev Yöneticisi’nde “D:” kullanan işlem varsa sonlandırın (veya bilgisayarı yeniden başlatıp sadece gerekli programları açın).

### 3. D:\ boyutunu kontrol et
- Dosya Gezgini’nde D:\ sürücüsüne sağ tık → Özellikler → “Kullanılan alan” ve “Kapasite” değerlerini not alın.
- **Kullanılan alan 50 GB’dan büyükse** aşağıdaki “AŞAMA A”yı uygulayın (önce TICARI, YEDEK vb. geçici taşıma). **50 GB’dan küçükse** “AŞAMA A”yı atlayıp doğrudan “AŞAMA B”ye geçin.

---

## AŞAMA A: D:\ İçeriğini 50 GB Altına İndirme (Sadece kullanılan alan 50 GB’dan büyükse)

### A1. Geçici hedef belirle
- Örnek: `C:\GECICI_D_TAŞIMA` veya harici diskte `E:\GECICI_D_TAŞIMA` klasörü oluşturun.
- Bu klasörde en az (D:\ kullanılan alan − 50 GB) kadar boş yer olmalı.

### A2. Taşınacak klasörleri taşı (taşıma = kesip yapıştır, kopya değil)
Sırayla şunları **geçici hedefe** taşıyın (D:\ kökünden):
- `D:\TICARI` → `C:\GECICI_D_TAŞIMA\TICARI` (veya E:\GECICI_D_TAŞIMA\TICARI)
- `D:\YEDEK` → aynı şekilde
- `D:\GECICI` → aynı şekilde
- `D:\SISTEM_DISINA_CIKARILAN` → aynı şekilde
- `D:\_SON_ONAY` → aynı şekilde
- D:\ kökünde SISTEM ve MUHASEBE dışında kalan başka klasör varsa onu da taşıyın.

### A3. Kontrol
- D:\ üzerinde sadece **SISTEM**, **MUHASEBE** ve (varsa) **BASLAT.bat**, **KONTROL_PANELI.bat** kalsın.
- D:\ sürücüsüne sağ tık → Özellikler → Kullanılan alan **50 GB’dan az** olmalı. Değilse SISTEM veya MUHASEBE içinden gereksiz büyük dosyaları geçici olarak başka yere taşıyın.

---

## AŞAMA B: Disk Yönetimi ile Birim Küçültme ve Harf Değiştirme

### B1. Disk Yönetimi’ni aç
- **Win + R** tuşlarına basın.
- Açılan kutuya `diskmgmt.msc` yazın, Enter’a basın.
- “Bilgisayar yönetimi” veya “Disk Yönetimi” penceresi açılacak. **Yönetici olarak çalıştırma** istenirse Evet deyin.

### B2. D: birimini tanı
- Pencerenin **alt** kısmında (birim listesi) **D:** yazan satırı bulun.
- O satırda: birim adı, dosya sistemi (NTFS), kapasite ve kullanılan alan yazar.
- **Yanlış diske dokunmayın.** Sadece şu an **D:** harfini taşıyan birimi kullanacaksınız.

### B3. D: birimini küçült (Shrink)
- **D:** satırına (ortadaki “D:” yazan alana) **sağ tıklayın**.
- Menüden **“Birimi Küçült”** (Shrink Volume) seçin.
- Bir süre sonra “Birimi Küçült” penceresi açılır; “Sorgulanabilir alan” (query) gösterilir.
- **“Küçültülecek alanı gabul et”** kutusunda **MB cinsinden** ne kadar küçültüleceğini girer.  
  - **Hedef:** D: biriminin **son boyutu 50 GB** olsun.  
  - 50 GB = 51200 MB.  
  - Örnek: Şu an D: 455 GB ise, küçültülecek alan = (455 − 50) × 1024 ≈ 414720 MB girin (veya “Küçültülecek alan” kutusunda maksimum değeri kullanıp, “Toplam boyut after shrink” 51200 MB’a yakın olacak şekilde ayarlayın; arayüzde “Enter the amount of space to shrink in MB” varsa 414720 gibi bir değer deneyin).
- **Küçült** (Shrink) düğmesine tıklayın.
- İşlem bitene kadar bekleyin. D: birimi küçülür; yanında **“Ayrılmamış”** (Unallocated) alan oluşur.

### B4. D: biriminin harfini M: yap
- **Küçülmüş birim** (hâlâ D: yazan) satırına **sağ tıklayın**.
- **“Sürücü Harfi ve Yolları Değiştir”** (Change Drive Letter and Paths) seçin.
- Açılan pencerede **D:** seçili olacak; **“Değiştir”** (Change) düğmesine tıklayın.
- **“Aşağıdaki sürücü harfini ata”** seçin, açılır listeden **M:** seçin.
- **Tamam** → **Evet** (uyarıda “programlar çalışmayabilir” denirse kabul edin).
- Artık bu birim **M:** olarak görünür. Eski D: klasörleri (SISTEM, MUHASEBE) şimdi **M:\SISTEM**, **M:\MUHASEBE** yolunda.

### B5. Bilgisayarı yeniden başlat (önerilir)
- Disk Yönetimi’ni kapatın.
- Bilgisayarı **yeniden başlatın**. Böylece tüm programlar M: harfini tanır.

---

## AŞAMA C: Yeni D: Sürücüsünü Oluşturma

### C1. Disk Yönetimi’ni tekrar aç
- `diskmgmt.msc` çalıştırın (gerekirse Yönetici olarak).

### C2. Ayrılmamış alanı bul
- Alt listede **“Ayrılmamış”** (Unallocated) yazan, B3’te oluşan alanı bulun. Genelde M: biriminin hemen yanında olur.

### C3. Yeni birim oluştur
- **Ayrılmamış** alana **sağ tıklayın**.
- **“Yeni Basit Birim”** (New Simple Volume) seçin.
- Sihirbaz açılır:
  - **İleri**
  - **Basit birim boyutu:** Varsayılan (tüm ayrılmamış alan) bırakın veya istediğiniz boyutu MB girin → **İleri**
  - **Sürücü harfi ata:** **D:** seçin → **İleri**
  - **Bu birimi şu ayarlarla biçimlendir:** Dosya sistemi **NTFS**, birim etiketi örn. **YeniD** veya boş bırakın, “Hızlı biçimlendir” işaretli → **İleri**
  - **Son** → **Bitir**
- Yeni **D:** sürücüsü oluşur ve listelenir.

---

## AŞAMA D: Klasörleri Yeni D:\ Altına Taşıma

### AŞAMA A uyguladıysanız (geçici taşıma yaptıysanız)
- `C:\GECICI_D_TAŞIMA\TICARI` (veya kullandığınız geçici yol) içeriğini **taşıyarak** `D:\TICARI` yapın.
- Aynı şekilde: **YEDEK**, **GECICI**, **SISTEM_DISINA_CIKARILAN**, **_SON_ONAY** klasörlerini geçici konumdan **D:\** altına taşıyın.
- Geçici klasörü (`C:\GECICI_D_TAŞIMA` vb.) boşalttıktan sonra silebilirsiniz.

### AŞAMA A uygulamadıysanız (D: zaten 50 GB’dan küçüktü)
- Bu adımı atlayın; zaten M: üzerinde sadece SISTEM ve MUHASEBE kaldı. TICARI, YEDEK vb. daha önce taşınmadıysa, bunlar şu an **M:\** altında kalmış olabilir; o zaman **M:\TICARI**, **M:\YEDEK** vb. klasörlerini **kesip** **D:\** altına yapıştırın.

---

## AŞAMA E: GitHub ve Config Güncelleme

### E1. Repo kökü
- Muhasebe Pro / SISTEM repo kökü artık **M:\SISTEM**. Tüm yol referansları **D:\** → **M:\** olmalı.

### E2. Güncellenecek yerler (D:\ → M:\)
- **M:\SISTEM\CONFIG\paths.ps1** (veya benzeri path dosyaları): İçinde `D:\` geçen satırları `M:\` yapın.
- **M:\SISTEM\.env**, **M:\SISTEM\API\.env**: `D:\` → `M:\`
- **M:\SISTEM\config.json**, **M:\SISTEM\CORE\config.json**: `D:\` → `M:\`
- **M:\SISTEM\README.md**, **M:\SISTEM\AGENTS.md**, **M:\SISTEM\WORKFLOW.md** ve diğer .md dosyaları: `D:\` → `M:\`
- **.cursorrules** (varsa): `D:\` → `M:\`
- GitHub’da (README, doküman) “D:\SISTEM” yazan açıklamaları “M:\SISTEM” yapın.

### E3. Arama ile kontrol
- M:\SISTEM içinde metin araması yapın: **D:\\** veya **D:\SISTEM**. Kalan tüm referansları **M:\** ve **M:\SISTEM** olarak değiştirin.

---

## SON KONTROL LİSTESİ

- [ ] M: sürücüsü var ve yaklaşık 50 GB.
- [ ] M:\ altında sadece **SISTEM** ve **MUHASEBE** (ve varsa .bat dosyaları).
- [ ] D: sürücüsü var ve TICARI, YEDEK, GECICI, SISTEM_DISINA_CIKARILAN, _SON_ONAY burada.
- [ ] Config ve dokümanlarda D:\ → M:\ güncellendi.
- [ ] MASTER.ps1 veya başlatıcı scriptler M:\SISTEM yolunu kullanıyor.

---

Bu talimat, işlemi **en ince ayrıntısına kadar** tarif eder. Her adımı sırayla uygulayın; bir adım hata verirse durun ve hatayı not alın.
