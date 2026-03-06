# Başkent Enerji Döviz – Local Import (YARDIMCI_KLASORLER birleştirme)

**Amaç:** Masaüstündeki `YARDIMCI_KLASORLER` içeriğini döviz muhasebe repo’suna `external/local_import` olarak ekleyip tek yerde toplamak; `.git`, `node_modules`, `__pycache__` dahil edilmez.

**Önemli:** Yerelde onay vermeden asla gerçek çalıştırma yapılmaz. Önce **sanal test** (DryRun), onay sonrası **-Apply** ile gerçek çalıştırma.

---

## Orijinal komut (Linux/macOS – Git Bash / WSL)

```bash
REPO="$HOME/Desktop/baskent-enerji-doviz"
SRC="$HOME/Desktop/YARDIMCI_KLASORLER"
cd "$REPO" && mkdir -p external/local_import && rsync -av --exclude ".git" --exclude "node_modules" --exclude "__pycache__" "$SRC"/ external/local_import/ && git add external/local_import && git commit -m "Import local helper folders for consolidation scan" && git push -u origin HEAD
```

---

## Nasıl kullanılır?

1. **Repo (REPO):** Döviz muhasebe programının Git deposu. Örnekler:
   - `D:\QuantaQuokka` (SmileMedical / baskentenerji.com projesi burada ise)
   - veya clone ettiğiniz `baskent-enerji-doviz` klasörü (örn. `Desktop\baskent-enerji-doviz`)

2. **Kaynak (SRC):** Birleştirilecek yardımcı klasörlerin olduğu yer. Varsayılan: **`D:\YARDIMCI_KLASORLER`** (D sürücüsünde). Dosyaları buraya koyun.

3. **Çalıştırma:**
   - **Windows:** `Import-BaskentDovizLocalImport.ps1` script’ini REPO ve SRC’yi ayarlayıp çalıştırın (aşağıda).
   - **Git Bash / WSL:** Yukarıdaki bash komutunda `REPO` ve `SRC` yollarını kendi bilgisayarınıza göre değiştirip çalıştırın.

4. **Ne yapar?**
   - `REPO/external/local_import` oluşturulur.
   - `SRC` içeriği oraya kopyalanır (`.git`, `node_modules`, `__pycache__` hariç).
   - `git add external/local_import` → commit → `git push -u origin HEAD`.

---

## Windows (PowerShell) – Yapılandırılmış script

Aşağıdaki script’i **kendi yollarınıza göre** düzenleyin; sonra PowerShell’de çalıştırın:

- `$REPO`: Döviz muhasebe repo yolu (örn. `D:\QuantaQuokka` veya `$env:USERPROFILE\Desktop\baskent-enerji-doviz`).
- `$SRC`: Yardımcı klasörler (örn. `$env:USERPROFILE\Desktop\YARDIMCI_KLASORLER`).

Script sadece **kopyalama + git add/commit/push** yapar; başka klasörlere dokunmaz.

---

## Sanal test (önce mutlaka bunu yapın)

Hiçbir dosya kopyalanmaz, git çalıştırılmaz. Sadece ne yapılacağı listelenir.

```powershell
cd "d:\Yeni Scriptler"
.\Import-BaskentDovizLocalImport.ps1
```

veya açıkça:

```powershell
.\Import-BaskentDovizLocalImport.ps1 -DryRun
```

Çıktıda REPO/SRC/hedef yol ve kopyalanacak dosya listesi görünür. Doğruysa onay verip gerçek çalıştırmaya geçin.

---

## Gerçek çalıştırma (sadece onay sonrası)

**Onay verdikten sonra** kopyalama ve git add/commit/push için:

```powershell
.\Import-BaskentDovizLocalImport.ps1 -Apply
```

**Dikkat:** Script adı ile `-Apply` arasında **mutlaka boşluk** olmalı (`.ps1` ile `-Apply` bitişik yazılmaz).

**Script nerede?** Script `d:\Yeni Scriptler` içinde. Önce o klasöre geçin veya tam yolu yazın:

```powershell
cd "d:\Yeni Scriptler"
.\Import-BaskentDovizLocalImport.ps1 -Apply
```

Veya mevcut klasörden tam yol ile:

```powershell
& "d:\Yeni Scriptler\Import-BaskentDovizLocalImport.ps1" -Apply
```

Repo bu bilgisayarda farklı bir yoldaysa:

```powershell
.\Import-BaskentDovizLocalImport.ps1 -Apply -RepoPath "D:\sizin\repo\yolu"
```

Parametre vermeden veya sadece `-DryRun` ile çalıştırırsanız script **asla** kopyalama veya push yapmaz.

---

## PowerShell script’i yapılandırma

1. `d:\Yeni Scriptler\Import-BaskentDovizLocalImport.ps1` dosyasını açın.
2. İlk satırlardaki `$REPO` ve `$SRC` değerlerini kendi yollarınıza göre değiştirin:
   - **$REPO:** Döviz repo’su (varsayılan: `D:\QuantaQuokka`).
   - **$SRC:** Yardımcı klasörler (varsayılan: `D:\YARDIMCI_KLASORLER`). Bu klasör D: sürücüsünde; dosyaları oraya koyun.
3. Önce sanal test, sonra onayla `-Apply` çalıştırın.

---

## Git Bash / WSL (Windows’ta yol örneği)

Repo ve kaynak Windows’ta ise Git Bash’te örnek:

```bash
REPO="/d/QuantaQuokka"
SRC="/d/YARDIMCI_KLASORLER"
cd "$REPO" && mkdir -p external/local_import && rsync -av --exclude ".git" --exclude "node_modules" --exclude "__pycache__" "$SRC"/ external/local_import/ && git add external/local_import && git commit -m "Import local helper folders for consolidation scan" && git push -u origin HEAD
```

Kullanıcı adınız farklıysa `CmkL-Owner` kısmını değiştirin; repo farklı dizindeyse `REPO` ve `SRC` yi güncelleyin.
