# Cursor / Agent Bellek – Ortak Bağlam

Bu dosya, Cursor ve diğer agent'ların sürekli referans alabileceği **ortak bellek** özetidir. Güncel tutuldukça PDR veya başka bir sisteme aktarılabilir.

---

## 1. Çalışma alanı ve çalışma prensipleri – talimat

- **Çalışma alanı tanımı bu bellek dosyasında sabitlenmez.** Agent, **masaüstü taraması / desktop taraması sonucunu** (veya mevcut workspace kökündeki klasör yapısı, dokümanlar ve script çıktılarını) inceleyerek **çalışma alanını kendisi anlamalıdır.**
- Bu anlayışa dayanarak agent, **kendisi için çalışma prensipleri oluşturmalıdır:** hangi klasörün kök olduğu, hangi projeye odaklanılacağı, dry-run → onay → uygulama, kopya/yedek politikası, yetki talepleri vb. İlk kez bu workspace’e girildiğinde veya belirsizlik olduğunda **desktop/workspace taraması yapıp sonucu yorumlayarak** bu prensipleri türetir ve gerekiyorsa bir dokümana (ör. `CALISMA_PRENSIPLERI.md` veya `.cursor/rules` altına) yazar.

---

## 2. SmileMedical projesi

- **Güncel talimat (deployment):** Proje şimdilik **beklemede**. Web’e yükleme yapılmayacak; kaynak **sadece web’den çekilecek** (git pull vb.), çalıştırma **sadece lokalde** olacak.
- **İşlev:** Döviz bürosu, site yönetimi, blog, coin (kripto) modülleri. .NET 8 API + Vue 3 frontend.
- **Domain referansları (projede geçen):** tradepanic.com, moneytransferturkey.com, baskentenerji.com.
- **Kullanıcıya ait:** **moneytransferturkey.com** (alan adı size ait). **Sunucu** (45.84.191.180) size ait: **VDS** (Virtual Dedicated Server); erişim **uzak masaüstü** (RDP) ile. SQL Server ve hosting bu VDS üzerinde.
- **tradepanic.com:** Canlı site (crypto portfolio tracker / price alerts). Projede bu domain de CORS ve config'te geçiyor; sahiplik kullanıcı tarafından netleştirilmedi.
- **Örnek / demo:** appsettings.Example.json, .env örnekleri; ilk çalıştırmada varsayılan admin (HostService); frontend'de "Demo giriş (API yok)".

---

## 3. Hassas bilgiler – konumlar (değerler yazılmaz)

- **Varsayılan admin:** HostService.cs — username, password, mail (ilk kurulum seed).
- **SQL Server:** appsettings.json (API ve Data) — ConnectionStrings:SQL (User Id, Password); sunucu IP proje dokümanlarında geçiyor.
- **E-posta / JWT:** appsettings.json — JwtIssuer, JwtAudience, EmailSettings, Domain (tradepanic.com, info@tradepanic.com).
- **Güvenlik:** Bu dosyada şifre veya token değeri tutulmaz; sadece "nerede oldukları" bilinir. appsettings.json .gitignore'da olmalı.

---

## 4. Yarım kalan / önemli görevler (özet)

**Tamamlanan (bu ve önceki oturumlar):** TEK_PROJE_PLANI.md güncel; kökteki D_*.md kopyaları kaldırıldı (tek nüsha docs/archive’ta); CALISMA_PRENSIPLERI’nda süpervizör ifadesi güncel (Cursor süpervizör, Antigravity referans). Güvenlik: HostService admin config’ten; CoinController catch’te sabit kullanıcı kaldırıldı; MediaController [Authorize]; Program.cs JWT null kontrolü; DesignTimeDbContextFactory yolu sabitlendi; CORS config’e taşındı; API.Tests JWT override; appsettings.Example’da AdminSeed + Cors.

**Açık / opsiyonel:** Antigravity.lnk kökte mi SmileMedical içinde mi (kullanıcı kararı). Periyodik build/test. Eski scriptler (OrganizeQuantaQuokka.ps1 vb.) Scripts’te; istenirse Scripts/archive’a taşınabilir.

Detay: `SmileMedical/docs/YARIM_KALAN_GOREVLER.md`, `SmileMedical/docs/GOREV_LISTESI_ACIKLAMA.md`, `SmileMedical/docs/DERIN_TARAMA_VE_ANALIZ_RAPORU.md`. Plan: "Kalan görevler devam" (YARIM_KALAN_GOREVLER durum güncellemesi + Antigravity.lnk kararı).

---

## 5. Lisans, telif ve subdomain (oturum özeti)

- **Lisans/sahiplik:** Workspace kökü (`QuantaQuokka`) ve `SmileMedical` altında LICENSE, NOTICE, LISANS_VE_SAHIPLIK_PLANI eklendi. Proje/ad alan adları: **QuantaQuokka**, **SmileMedical**, **tradepanic** — telif hakkı sahibine ait olarak belgelendi. Placeholder dolduruldu: telif hakkı sahibi **Cem Ekinci** olarak LICENSE ve NOTICE’ta ayarlandı (isterseniz kendi adınızı/unvanınızı yazabilirsiniz).
- **cPanel subdomain:** Proje isimlerini subdomain olarak almak için rehber: `SmileMedical/docs/CPANEL_SUBDOMAIN_KURULUM.md`. Örnek: smilemedical.moneytransferturkey.com, tradepanic.*, quantaquokka.*. cPanel'e agent bağlanamaz; kullanıcı manuel veya cPanel API script ile ekler.

---

## 6. PDR’ye kurulum (sizin tamamlayacağınız kısım)

- **PDR** = ? (Hangi sistem: Cursor Memory, MemoryGraph, başka bir panel/tool? Adını yazarsanız bu bellek ona göre yapılandırılabilir.)
- Bu dosya (`.cursor/BELLEK.md`) proje içinde olduğu için Cursor okuyup bağlam alabilir; PDR bir dosya okuyucu veya MCP kullanıyorsa bu yolu tanımlayabilirsiniz.

---

*Son güncelleme: 2025-02-20. Oturum özeti: lisans/telif (QuantaQuokka, SmileMedical, tradepanic), cPanel subdomain rehberi, yarım kalan görevler durumu (tamamlananlar ve açık maddeler) belleğe işlendi.*
