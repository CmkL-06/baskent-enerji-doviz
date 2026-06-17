# Veri Kaybı Analiz Raporu — GoDaddy cPanel

**Tarih:** 17 Haziran 2026  
**Analiz:** cPanel Terminal + phpMyAdmin + Backup dosya taraması

---

## 1. Durum Özeti

**Ana bulgu:** GoDaddy MariaDB'deki boş görünen veritabanları büyük olasılıkla hiç doldurulmadı.  
Asıl üretim verisi (döviz işlemleri, exchange) **Windows Server (159.195.55.1) MSSQL'de** bulunmaktadır.  
GoDaddy'deki `full` ve `moneytransfer` veritabanları boş migration hedefleriydi.

---

## 2. GoDaddy Veritabanı Durumu (Tam Tablo)

| Veritabanı | Tablo | Satır | Durum | Notlar |
|---|---|---|---|---|
| `aleynaekinci` | 41 | 2754 | ✅ Sağlıklı | aleynaekincibeauty.com CMS, son güncelleme Mar 17 |
| `kulyatirimgroup_site` | 81 | 5030 | ⚠️ Eski | Son güncelleme 2023-09-13, 3 yıl dokunulmamış |
| `kulyatirimgroup` | 16 | 84 | ⚠️ Eski | Son güncelleme 2023-05-14 |
| `changeoff` | 23 | 67 | ✅ Aktif | Dün güncellendi (2026-06-16) |
| `i9369962_wp1` | 12 | 3 | 🔴 Neredeyse boş | alanyayatirim.com WP — sadece varsayılan kurulum postları |
| `i9369962_rgrl1` | 12 | 163 | ⚠️ Güncellenmemiş | İçerik var ama hiç yazılmamış (NULL update_time) |
| `puantaj2015` | 80 | 1362 | ⚠️ Belirsiz | NULL update_time |
| `full` | 0 | 0 | 🔴 Tamamen boş | Migration hedefi, hiç doldurulmadı |
| `moneytransfer` | 0 | 0 | 🔴 Tamamen boş | Migration hedefi, hiç doldurulmadı |

---

## 3. i9369962_wp1 (alanyayatirim.com WordPress) İçeriği

```
post_status | post_type | post_title      | post_date
------------|-----------|-----------------|--------------------
publish     | post      | Hello world!    | 2023-05-14 01:33:40
publish     | page      | Sample Page     | 2023-05-14 01:33:40
draft       | page      | Privacy Policy  | 2023-05-14 01:33:40
```

**Sonuç:** WordPress 2023-05-14'te kurulmuş ama **hiç içerik girilmemiş**. Bu bir veri kaybı değil, boş kurulum.  
Siteye ilan/içerik eklenmesi gerekiyorsa elle yapılmalı.

---

## 4. Yedek Dosya Envanteri

### 4.1 `~/.mysql_backup/` (Root cron — 10 Mayıs 2026)

> ⚠️ Bu yedekler **veritabanlar zaten boşken** alınmış — kurtarma sağlamaz.

| Dosya | Boyut | Durum |
|---|---|---|
| `full.sql.gz` | 596 B | Boş (sadece header) |
| `moneytransfer.sql.gz` | 615 B | Boş (sadece header) |
| `i9369962_wp1.sql.gz` | 30K | 3 varsayılan post |
| `i9369962_rgrl1.sql.gz` | 18K | Mevcut içerik |
| `kulyatirimgroup_site.sql.gz` | 102K | 2023 verisi |
| `kulyatirimgroup.sql.gz` | 8.8K | 2023 verisi |
| `puantaj2015.sql.gz` | 869K | Puantaj verisi |

### 4.2 `~/backups-repo/` — KRİTİK MSSQL Yedekleri (19 Mart 2026)

> ✅ Bu dosyalar **Windows Server MSSQL'e restore edilmesi gereken asıl verilerdir.**

| Dosya | Boyut | Tür | İçerik |
|---|---|---|---|
| `fresh_backups_20260320.zip` | 9.4M | ZIP | `mtt-moneyexchangeturkey.bak` (42.9MB) + `mtturkey_exchange.bak` (19.5MB) |
| `mtturkey_exchange_2025-11-20.zip` | 6M | ZIP | `mtturkey_exchange_2025-11-20_06-48-47` (42.7MB) — MSSQL .bak |
| `backup_dbdump_2511200657.dump` | 41M | NTBackup | Windows NT Backup + MSSQL (Kasım 2025, eski Plesk) |
| `baskent_user-data_2602171048.zip` | 50M | ZIP | api.baskentenerji.com deploy DLL'leri |
| `mtt_user-data_2602171048.zip.part1+2` | 173M | Split ZIP | Büyük kullanıcı/uygulama veri arşivi |
| `moneytransfer.sql` | 1.6K | SQL | Boş dump (CREATE DATABASE + header only) |

---

## 5. Kök Neden Analizi

### Neden `full` ve `moneytransfer` veritabanları boş?

Bu iki veritabanı, eski MoneyTransferTurkey sisteminin MariaDB'ye taşınması için oluşturulmuş placeholder'lardı.  
Taşıma **hiçbir zaman tamamlanmadı** — asıl veriler hep MSSQL'de kaldı.

### Neden alanyayatirim.com WordPress boş?

WordPress 2023'te kurulmuş ama site içeriği (ilanlar, sayfalar) girilmemiş.  
DNS sorunu nedeniyle site uzun süre erişilemez durumdaydı.

### Gerçek veri kaybı var mı?

- **GoDaddy MariaDB**: Hayır — boş olan şeyler hep boştu.
- **Windows Server MSSQL**: Kontrol edilmedi. `fresh_backups_20260320.zip` içindeki `.bak` dosyaları GoDaddy'de saklanıyor ama sunucuda restore edilip edilmediği bilinmiyor.

---

## 6. Acil Yapılması Gerekenler

### 🔴 En Kritik: MSSQL Backup'ları Windows Server'a Restore Et

GoDaddy'deki `fresh_backups_20260320.zip` içindeki dosyaları Windows Server'a (159.195.55.1) transfer et:

```
mtt-moneyexchangeturkey.bak → SSMS ile restore: mtt-moneyexchangeturkey
mtturkey_exchange.bak       → SSMS ile restore: mtturkey_exchange (şema)
```

**Transfer yöntemi:**
```powershell
# Windows Server'da çalıştır:
# GoDaddy'den scp/SFTP ile çek veya cPanel File Manager'dan indir
# SSMS → Databases → Restore Database → Device → .bak dosyası seç
```

### 🟡 Otomatik MySQL Yedekleme Düzelt

`~/.mysql_backup/` klasörü root'a ait — bu bir root cron job. Yedekler güncel mi kontrol et:

```bash
crontab -l  # kullanıcı crontab
sudo crontab -l  # root crontab (GoDaddy'de çalışmayabilir)
```

GoDaddy shared hosting'de UpdraftPlus kurulumu önerilir (WordPress için).

### 🟡 alanyayatirim.com WordPress İçerik Planı

Site tamamen boş. DNS artık doğru (92.205.13.107). SSL yenilenince içerik girilmeli:
- Kul Gayrimenkul ilanları → WordPress
- Sayfalar: Hakkımızda, İletişim, Hizmetler

### 🟢 AutoSSL (DNS Propagation Sonrası)

```
cPanel → SSL/TLS Status → Run AutoSSL
```
Hem alanyayatirim.com hem baskentdanismanlik.com için.

---

## 7. `aleynaekinci` Veritabanı — Mevcut Sağlıklı Yapı

En aktif ve içerikli veritabanı. 41 tablo, 2754 satır, 7.16 MB.  
**Hemen yedeklenmeli:**

```bash
# cPanel Terminal'de:
mysqldump -u aleynaekinci_user -p aleynaekinci > ~/aleynaekinci_backup_$(date +%Y%m%d).sql
```

---

*Rapor: 17 Haziran 2026 — Claude (Anthropic) — cPanel Terminal analizi*
