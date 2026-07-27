# Başkent Enerji Döviz Yönetim Sistemi — Çalışma Talimatları

Bu dosya, bu proje üzerinde çalışırken izlenmesi gereken kuralları, mimariyi ve operasyonel
bilgiyi özetler. Her göreve başlamadan önce oku; burada yazanlar varsayılan davranışı geçersiz
kılar.

## 0. Temel Kurallar (İhlal Edilmez)

1. **Sadece canlı sunucuyu kullan.** Kod ve deploy her zaman `C:\inetpub\baskent-enerji-doviz\`
   üzerinden yapılır. Masaüstündeki (Desktop) herhangi bir kopyaya asla dokunma.
2. **Görev sırası kesindir.** Bir görev tamamlanmadan başka bir göreve geçilmez.
3. **Verimlilik.** Tek seferde tam, nihai çıktı ver; iteratif deneme-yanılma yapma.
4. **Büyük/finansal değişikliklerde onay şart.** Kod değişikliği yapılabilir ve build/test
   edilebilir onay beklemeden — ama **deploy** (canlıya alma) ve **veritabanı yazma işlemleri**
   (rank değiştirme, kur güncelleme, manuel SQL UPDATE) için HER SEFERİNDE taze, o işleme özel
   onay gerekir. Daha önce verilmiş genel bir "evet" sonraki bir deploy'u kapsamaz.
5. **Kasa/Cari tablo tasarımı:** Alınan/Verilen ayrı sütunlar tercih edilir; çelişkili tek TL
   değeri istenmez.
6. **web.config, Material Symbols Outlined, .fi (flag-icons) kullanımına** ilişkin stil
   kurallarına uy; deploy sırasında `web.config`'i koru (robocopy `/XF` ile appsettings hariç
   tutulur, web.config zaten hedefte zaten var ve `/MIR` onu silmez çünkü kaynakta da mevcut
   olmalı — kaynak publish çıktısında yoksa elle kontrol et).

## 1. Mimari Özet

- **Backend:** .NET 8 / EF Core — `BaskentEnerji.API` (controller'lar), `BaskentEnerji.Business`
  (servisler), `BaskentEnerji.Data` (DbContext), `BaskentEnerji.Entity` (entity/view model'ler).
- **Frontend:** Vue 3 + Pinia + TypeScript — `frontend/src/`. Ayrıca `frontend-tg-dealer` (bayi
  paneli, ayrı Vue app) ve `telegram-bot` (Python) var.
- **Veritabanı:** SQL Server, instance `.\SQLEXPRESS`, database `mtturkey_exchange`.
- **IIS Siteleri:**
  - `BaskentEnerji-API` → `C:\inetpub\wwwroot\api` (app pool aynı adla)
  - `BaskentEnerji-Frontend` → `C:\inetpub\wwwroot\frontend`
  - Canlı adresler: `https://baskentenerji.com` (frontend), `https://api.baskentenerji.com`
    (backend, `/swagger/index.html` health-check için kullanılır).
- **Git:** `https://github.com/CmkL-06/baskent-enerji-doviz` (origin) — push için kullanıcıdan
  açık onay al.

### Kritik iş mantığı noktaları
- **WAC (Weighted Average Cost):** `WacService.cs` — her alışta ağırlıklı ortalama maliyet
  güncellenir (`RecalculateWacOnPurchaseAsync`), satışta WAC DEĞİŞMEZ sadece miktar azalır
  (`AdjustWacQuantityAsync`), gerçekleşen kâr `(satışKuru - WAC) × miktar` formülüyle hesaplanır
  (`CalculateRealizedProfitAsync`). Bu formüller bu oturumda gerçek verilerle sayısal olarak
  doğrulandı (bkz. bölüm 4).
- **TransactionType enum:** `0=Buy, 1=Exchange, 2=Deposit, 3=Withdrawal, 4=Transfer,
  5=Adjustment, 6=Party`. "Exchange" (1) hem satışı hem arbitrajı kapsar — ayrı bir "Sell" değeri
  yoktur; frontend'de hangi bacağın TRY olduğuna bakılarak Alış/Satış/Arbitraj ayrımı yapılır.
- **Kasa/Vault değerleme:** `VaultService.cs` — satılmamış (elde kalan) döviz stoku artık GÜNCEL
  PİYASA KURU değil, WAC ile değerlenir (gerçekleşmemiş kâr erken muhasebeleştirilmesin diye);
  WAC ile piyasa kuru arasındaki fark ayrıca "Anlık Piyasa Farkı" olarak gösterilir
  (`UnrealizedProfit`).
- **Z-Raporu maliyet/satış kuru ayrımı:** Bir "Döviz Satış" satırında "Alış Kuru" olarak işlemin
  kendi kuru değil, satıştan önceki gerçek ortalama maliyet (`CurrencyWacHistories.OldWac`,
  transactionId+currencyId ile eşleştirilir) gösterilir; bulunamazsa Net Kâr/Zarar'dan geriye
  doğru türetilir (`ModernZReport.vue`, `costBasisRate` hesaplaması).
- **Kullanıcı rolleri:** `rank` alanına göre — `rank>=100` Owner (👑), `rank>=99` Admin,
  `rank>=50` Moderator/Staff. Owner-only işlemler (gider onayı, gün kapanışı onayı vb.) rank<100
  ise arayüzde buton bile göstermez — sistemde HİÇ rank≥100 kullanıcı kalmazsa bu işlemler
  kimse tarafından yapılamaz hale gelir (bu oturumda böyle bir kilitlenme yaşanıp düzeltildi).
- **Gider tanımı silme:** Ödemesi olan bir `ExpenseDefinition` gerçekten silinmez, sadece pasife
  alınır (`IsActive=false`) — geçmiş kayıtlar bozulmasın diye. Frontend bunu ayrı mesajla
  belirtir ("geçmiş ödeme kaydı olduğu için pasife alındı").
- **Kur kaynakları:** TCMB/Binance/OpenER sağlayıcılarından otomatik çekilen ~21 para birimi var;
  KRUB (Kart Ruble) ve MEUR (Metal Euro) gibi egzotik birimlerde otomatik kaynak YOK — manuel
  girilir ve zamanla bayatlayabilir. Bu birimlerin kurunu güncellemeden önce KULLANICIDAN gerçek
  değerleri iste, tahmin etme.

## 2. Standart Denetim/Geliştirme İş Akışı

1. İlgili bölüm için 2 paralel `Explore`/`general-purpose` Agent (backend + frontend odaklı)
   dispatch et.
2. Agent bulgularını **KÖRÜ KÖRÜNE GÜVENME** — her bulguyu ilgili kodu doğrudan okuyarak
   bağımsız doğrula (bu oturumda agent'lar defalarca yanlış pozitif üretti: zaten düzeltilmiş
   ya da tasarım gereği olan durumları "hata" olarak raporladı).
3. Sadece doğrulanmış gerçek sorunları düzelt.
4. Build: backend `dotnet build BaskentEnerji.API/BaskentEnerji.API.csproj -c Release`,
   frontend `cd frontend && npm run build`.
5. Test: `dotnet test BaskentEnerji.Business.Tests/BaskentEnerji.Business.Tests.csproj -c
   Release` — **baseline: 60 test, 53 geçer, 7 önceden var olan hata** (hepsi
   `ZReportServiceTests.cs` içinde, "Dictionary key collision" — test verisi üretim hatası,
   gerçek bir üretim kodu hatası DEĞİL). Bu sayı değişirse dikkatle incele.
6. `AskUserQuestion` ile deploy için TAZE onay al.
7. Deploy (bkz. bölüm 3).
8. Canlıda `claude-in-chrome` / Browser pane ile doğrula (screenshot, console error kontrolü —
   `exchange-*.js` gibi eski bundle hash'lerine referans veren console hataları bilinen/ilgisiz
   önbellek kalıntılarıdır, göz ardı edilebilir).

## 3. Deploy Komutları

### Backend
```bash
cd /c/inetpub/baskent-enerji-doviz
rm -rf /c/temp_api_publish
dotnet publish BaskentEnerji.API/BaskentEnerji.API.csproj -c Release -o /c/temp_api_publish
```
```powershell
Stop-WebAppPool -Name "BaskentEnerji-API"
Start-Sleep -Seconds 3
robocopy "C:\temp_api_publish" "C:\inetpub\wwwroot\api" /MIR /NFL /NDL /NJH /NJS /XF appsettings.json /XF appsettings.Development.json /R:3 /W:5
Start-WebAppPool -Name "BaskentEnerji-API"
```
Doğrulama:
```bash
md5sum /c/temp_api_publish/BaskentEnerji.Business.dll /c/inetpub/wwwroot/api/BaskentEnerji.Business.dll
curl -sk -o /dev/null -w "%{http_code}\n" https://api.baskentenerji.com/swagger/index.html   # 200 bekleniyor
```

### Frontend
```bash
cd /c/inetpub/baskent-enerji-doviz/frontend && npm run build
```
```powershell
robocopy "C:\inetpub\baskent-enerji-doviz\frontend\dist" "C:\inetpub\wwwroot\frontend" /MIR /NFL /NDL /NJH /NJS
```
Not: robocopy `/MIR` eski hash'li dosyaları "EXTRA File" olarak silmesi normaldir (yeni build her
zaman farklı hash'li dosya adları üretir). Exit code 1 = "dosyalar kopyalandı", hata değildir.

## 4. Veritabanı Erişimi

**SADECE Windows Authentication (`-E`) kullan** — kullanıcı adı/parola ile `sqlcmd` bağlantısı
güvenlik sınıflandırıcısı tarafından "credential leakage" olarak engellenir:
```powershell
sqlcmd -S ".\SQLEXPRESS" -d mtturkey_exchange -E -Q "SELECT ..." -W
```
Türkçe karakter içeren `WHERE` filtreleri (örn. `Name='Kargıcak Kasa'`) encoding sorunları
yüzünden 0 satır dönebilir — GUID ile filtrelemeyi veya `GROUP BY` ile sonucu SQL'den almayı
tercih et.

## 5. Bilinen Veri Durumu (bu dosyanın güncellendiği tarih itibarıyla)

- 2 ofis: Başkent Enerji Merkez, Kargıcak Şubesi — 2 kasa: Merkez Kasa, Kargıcak Kasa.
- Owner (rank≥100): **Cem Ekinci** (kullanıcı adı `ihtiyar`). Admin (rank 99): Aleyna. Staff
  (rank 50): Damat.
- KRUB ve MEUR kurları manuel/bayat — güncellemeden önce kullanıcıdan gerçek değer iste.

---
Bu dosyayı, mimaride veya kurallarda kalıcı bir değişiklik yaptığında güncel tut.
