# Başkent Enerji & Money Transfer Turkey - Master Project Repository

Bu depo, Başkent Enerji ve Money Transfer Turkey projelerinin tüm kaynak kodlarını, canlı sistem yedeklerini, otomasyon araçlarını ve teknik dokümantasyonunu tek bir çatıda birleştiren ana merkezdir.

## 🚀 Proje Genel Bakış
Sistem, finansal veri takibi, döviz kurları yönetimi ve Telegram botları aracılığıyla operasyonel yönetim sağlayan entegre bir yapıdır.

- **Frontend:** Kurumsal Dashboard ve İşlem Panelleri.
- **Backend:** .NET Web API tabanlı merkezi iş mantığı katmanı.
- **Veritabanı:** MSSQL Server (Canlı ve Yedek Veriler).
- **Botlar:** Operatör, Ruble kurları ve genel bilgilendirme için Python tabanlı Telegram botları.

---

## 📁 Klasör Yapısı ve İçerik

### 💻 Yazılım Kaynak Kodları (Core)
*   `BaskentEnerji.API/`: Ana Web API projesi (.NET).
*   `BaskentEnerji.Business/` & `.Data/` & `.Entity/`: Katmanlı mimari bileşenleri.
*   `MoneyTransferTurkey/`: Para transferi platformunun kaynak kodları.
*   `telegram-bot/`: Python tabanlı botların ana dizini ve operasyonel scriptler.

### 📦 Yayın ve Dağıtım (Deploy)
*   `deploy/`: Canlı sunucuya (Linux/Windows) gönderilmeye hazır derlenmiş (publish) paketler.
    *   `deploy-api-to-canli.ps1`: Otomatik canlı sunucu dağıtım scripti.

### 💾 Yedekler ve Veriler (Backups)
*   `backups/`: SQL Server veritabanı yedekleri (`.bak`, `.sql`, `.dump`).
    *   `fresh_backups_20260320.zip`: 20 Mart 2026 tarihli en güncel canlı sistem yedeği.
    *   `mtt_user-data_...zip.part1/2`: Parçalanmış büyük boyutlu kullanıcı veri yedekleri.

### 📚 Dokümantasyon
*   `docs/reports/`: Sistem analizleri, güvenlik raporları, karşılaştırma raporları ve kurulum rehberleri.
*   `BASKENT_PROJE.code-workspace`: VS Code için birleşik çalışma alanı (workspace) ayarları.

---

## 🛠️ Kurulum ve Çalıştırma

### **Telegram Botlarını Başlatma** (Yerel veya Sunucu)
1. `telegram-bot/` dizinine gidin.
2. `.env` dosyasındaki API tokenlarını kontrol edin.
3. `START_BOTLAR.bat` (Windows) veya `python run_all.py` (Linux) komutunu çalıştırın.

---

## 🛡️ Güvenlik ve Gizlilik
Bu depo hassas veriler (API keyler, DB bağlantıları) içerir. Sadece yetkili personel tarafından erişilmeli ve `.env` dosyaları gizli tutulmalıdır.

---

*Son Güncelleme: 20 Mart 2026*
