# D: → M: (50 GB) ve Yeni D: Sürücüsü Planı

**Tarih:** 2026-02-20  

---

## 1. HEDEF

| Önceki | Sonraki |
|--------|--------|
| Mevcut **D:** sürücüsü | **M:** olarak yeniden adlandırılacak; kapasitesi **50 GB** olacak (Muhasebe Pro için) |
| — | **Yeni D:** sürücüsü oluşturulacak (mevcut D: küçültüldükten sonra kalan alandan veya ayrı alan) |
| D:\ üzerinde her şey | **M:\** (50 GB) = sadece **Muhasebe Pro** (SISTEM + MUHASEBE) |
| — | **D:\** = TICARI, YEDEK, GECICI, SISTEM_DISINA_CIKARILAN, _SON_ONAY vb. (Muhasebe Pro dışı) |
| Repo/config’te **D:\** | Tüm ilgili yerlerde **M:\** olarak güncellenecek (GitHub, config, doküman) |

---

## 2. ADIMLAR (Özet)

1. **Yedek / hazırlık**  
   - Önemli verileri yedekleyin.  
   - Açık uygulamaları (özellikle D:\SISTEM kullananları) kapatın.

2. **D: 50 GB’dan büyükse – önce taşıma (önemli)**  
   - Mevcut D:\ içeriği 50 GB’tan fazlaysa Windows birimi doğrudan 50 GB’a küçültemez.  
   - **TICARI**, **YEDEK**, **GECICI**, **SISTEM_DISINA_CIKARILAN**, **_SON_ONAY** vb. büyük klasörleri **geçici olarak** başka bir yere (C:\, masaüstü veya harici disk) taşıyın.  
   - D:\ üzerinde sadece **SISTEM** ve **MUHASEBE** kalsın; toplam boyut 50 GB’ın altına insın.  
   - Sonra küçültme ve M: / yeni D: işlemlerini yapın; ardından taşıdığınız klasörleri **yeni D:\** altına geri taşıyın.

3. **Mevcut D: birimini 50 GB’a küçültme**  
   - **Disk Yönetimi** (diskmgmt.msc) veya **diskpart** ile mevcut **D:** birimini **küçült** (Shrink); birim boyutu **50 GB** olsun.  
   - Kalan alan “ayrılmamış” olarak görünecek.

4. **Küçültülmüş birimi M: yapma**  
   - Mevcut **D:** biriminin (artık 50 GB) sürücü harfini **M:** olarak değiştirin.  
   - Gerekirse bilgisayarı yeniden başlatın.

5. **Yeni D: birimini oluşturma**  
   - Küçültme sonrası kalan **ayrılmamış alan**dan yeni birim oluşturun.  
   - Sürücü harfini **D:** atayın.  
   - (İsterseniz başka bir diskte boş alan varsa oradan da D: oluşturabilirsiniz.)

6. **Klasör taşıma**  
   - **M:\** üzerinde yalnızca **SISTEM** ve **MUHASEBE** kalsın.  
   - Adım 2’de geçici yere taşıdıysanız: **TICARI**, **YEDEK**, **GECICI**, **SISTEM_DISINA_CIKARILAN**, **_SON_ONAY** vb. klasörleri **yeni D:\** altına geri taşıyın.  
   - Taşımadıysanız: Bu klasörleri doğrudan **D:\** altına taşıyın.

7. **GitHub / config / doküman güncelleme**  
   - Tüm **D:\** → **M:\** değişiklikleri:  
     - Repo içi dokümanlar (README, .md),  
     - Config dosyaları (paths.ps1, .env, config.json vb.),  
     - .cursorrules, AGENTS.md vb.  
   - GitHub’da yol ile ilgili açıklamalar varsa onları da **M:\** olacak şekilde güncelleyin.

---

## 3. YENİ SÜRÜCÜ YAPISI

**M:\ (50 GB – sadece Muhasebe Pro)**  
```
M:\
├── SISTEM          ← API, CORE, VERITABANI, SCRIPTLER (Git repo)
└── MUHASEBE        ← Muhasebe Pro veri/doküman
```

**D:\ (yeni sürücü – diğer veriler)**  
```
D:\
├── TICARI
├── YEDEK
├── GECICI
├── SISTEM_DISINA_CIKARILAN
├── _SON_ONAY
└── (gerekirse .bat vb.)
```

---

## 4. KAPASİTE

- **M: sürücüsü:** **50 GB** (Muhasebe Pro için; mevcut D: küçültülerek elde edilir).
- **D: sürücüsü:** Küçültme sonrası kalan alan kadar (yeni birim).

---

Bu belge yalnızca **plan ve rehber**dir; sürücü değişikliği ve birim oluşturma işlemleri Windows Disk Yönetimi veya diskpart ile sizin tarafınızdan yapılmalıdır.
