# Masaüstünden Plesk Obsidian’a Erişim

**Amaç:** Bu bilgisayardan (veya uzak masaüstünden) Plesk Obsidian paneline girmek.

---

## Uygulanan izin ve yapılandırmalar

- **Güvenlik duvarı:** 8443 TCP gelen trafik için kural eklendi: `Plesk HTTPS 8443 (SMB Web)` (dir=in, action=allow, localport=8443).
- **Mevcut Plesk kuralları (etkin):** Plesk, Plesk Newsfeeds, SECURITY: Allow Plesk HTTPS, SECURITY: Allow Plesk HTTP — hepsi Inbound Allow.
- **Port durumu:** 8443 bu sunucuda dinleniyor (0.0.0.0:8443 ve [::]:8443 LISTENING).

---

## 1. Hızlı erişim

- **Masaüstü kısayolu:** `Desktop\Plesk Obsidian.url`  
  Çift tıklayın → tarayıcıda Plesk açılır (sunucu IP: 45.84.191.180, port 8443).

---

## 2. Adresler

| Ortam | Adres |
|--------|--------|
| Sunucunun dış IP’si | **https://45.84.191.180:8443** |
| Sunucuda yerel | **https://localhost:8443** veya **https://127.0.0.1:8443** |
| Alan adı (Plesk buna bağlıysa) | **https://baskentenerji.com:8443** veya **https://panel.baskentenerji.com** |
| SMB Web View (Plesk içi) | **https://localhost:8443/smb/web/view** |

Tarayıcıya bu adreslerden birini yazıp Enter’a basın.

---

## 3. İlk giriş

1. Adres açıldığında **“Güvenli değil” / sertifika uyarısı** çıkarsa → **Gelişmiş** → **… adresine git** ile devam edin (sunucu sertifikası self-signed olabilir).
2. Plesk giriş sayfasında **kullanıcı adı** ve **şifre** ile giriş yapın (sunucu kurulumunda veya hosting sağlayıcısı tarafından verilir).

---

## 4. Erişim olmazsa

- **Firewall:** Sunucuda 8443 portu dışarıya açık olmalı (Plesk’te **Araçlar ve Ayarlar** → **Güvenlik** → **Güvenlik Duvarı**).
- **Plesk servisi:** Sunucuda Plesk’in çalıştığından emin olun (Windows: Hizmetler’de “Plesk”).
- **IP değişikliği:** Sunucu IP’si 45.84.191.180 değilse, masaüstü kısayolundaki `.url` dosyasını Not Defteri ile açıp `URL=` satırındaki adresi yeni IP ile değiştirin.

---

**İlgili belge:** `PLESK_TUM_YAPILANDIRMA_TAMAMLAMA.md` — domain ve hosting ayarları.
