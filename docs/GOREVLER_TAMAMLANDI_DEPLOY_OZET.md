# Görevler Tamamlandı – Deploy ve Özet

**Tarih:** 2026-02-24  
**Yapılanlar:** Kalan kontroller, API deploy, GitHub push, final özet.

---

## 1. Tamamlanan görevler

| # | Görev | Durum |
|---|--------|--------|
| 1 | **Kontroller ve testler** | Tamamlandı |
| 2 | **Yükleme doğrulama ve deploy** | Tamamlandı |
| 3 | **GitHub repo final (push)** | Tamamlandı |
| 4 | **Final doküman ve özet** | Tamamlandı |

---

## 2. Kontrol sonuçları

- **IIS – baskentenerji.com:** vdir = `C:\Inetpub\vhosts\baskentenerji.com\httpdocs`, site Started.
- **IIS – api.baskentenerji.com:** vdir = `...\api.baskentenerji.com`, site Started.
- **Web kök:** `http://127.0.0.1/` (Host: baskentenerji.com) → **200**.
- **/login:** `login/index.html` ve rewrite kuralı ile **302 → /ihtiyar/**.

---

## 3. Deploy

- **Betik:** `BASKENT_PROJE\deploy-api-to-canli.ps1`
- **Kaynak:** `BASKENT_PROJE\publish-api` → hedef `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com`
- **Yapılan:** App pool durduruldu, canlı `appsettings.json` ve `web.config` yedeklendi, publish çıktısı kopyalandı, canlı ayarlar geri yüklendi, eski DLL artefaktları silindi, app pool başlatıldı.
- **Sonuç:** "Deploy tamamlandi. App Pool yeniden baslatildi."

---

## 4. GitHub

- **Repo:** `https://github.com/CmkL-06/baskent-enerji-doviz.git`
- **Dal:** `master`
- **Son commit:** `c03640a` – "Deploy sonrasi: Program.cs ve dealer-panel-enhance guncellemeleri"
- **Push:** 384191b..c03640a master → origin
- **.gitignore:** appsettings*.json, firebaseConfig*.json, output/, .vs/, bin/obj vb. mevcut.

---

## 5. Önemli yollar

| Bileşen | Yol |
|--------|-----|
| Web (httpdocs) | `C:\Inetpub\vhosts\baskentenerji.com\httpdocs` |
| API canlı | `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com` |
| Deploy kaynağı | `C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-api` |
| Kaynak kod (Git) | `C:\Users\Administrator\Desktop\BASKENT_PROJE\kaynak-kod` |
| Deploy betiği | `C:\Users\Administrator\Desktop\BASKENT_PROJE\deploy-api-to-canli.ps1` |

---

## 6. Sonraki deploy için

1. API değişikliği varsa: `kaynak-kod` içinde `dotnet publish -c Release -o ..\publish-api` (veya mevcut publish komutunuz), ardından `.\deploy-api-to-canli.ps1`.
2. Frontend (ihtiyar) değişikliği varsa: build çıktısını `httpdocs\ihtiyar\` ve gerekirse `httpdocs\login\` altına kopyalayın.
3. Tg/Flask paneli: `Desktop\bot` altında `autostart.bat` veya `bstart.bat` ile Flask’ı başlatın; tg.moneytransferturkey.com çalışır.

---

**Özet:** Kontroller yapıldı, API canlıya deploy edildi, GitHub’a push atıldı, bu özet dokümanı yazıldı.
