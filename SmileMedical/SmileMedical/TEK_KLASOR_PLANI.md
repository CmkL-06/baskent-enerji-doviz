# İki Projeyi Tek Klasör Haline Getirme Planı

## Hedef yapı

```
Desktop\QuantaQuokka\
├── SmileMedical\          ← Mevcut QuantaQuokka_ içeriği (.NET API)
│   ├── SmileMedical.sln
│   ├── SmileMedical.API\
│   ├── SmileMedical.Business\
│   ├── ...
│   └── (tüm mevcut dosyalar)
└── Quantum_Terminal\      ← Quantum Terminal (rapor + ileride kod)
    ├── Quantum_Terminal_Teknik_Rapor.txt
    └── (diğer Quantum_Terminal dosyaları)
```

## Adımlar (dry-run → onay → uygulama)

1. **Yeni üst klasör:** `Desktop\QuantaQuokka\` oluşturulur.
2. **Mevcut proje taşınır:** `Desktop\QuantaQuokka_` → `Desktop\QuantaQuokka\SmileMedical\` (içerik taşınır, kopya değil).
3. **Quantum_Terminal alt klasörü:** `Desktop\QuantaQuokka\Quantum_Terminal\` oluşturulur.
4. **Rapor dosyaları taşınır:**  
   `Quantum_Terminal_Teknik_Rapor.txt` (ve varsa diğer Quantum_Terminal dosyaları)  
   → `Desktop\QuantaQuokka\Quantum_Terminal\`

## Tespit edilen Quantum Terminal ile ilgili dosyalar

- `C:\Users\CmkL-Owner\Desktop\Quantum_Terminal_Teknik_Rapor.txt`
- `C:\Users\CmkL-Owner\Desktop\Raporlar_Merkez\Quantum_Terminal_Teknik_Rapor.txt`
- `C:\Users\CmkL-Owner\Desktop\PC_Arsiv_Duzenli\01_Raporlar_Analiz\Quantum_Terminal_Teknik_Rapor.txt`
- `C:\Users\CmkL-Owner\Desktop\PC_Arsiv_Duzenli\01_Raporlar_Analiz\Quantum_Terminal_Teknik_Rapor_8bfa06.txt`

Not: Quantum Terminal React/TypeScript proje klasörü bulunamadı; sadece rapor dosyaları taşınacak. Kod klasörü sonradan eklenebilir.

## Onay

Bu plana göre taşıma yapılmadan önce **MergeIntoOneFolder.ps1 -WhatIf** çalıştırılacak, liste gösterilecek ve sizin onayınız alınacak.
