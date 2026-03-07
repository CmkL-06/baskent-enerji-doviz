# Sistem – En İyi Durum Yapılandırması

**Tarih:** 2026-02  
**Amaç:** Tüm ayarları mantıklı ve güvenli değerlere getirip tek referansta toplamak.

---

## 1. Uygulanan / Kaydedilen İyileştirmeler

| Bileşen | İyileştirme | Durum |
|---------|-------------|--------|
| **API – Program.cs** | Canlıda güvenlik başlıkları (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy) | Kaydedildi |
| **IIS API App Pool** | queueLength 2000, idleTimeout 20 dk, periodicRestart 24 saat | `scripts\IIS-API-AppPool-Optimize.ps1` çalıştırıldığında uygulanır |
| **Firewall** | 8443 (Plesk) açık; Plesk HTTPS kuralları etkin | Önceden uygulandı |
| **Deploy** | Canlı appsettings/web.config korunuyor | `deploy-api-to-canli.ps1` ile |

---

## 2. Çalıştırılacak Script’ler (İsteğe Bağlı / Tek Sefer)

```powershell
# API App Pool optimizasyonu (yönetici PowerShell)
cd C:\Users\Administrator\Desktop\BASKENT_PROJE\scripts
.\IIS-API-AppPool-Optimize.ps1
```

---

## 3. Önerilen Değerler Özeti

| Ayar | Önerilen | Nerede |
|------|----------|--------|
| **baskentenerji.com vdir** | `...\httpdocs` | IIS / Plesk |
| **api.baskentenerji.com vdir** | `...\api.baskentenerji.com` | IIS / Plesk |
| **API App Pool** | Yönetilen Kod Yok | IIS |
| **API ConnectionString** | Connection Timeout=120, Max Pool Size=200, Min Pool Size=0–10 | appsettings (canlı) |
| **Güvenlik başlıkları** | nosniff, SAMEORIGIN, XSS-Protection, Referrer-Policy | Program.cs (canlıda) |
| **Plesk erişim** | 8443 TCP açık | Firewall |

---

## 4. İlgili Belgeler

- **PLESK_PANEL_INCELEME_VE_SISTEM_YAPILANDIRMA.md** – Plesk ve IIS kontrol listesi  
- **IIS_APPLICATION_POOL_OPTIMIZASYON.md** – Plesk varsayılan havuz ayarları  
- **REFERANS_VERITABANI_VE_YAPILANDIRMA.md** – Veritabanı ve connection string  

Ayarlar bu belgeye göre iyileştirildi ve kaydedildi.
