# İsim Değişiklikleri – Referans (API / Veritabanı)

**Dikkat:** Veritabanında veya dokümanlarda kullanıcı adı / mail değişiklikleri yapılmış olabilir. Giriş ve Seed betikleri buna göre kullanılmalı.

---

## 1. Tablo ve şema

| Ortam | Tablo |
|-------|--------|
| **EF Core / API** | Şema: `mtturkey_exchange`, Tablo: `Users` |
| **Tam nitelikli** | `mtturkey_exchange.Users` |

---

## 2. Kullanıcı alanları (giriş için)

- **Girişte kullanılan:** API’ye `Mail` veya `email` gönderilir; API hem **Mail** hem **Username** ile eşleştirir:  
  `u.Mail == requestData.Mail || u.Username == requestData.Mail`
- **Şifre:** `Password` alanında BCrypt (`$2a$...`) veya 64 karakterlik SHA256 hex kullanılır.

---

## 3. Yedek CSV’deki gerçek kullanıcılar (DB_YEDEK)

| Username  | Mail                  | Firstname | Lastname   | Rank | Not |
|-----------|------------------------|-----------|------------|------|-----|
| ihtiyar   | f..@f.com              | Cem       | Ekinci     | 99   | Owner hesabı (dokümanda username ihtiyar) |
| Damat     | damat@gmail.com        | Damat     | Donik      | 99   | Admin |
| PERSONEL  | personel@gmail.com     | ELEMAN    | ELEMANOGLU | 1    | Personel |
| Perso     | kargicak@gmail.com     | PERSONEL  | ELEMAN     | 1    | Alternatif personel adı |

**Önemli:** Hem **PERSONEL** hem **Perso** var; ikisi de personel rolünde. Seed betiği her iki isim için de şifre güncellemeli.

---

## 4. Dokümandaki giriş bilgileri (BELLEK-V4)

| Rol     | Username | Şifre     | Açıklama        |
|---------|----------|-----------|------------------|
| Owner   | ihtiyar  | owner1    | Cem Kul          |
| Admin   | Damat    | admin1    |                 |
| Personel| PERSONEL | personel1 |                 |

Giriş: **Mail** veya **Username** + **Şifre** (örn. Mail=`ihtiyar`, Şifre=`owner1`).

---

## 5. İsim değişikliği yapıldıysa

- Veritabanında **Username** veya **Mail** değiştirildiyse: Seed/UPDATE betiklerinde `WHERE Username = N'...'` veya `WHERE Mail = N'...'` değerlerini **canlı DB’deki güncel isimlere** göre güncelleyin.
- **Perso / PERSONEL:** İkisi de varsa Seed betiği ikisini de güncellemeli (personel1 şifresi).
- Yeni bir “owner” veya “admin” kullanıcı adı kullanıyorsanız, Seed betiğine o **Username** için de UPDATE ekleyin.

---

## 6. Seed betiği kapsamı

- **Güncellenen hesaplar:** `ihtiyar`, `Damat`, `PERSONEL`, **`Perso`** (isim değişikliği nedeniyle eklenmeli).
- **Şifreler:** owner1, admin1, personel1 (SHA256 hash; API hem SHA256 hem BCrypt kabul eder).

Bu referans, isim değişikliklerinden sonra doğru kullanıcıyla giriş ve doğru hesaba şifre atamak için kullanılır.

---

## 7. Canlı bağlantı (güncel)

**Tek referans:** **REFERANS_VERITABANI_VE_YAPILANDIRMA.md**

- **Veritabanı adı:** **mtt-moneyexchangeturkey** (şema: mtturkey_exchange)
- **Instance:** `localhost\SQLEXPRESS` | **SQL Login:** `mtturkey_exchange`
- **API login:** `POST https://api.baskentenerji.com/api/v1/User/login` — `{"Mail":"ihtiyar","Password":"owner1"}`
