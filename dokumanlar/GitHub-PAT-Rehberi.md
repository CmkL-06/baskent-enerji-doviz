# GitHub Depoları İçin PAT (Personal Access Token) Rehberi

Bu rehber, **her GitHub deposu için ayrı ayrı** PAT oluşturma ve kullanma sürecini adım adım anlatır.

---

## 1. PAT Nedir?

**Personal Access Token (PAT)**, GitHub’da şifre yerine kullanılan, belirli izinlere ve süreye sahip erişim anahtarıdır. Her depo için ayrı PAT kullanmak:

- Güvenliği artırır (bir token sızdığında sadece o depo etkilenir)
- İzinleri depo bazında sınırlar
- Hangi token’ın nerede kullanıldığını takip etmeyi kolaylaştırır

---

## 2. Her Depo İçin PAT Oluşturma (Genel Adımlar)

Aşağıdaki adımları **her GitHub deposu için tekrarlayın**.

### 2.1 GitHub’da Token Sayfasına Gitme

1. GitHub’da sağ üstten profil fotoğrafına tıklayın.
2. **Settings** → sol menüden **Developer settings** → **Personal access tokens** → **Tokens (classic)** veya **Fine-grained tokens** seçin.

**Fine-grained (önerilen):** Sadece ilgili depoya yetki verir.  
**Classic:** Tüm hesapla çalışır; daha eski yöntemdir.

### 2.2 Fine-grained token (depo bazlı, önerilen)

1. **Generate new token** → **Generate new token (fine-grained)**.
2. **Token name:** Depoyu belli edecek isim verin, örn: `pat-repo-baskent-enerji-doviz`.
3. **Expiration:** 30/60/90 gün veya **No expiration** (riskli).
4. **Repository access:** **Only select repositories** → sadece bu depoyu seçin.
5. **Permissions:**
   - **Contents:** Read and write (push/pull için)
   - **Metadata:** Read-only (otomatik)
   - Gerekirse **Pull requests:** Read and write
6. **Generate token** → token’ı **bir kez** kopyalayıp güvenli yere kaydedin (sonra tekrar gösterilmez).

### 2.3 Classic token (tüm hesapla)

1. **Generate new token** → **Generate new token (classic)**.
2. **Note:** Örn: `PAT - baskent-enerji-doviz`.
3. **Expiration:** İhtiyaca göre seçin.
4. **Select scopes:** En azından `repo` (tüm repo erişimi).
5. **Generate token** → token’ı kopyalayıp saklayın.

---

## 3. Her Depo İçin Yapılacaklar (Depo Bazında)

Aşağıdaki işlemleri **her depo klasöründe** ayrı ayrı uygulayın.

### 3.1 Git ile kullanım (HTTPS)

Depo dizininde:

```powershell
cd C:\Users\CmkL-Owner\Documents\Projeler\baskent-enerji-doviz
git remote set-url origin https://<KULLANICI_ADI>:<BU_DEPO_ICIN_PAT>@github.com/<ORG_VEYA_KULLANICI>/<REPO_ADI>.git
```

Örnek:

```powershell
git remote set-url origin https://CmkL-Owner:ghp_xxxxxxxxxxxx@github.com/CmkL-Owner/baskent-enerji-doviz.git
```

**Not:** PAT’i komut satırında kullanmak kalıcı değildir; Windows Credential Manager’a kaydedilir. Daha güvenli yol: sadece `https://github.com/...` kullanıp, `git push`/`git pull` sırasında kullanıcı adı + PAT ile giriş yapmak (Credential Manager’a o depo için kaydedilir).

### 3.2 Credential Manager (Windows) – depo bazında

1. İlk `git push` veya `git pull` sırasında kullanıcı adı ve şifre istenirse:
   - **Username:** GitHub kullanıcı adınız
   - **Password:** Bu depo için oluşturduğunuz PAT
2. Windows bu bilgiyi o depo/hesap için saklar; sonraki işlemlerde otomatik kullanır.

### 3.3 Ortam değişkeni (CI/script için)

Script veya CI’da kullanacaksanız, **depo adına özel** bir değişken kullanın:

```powershell
$env:GITHUB_TOKEN_BASKENT_ENERJI = "ghp_xxxxxxxxxxxx"
```

`.env` kullanıyorsanız (dosyayı asla commit etmeyin):

```
GITHUB_TOKEN_BASKENT_ENERJI=ghp_xxxxxxxxxxxx
```

---

## 4. Depo Başına Checklist (Her Yeni Depo İçin)

Her yeni GitHub deponu için bu listeyi doldurun:

| Adım | Yapıldı | Depo adı / not |
|------|--------|-----------------|
| 1. GitHub’da bu depo için yeni PAT oluştur (fine-grained veya classic) | ☐ | |
| 2. Token adında depo adını kullan (örn: `pat-repo-xxx`) | ☐ | |
| 3. Sadece bu depoyu seç (fine-grained ise) | ☐ | |
| 4. PAT’i güvenli yerde sakla (şifre yöneticisi vb.) | ☐ | |
| 5. Bu depo klasöründe `git remote` / credential ayarla | ☐ | |
| 6. Bir test push/pull ile dene | ☐ | |
| 7. Süre bitiminde yenileme tarihini not al | ☐ | |

---

## 5. Mevcut Projeleriniz İçin Örnek Listesi

Aşağıdaki tabloyu kendi depolarınızla doldurup takip edebilirsiniz:

| Depo (proje) | PAT adı / not | Son kullanım / yenileme |
|--------------|----------------|--------------------------|
| baskent-enerji-doviz | | |
| _yedek_MoneyTransferTurkey | | |
| (yeni depo ekleyin) | | |

---

## 6. Güvenlik Önerileri

- Her depo için **ayrı** PAT kullanın; tek token’ı tüm repolarda kullanmayın.
- Mümkünse **fine-grained** token ve **sadece ilgili depo** seçin.
- Süre sınırı koyun (örn: 90 gün); bitmeden yeni token üretip eskiyi iptal edin.
- PAT’i kod içine veya repoya commit etmeyin; `.env` ve `.env.local` dosyalarını `.gitignore`’a ekleyin.
- Sızdığını düşündüğünüz token’ı GitHub’dan hemen **revoke** edin ve yeni PAT oluşturun.

---

## 7. Hızlı Başvuru: PAT Oluşturma Linki

- **Fine-grained:** https://github.com/settings/tokens?type=beta  
- **Classic:** https://github.com/settings/tokens  

Bu rehberi her yeni depo eklediğinizde “Depo Başına Checklist” bölümünü tekrarlayarak kullanabilirsiniz.
