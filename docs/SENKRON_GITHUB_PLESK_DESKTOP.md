# GitHub ↔ Plesk ↔ Desktop Senkron Rehberi

**Baskent Enerji – Döviz Muhasebe Programı**  
**Tek proje klasörü:** `BASKENT_PROJE` · Kaynak: Desktop | Yayın: GitHub | Canlı: Plesk (api.baskentenerji.com)

---

## Kurumsal yapı belgeleri

| Konu | Belge |
|------|--------|
| **GitHub** – Tek repo, `main` dalı, commit kuralları, klasör yapısı | [GITHUB_KURUMSAL_YAPI.md](GITHUB_KURUMSAL_YAPI.md) |
| **Plesk** – Domain, belge kökü, SSL, IIS, kontrol listesi | [PLESK_KURUMSAL_YAPI.md](PLESK_KURUMSAL_YAPI.md) |

Bu rehber günlük senkron akışını anlatır; repo adı ve Plesk standartları yukarıdaki belgelerde tanımlıdır.

---

## 1. Güvenlik (Öncelik)

Aşağıdakiler **asla** GitHub’a gitmez (`.gitignore`’da):

| Dosya / klasör | Neden |
|----------------|--------|
| `appsettings*.json` | Connection string, JWT secret, e-posta şifresi |
| `firebaseConfig*.json` | Firebase private key |
| `*.pfx` | SSL sertifikaları |
| `.env`, `.env.*` | Ortam değişkenleri |
| `**/logs/`, `**/Logs/` | Log içeriği |
| `login-body.json` | Test şifreleri |

Canlı (Plesk) tarafında bu dosyalar **deploy sırasında korunur** (`deploy-api-to-canli.ps1`).

---

## 2. Üç Taraflı Yapı

```
┌─────────────────────┐     push/pull      ┌─────────────────┐
│   Desktop           │ ◄─────────────────► │   GitHub        │
│   BASKENT_PROJE     │                     │   (tek repo)    │
│   kaynak-kod/       │                     │   main          │
└──────────┬──────────┘                     └────────┬────────┘
           │                                           │
           │ deploy-api-to-canli.ps1                   │ Plesk Git pull
           │ (publish → canlı)                         │ (isteğe bağlı)
           ▼                                           ▼
┌──────────────────────────────────────────────────────────────────┐
│   Plesk / IIS                                                     │
│   api.baskentenerji.com → C:\Inetpub\...\api.baskentenerji.com    │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Desktop → GitHub (Yerel repo bağlama)

GitHub’da repo zaten varsa:

```powershell
cd C:\Users\Administrator\Desktop\BASKENT_PROJE
git remote add origin https://github.com/CmkL-06/baskentenerji-doviz-api.git
git branch -M main
git push -u origin main
```

**Mevcut repo:** https://github.com/CmkL-06/baskentenerji-doviz-api (private).  
İlk kez repo oluşturacaksanız: GitHub’da “New repository” → repo adı (örn. `baskentenerji-doviz-api` veya `BASKENT_PROJE`) → yukarıdaki `origin` URL’sini kullanın. Detay: [GITHUB_KURUMSAL_YAPI.md](GITHUB_KURUMSAL_YAPI.md).

---

## 4. Günlük Çalışma (Desktop)

1. **Değişiklik yap** (kod: `BASKENT_PROJE\kaynak-kod\` altında; controller, entity vb.).
2. **Commit + push:**
   ```powershell
   cd C:\Users\Administrator\Desktop\BASKENT_PROJE
   git add -A
   git status   # appsettings / firebase gelmemeli
   git commit -m "Açıklayıcı mesaj"
   git push origin main
   ```
3. **Canlıya almak:**  
   `C:\Users\Administrator\Desktop\BASKENT_PROJE\deploy-api-to-canli.ps1`  
   (Önce gerekirse: `kaynak-kod` içinde `dotnet publish ... -o ..\publish-api`)

---

## 5. Plesk Tarafı

**Seçenek A – Şu anki yöntem (önerilen):**  
- Canlıya sadece **deploy-api-to-canli.ps1** ile çıktı alınır.  
- GitHub = yedek + versiyon; Plesk = publish klasöründen kopyalanan dosyalar.

**Seçenek B – Plesk Git ile çekmek:**  
- Plesk → Domains → api.baskentenerji.com → **Git** → GitHub repo URL ekle.  
- “Deploy from repository” ile çekince **appsettings.json ve web.config’i Plesk’te ayrı tutun** (üzerine yazılmasın).  
- Standartlar: [PLESK_KURUMSAL_YAPI.md](PLESK_KURUMSAL_YAPI.md).

---

## 6. Senkron Kontrol Listesi

| Kontrol | Açıklama |
|--------|----------|
| Desktop’ta `git status` temiz | Gereksiz dosya commit’lenmemiş |
| `.gitignore` içinde `appsettings*.json` | Şifreler repo’da yok |
| GitHub’da son commit = yerel `main` | Push eksik kalmamış |
| Canlıda API çalışıyor | Deploy sonrası test: `scripts\Test-API-AfterDeploy.ps1 -Login` |
| Plesk’te appsettings canlı değerlerde | Deploy betiği canlı ayarları koruyor |

---

## 7. Kısayollar

| Ne | Komut / Yol |
|----|----------------|
| **Proje kökü** | `C:\Users\Administrator\Desktop\BASKENT_PROJE` |
| API kaynak (kod) | `C:\Users\Administrator\Desktop\BASKENT_PROJE\kaynak-kod` |
| Publish çıktı | `C:\Users\Administrator\Desktop\BASKENT_PROJE\publish-api` |
| Deploy betiği | `C:\Users\Administrator\Desktop\BASKENT_PROJE\deploy-api-to-canli.ps1` |
| Deploy sonrası test | `C:\Users\Administrator\Desktop\BASKENT_PROJE\scripts\Test-API-AfterDeploy.ps1 -Login` |
| Canlı dizin | `C:\Inetpub\vhosts\baskentenerji.com\api.baskentenerji.com` |
| Belgeler | `C:\Users\Administrator\Desktop\BASKENT_PROJE\docs` |

---

GitHub repo URL’nizi `git remote add origin ...` satırına yazıp ilk `git push`’ı yaptıktan sonra üç taraflı senkron bu akışa göre çalışır. Kurumsal standartlar için [GITHUB_KURUMSAL_YAPI.md](GITHUB_KURUMSAL_YAPI.md) ve [PLESK_KURUMSAL_YAPI.md](PLESK_KURUMSAL_YAPI.md) belgelerine bakın.
