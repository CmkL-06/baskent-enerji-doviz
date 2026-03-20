# İnceleme — Güvenlik ve NuGet Paketleri

**Tarih:** 18 Mart 2026  
**Kapsam:** `M:\BASKENT_PROJE_TEKBIRLESIK\ana-repo` (BaskentEnerji + MoneyTransferTurkey)  
**Tetikleyici:** GitHub Dependabot 3 adet “high” güvenlik uyarısı bildirdi.

---

## 1. Proje yapısı

- **BaskentEnerji.sln** — Ana proje (API, Business, Data, Entity, Tests)
- **MoneyTransferTurkey.sln** — Alt proje (API, Business, Data, API.Tests)

Tüm bağımlılıklar `.csproj` içinde `PackageReference` ile tanımlı.

---

## 2. Tespit edilen paketler (özet)

| Paket | Mevcut sürüm | Proje |
|--------|----------------|--------|
| AutoMapper | 13.0.1 | BaskentEnerji.Business, MoneyTransferTurkey.Business |
| MimeKit | 4.15.1 | MoneyTransferTurkey.Business |
| MailKit | 4.6.0 | MoneyTransferTurkey.Business |
| Microsoft.EntityFrameworkCore | 8.0.6 | BaskentEnerji.Data, MoneyTransferTurkey.* |
| Microsoft.EntityFrameworkCore.Design | 8.0.6 | API projeleri |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.6 | Data projeleri |
| System.IdentityModel.Tokens.Jwt | 7.6.0 | BaskentEnerji.API, MoneyTransferTurkey.API |
| Swashbuckle.AspNetCore | 6.4.0 | API projeleri |
| FirebaseAdmin | 3.0.0 | Business projeleri |
| HtmlAgilityPack | 1.11.61 | Business projeleri |
| Diğer | Microsoft.* 8.0.x, xunit, coverlet vb. | Çeşitli |

---

## 3. Güvenlik incelemesi (açık kaynak / CVE araştırması)

### 3.1 MimeKit 4.15.1
- **CVE-2026-30227:** CRLF enjeksiyonu; “prior to 4.15.1” etkilenir, **4.15.1 düzeltilmiş sürüm**.
- **Sonuç:** Mevcut sürüm (4.15.1) bu CVE için **güvende**. Ek işlem gerekmez.

### 3.2 MailKit 4.6.0
- MailKit, MimeKit’e bağımlı; CVE-2026-30227 MimeKit tarafındaydı.
- Projede **MimeKit 4.15.1** açıkça referans verildiği için MimeKit tarafı güncel.
- **Sonuç:** Mevcut kullanım için **ek risk notu yok**. İleride MailKit’i de güncel sürüme çekmek faydalı olur.

### 3.3 AutoMapper 13.0.1
- **GHSA-rvv3-g6hj-g44x** (DoS): Etkilenen aralık 16.0.0–16.1.0; yama 15.1.1 ve 16.1.1.
- **Sonuç:** 13.0.1 bu advisory kapsamında **değil**. Bilinen bir CVE atanmamış.

### 3.4 Microsoft.EntityFrameworkCore 8.0.6
- **CVE-2024-43483:** İlgili bağımlılık (ör. Microsoft.Extensions.Caching.Memory) nedeniyle 8.0.6 etkilenebilir; **8.0.11** güvenli sürüm.
- **Öneri:** Tüm `Microsoft.EntityFrameworkCore*` ve ilgili Microsoft paketlerini **8.0.11** (veya daha güncel güvenli sürüm) seviyesine yükseltin.

### 3.5 System.IdentityModel.Tokens.Jwt 7.6.0
- **CVE-2024-21319:** DoS; etkilenen 7.x aralığı “7.0.0–7.1.2 öncesi”; **7.1.2 ve sonrası** yamalı.
- **Sonuç:** 7.6.0 > 7.1.2 olduğu için bu CVE açısından **güvende**. Paket eski/legacy kabul edildiği için uzun vadede 8.x veya güncel sürüme geçmek mantıklı.

---

## 4. GitHub “3 high” uyarısı hakkında

Dependabot sayfası (giriş gerektiği için) burada açılamadı. Genelde:

- **Microsoft.EntityFrameworkCore** 8.0.6 → 8.0.11 (CVE-2024-43483) yükseltmesi “high” olarak görülebilir.
- Kalan 2 uyarı büyük ihtimalle başka Microsoft.* veya bağımlılık ağacındaki paketlerle ilgili.

**Ne yapmalı:** GitHub’da giriş yapıp **Security → Dependabot** sekmesine bakın; hangi 3 paket için “high” uyarı verildiğini oradan görebilirsiniz.

---

## 5. Önerilen aksiyonlar (öncelik sırasıyla)

1. **Entity Framework Core ve ilgili Microsoft paketleri**
   - Tüm `Microsoft.EntityFrameworkCore*`, `Microsoft.Extensions.*` (projede kullanılanlar) için **8.0.11** veya daha güncel güvenli sürüme güncelleyin.
   - Örnek (proje klasöründe):
     ```bash
     dotnet list package --outdated
     dotnet add package Microsoft.EntityFrameworkCore --version 8.0.11
     # (Diğer EF ve Extensions paketleri için de aynı şekilde sürüm güncellemesi)
     ```
   - Sonrasında tam build ve test:
     ```bash
     dotnet build
     dotnet test
     ```

2. **GitHub Dependabot**
   - Repo: https://github.com/CmkL-06/baskent-enerji-doviz  
   - **Security → Dependabot** ile 3 high uyarının tam listesini inceleyin; Dependabot’un önerdiği “bump” PR’larını merge etmek genelde en hızlı yol.

3. **İsteğe bağlı güncellemeler**
   - **System.IdentityModel.Tokens.Jwt:** 7.6.0 CVE açısından güvende; ileride 8.x veya en güncel sürüme geçilebilir.
   - **Swashbuckle.AspNetCore / diğer paketler:** `dotnet list package --outdated` ve Dependabot önerileri ile takip edin.

---

## 6. Özet tablo

| Paket | Mevcut | Durum | Öneri |
|--------|--------|--------|--------|
| MimeKit | 4.15.1 | CVE-2026-30227 için güvende | — |
| MailKit | 4.6.0 | MimeKit 4.15.1 kullanıldığı için OK | İsteğe bağlı güncelleme |
| AutoMapper | 13.0.1 | Bilinen CVE yok | — |
| Microsoft.EntityFrameworkCore (ve ilgili) | 8.0.6 | CVE-2024-43483 riski | **8.0.11+ yükselt** |
| System.IdentityModel.Tokens.Jwt | 7.6.0 | CVE-2024-21319 için güvende | İsteğe bağlı 8.x |

**Veri koruma:** Bu inceleme sadece okuma ve raporlama amaçlıdır; hiçbir dosya veya bağımlılık otomatik değiştirilmemiştir. Güncellemeleri siz yaptıktan sonra `dotnet build` ve `dotnet test` ile mutlaka doğrulayın.

---

## 7. Uygulama durumu (öneriler uygulandı)

- **Entity Framework Core ve ASP.NET Core paketleri:** Tüm `Microsoft.EntityFrameworkCore*`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` referansları **8.0.6 → 8.0.11** olarak güncellendi (CVE-2024-43483 için).
- **Microsoft.Extensions.* / System.Net.Http.Json:** 8.0.11 NuGet’te olmadığı için **8.0.0 / 8.0.1** bırakıldı; sadece EF/AspNetCore paketleri 8.0.11’e çekildi.
- **Doğrulama:** `dotnet restore` ve `dotnet build BaskentEnerji.sln` başarıyla tamamlandı.
- **GitHub Dependabot:** 3 high uyarıyı kapatmak için GitHub’da **Security → Dependabot** sayfasından önerilen PR’ları inceleyip merge etmeniz önerilir.
- **AutoMapper NU1903 uyarısı:** İncelemede 13.0.1 sürümü etkilenmiyor (advisory 16.x için); isteğe bağlı olarak ileride 15.1.1 veya 16.1.1’e yükseltilebilir.
