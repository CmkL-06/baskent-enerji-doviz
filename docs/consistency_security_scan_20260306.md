# Tutarlılık ve Güvenlik Taraması (2026-03-06)

## Kapsam
- Branch: `cursor/flaky-test-resolution-146b` (güncel, remote ile senkron).
- Son sistem verisi `origin/BASKENT-DOVIZ` merge edilmiştir.
- Tarama odağı: `SmileMedical`, `external/local_import`, secret riski, binary artefaktlar.

## Özet Durum
- `SmileMedical`: **1386 dosya**, ~**74.85 MB**.
- `external/local_import`: **6 dosya**, ~**0.01 MB** (çoğunlukla kural/doküman).
- Git izlenen binary-benzeri dosya: **143** (`.dll/.exe/.pdb/.db/.vsix`).
- `SmileMedical/Build_Output`: **149 dosya**, ~**54.76 MB** (yayın artefakt ağırlıklı).
- `SmileMedical/SmileMedical` iç içe kopya: **605 dosya**, dış klasörle aynı içerik oranı ~**%93.88**.

## Kritik Bulgular (Onaysız değişiklik yapılmadı)
1. **Plain-text secret riski**
   - `AnasıTAS_Deniz.API/appsettings.json` içinde SQL bağlantı şifresi ve JWT secret bulunuyor.
   - `docs/REFERANS_VERITABANI_VE_YAPILANDIRMA.md` içinde tam bağlantı stringi + şifre geçiyor.
   - `scripts/Test-API-AfterDeploy.ps1` içinde login örnek credential var.

2. **Üretim artefaktlarının repoda tutulması**
   - `SmileMedical/Build_Output` altında çok sayıda derlenmiş binary bulunuyor.
   - Bu içerik hem repo şişkinliği hem de lisans/sızdırma yüzeyi artırıyor.

3. **Klasör tekrarları / tutarsızlık riski**
   - `SmileMedical/SmileMedical` iç içe yapı büyük ölçüde aynı dosyaları tekrar ediyor.
   - Gelecekte yanlış klasörde düzenleme yapılması yüksek risk.

4. **`local_import` beklenti farkı**
   - Dokümanda geniş import akışı tarifli; mevcut `external/local_import` ise yalnızca 6 dosya.
   - Bu “tam import yapıldı mı?” sorusunu açık bırakıyor.

## Karar Matriksi (Senden onay bekleyen adımlar)
### A) Secret temizliği
- A1: `appsettings.json` ve dokümanlardan gerçek sırları kaldırıp placeholder'a geçelim.
- A2: Sadece yeni commitlerde secret engeli (pre-commit/CI) koyalım, eski içerik dursun.
- A3: Hiç dokunmayalım (yalnızca izleme).

### B) Binary artefaktlar
- B1: `SmileMedical/Build_Output` ve benzer binaryleri repodan çıkartalım, `.gitignore` ile engelleyelim.
- B2: Sadece whitelist gereken binaryleri bırakalım (listeyi sen belirle).
- B3: Tamamı kalsın.

### C) İç içe `SmileMedical/SmileMedical`
- C1: İç içe kopyayı kaldır, üst seviyeyi tek kaynak kabul et.
- C2: Üst seviyeyi kaldır, içteki kopyayı tek kaynak kabul et.
- C3: Şimdilik ikisi de kalsın (sadece raporla).

### D) “Tek dosya ile senkron” (Madde 5 için)
- D1: Tek kaynak dosya = `scripts/Import-BaskentDovizLocalImport.ps1` (makinece yürütülen tek otorite).
- D2: Tek kaynak dosya = `baskent_enerji_doviz_local_import.md` (manuel süreç).
- D3: Tek kaynak dosya = yeni `sync_manifest.json` (öneririm: script + doküman bunu okusun).

## 6C Uygulaması (test projesi)
- Çözüme `AnasıTAS_Deniz.Tests` eklendi.
- `tools_string` için 4 test eklendi (slug/ip davranışları).
- Build + test geçti.

## Not
- Sen “sormadan tercih yapma” dediğin için bu raporda **hiçbir temizleme/silme yapılmadı**.
