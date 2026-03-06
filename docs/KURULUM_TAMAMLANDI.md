# Baskent Enerji Döviz Programı – Kurulum Özeti

**Tarih:** 25.02.2026

---

## Tamamlananlar

| Bileşen | Durum |
|---------|--------|
| **baskentenerji.com** | Document root: `httpdocs`. Ana sayfa (/) ve /login → **giriş ekranı** (/ihtiyar/) yönlendirmesi. Çalışma prensibi: site giriş ekranı ile başlar (BASKENTENERJI_CALISMA_PRENSIPLERI.md). |
| **Panel (İhtiyar)** | https://baskentenerji.com/ihtiyar/ |
| **API** | api.baskentenerji.com deploy edildi. Canlı `appsettings.json` yedek config ile güncellendi (ConnectionStrings, JwtSecretKey). |
| **GitHub** | Repo: https://github.com/CmkL-06/baskentenerji-doviz-api (private). Desktop ile senkron. |
| **tg.moneytransferturkey.com** | IIS site + reverse proxy (Flask port 5000). HTTP çalışıyor. |
| **Güvenlik** | .gitignore: appsettings*, firebase*, .env. Web.config güvenlik başlıkları eklendi. |

---

## Erişim Adresleri

| Ne | URL |
|----|-----|
| Döviz paneli | https://baskentenerji.com/ihtiyar/ |
| API | https://api.baskentenerji.com |
| API Swagger | https://api.baskentenerji.com/swagger |
| Operatör paneli (tg) | http://tg.moneytransferturkey.com/operator (Flask çalışıyorsa) |

---

## Günlük Kullanım

1. **Kod değişikliği → GitHub:**  
   `C:\Users\Administrator\AnasıBerdus-Deniz` → `git add -A` → `git commit -m "..."` → `git push origin main`

2. **Canlıya alma:**  
   `C:\Users\Administrator\Desktop\BASKENT_PROJE\deploy-api-to-canli.ps1`  
   (Gerekirse önce: `dotnet publish ... -o publish-api`)

3. **Bot/Flask:**  
   `C:\Users\Administrator\Desktop\bot\autostart.bat`

---

## Notlar

- API login bazen timeout verebilir (soğuk başlangıç); birkaç saniye sonra tekrar deneyin.
- tg için HTTPS isterseniz Plesk Let's Encrypt ile sertifika ekleyip 443 binding tanımlayın.
- Şifreler ve bağlantı bilgileri artık canlı `appsettings.json` içinde; bu dosya deploy sırasında korunur.
