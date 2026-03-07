# Cursor Web'de Bu Proje İçin Kod Geliştirme / Agent Başlatma

Bu projeyi **Cursor Web** (tarayıcı) üzerinden açıp **Agent** ile kod geliştirme alanı olarak kullanmak için adımlar.

---

## 1. Cursor Web'de projeyi açma

1. **https://cursor.com** adresine gidin ve giriş yapın.
2. **"Open Project"** veya **"Clone from GitHub"** seçin.
3. Repo: **`CmkL-06/baskent-enerji-doviz`** (veya kullandığınız GitHub repo adı).
4. Branch: **`BASKENT-DOVIZ`** (veya `main`) seçin.
5. Proje yüklendikten sonra workspace kökü bu reponun içeriği olacaktır.

---

## 2. Agent'ı başlatma (kod geliştirme)

1. Cursor Web arayüzünde **Chat** veya **Agent** panelini açın (genelde yan panel veya Cmd/Ctrl+L).
2. **"New Chat"** veya **"Start Agent"** ile yeni bir oturum başlatın.
3. İlk mesajda proje bağlamını verin, örneğin:

   ```
   Bu proje Başkent Enerji döviz muhasebe sistemi (QuantaQuokka). 
   Backend: AnasıTAS_Deniz .NET 8 API, DB: mtt-moneyexchangeturkey. 
   Sıradaki görev: docs/DURUM_ANALIZI_20260307.md ve CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md'ye göre ilerle.
   ```

4. Agent, `.cursor/rules` ve `AGENTS.md` dosyalarını okuyup projeye göre öneri ve değişiklik yapacaktır.

---

## 3. Proje bağlamı (Agent için kısa özet)

- **Backend:** `AnasıTAS_Deniz.sln` (API, Business, Data, Entity, Tests). Port: 5093.
- **DB:** SQL Server, veritabanı `mtt-moneyexchangeturkey`, şema `mtturkey_exchange`. Bağlantı: `appsettings.json` → `ConnectionStrings:SQL` (repo'da yok).
- **Referanslar:** `README.md`, `AGENTS.md`, `docs/REFERANS_VERITABANI_VE_YAPILANDIRMA.md`, `docs/CALISMA_PLANI_GUVENLIK_VE_DOVIZ_SURUM.md`, `docs/DURUM_ANALIZI_20260307.md`.
- **Sıradaki işler:** Faz 1 güvenlik (1.4.1 [Authorize], 1.1.x sunucu temizliği), Dependabot uyarıları, weeklyProfit / userId TODO'ları.

---

## 4. İlk görev önerisi (Agent'a söyleyebilirsiniz)

- "GitHub Dependabot 2 moderate uyarıyı incele ve güncellemeleri uygula."
- "ExchangeController ve ExchangeAutoRateController'a [Authorize] ekle (Faz 1.4.1)."
- "docs/DURUM_ANALIZI_20260307.md'deki sıradaki adımları uygula."

---

## 5. Yerel ortam (Windows) ile fark

Cursor Web ortamı Linux tabanlı VM olabilir; `AGENTS.md` buna göre yazılmıştır. Windows’ta çalıştırıyorsanız:

- `dotnet` ve `node` yolları farklı olabilir.
- SQL Server için yerelde `localhost\SQLEXPRESS` veya Docker kullanın; connection string `appsettings.json` içinde kalır.

Bu kılavuz Cursor Web’de **agent’ı sizin başlatmanız** içindir; proje kuralları `.cursor/rules` altında agent’a otomatik bağlam sağlar.
