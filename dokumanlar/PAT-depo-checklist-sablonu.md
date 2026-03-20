# Bu depo için PAT checklist

**Depo adı:** `_________________________`  
**GitHub URL:** `https://github.com/_________________________`

| # | İşlem | ✓ |
|---|--------|---|
| 1 | GitHub → Settings → Developer settings → Personal access tokens | ☐ |
| 2 | "Generate new token (fine-grained)" seç | ☐ |
| 3 | Token name: `pat-repo-<bu-depo-adi>` yaz | ☐ |
| 4 | Repository access → Only select repositories → bu depoyu seç | ☐ |
| 5 | Permissions: Contents = Read and write | ☐ |
| 6 | Generate token → token'ı kopyala ve güvenli yere kaydet | ☐ |
| 7 | Bu klasörde: `git remote -v` ile mevcut remote kontrol et | ☐ |
| 8 | Gerekirse: `git remote set-url origin https://KULLANICI:PAT@github.com/...` | ☐ |
| 9 | `git pull` veya `git push` ile test et | ☐ |
| 10 | Token bitiş tarihini not al: _________________________ | ☐ |

**Not:** Bu dosyayı her depo köküne kopyalayıp depo adını doldurarak kullanın. PAT değerini bu dosyaya yazmayın.
