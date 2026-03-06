# SmileMedical Frontend

Vue 3 + Vite ile SmileMedical API için admin arayüzü.

## Gereksinimler

- Node.js 18+
- SmileMedical.API çalışır durumda (opsiyonel; API yoksa "Demo giriş" ile layout görüntülenir)

## Kurulum

```bash
cd frontend
npm install
```

## Çalıştırma

```bash
npm run dev
```

Tarayıcıda http://localhost:5173 açılır.

- **Gerçek giriş:** Backend’i aynı makinede çalıştırın (varsayılan port 5000). Vite proxy `/api` isteklerini backend’e yönlendirir.
- **Demo giriş:** API çalışmıyorsa login sayfasında "Demo giriş (API yok)" ile panele girebilirsiniz.

## API base URL

- `.env` dosyası oluşturup (`.env.example` kopyalayın) `VITE_API_BASE_URL=http://localhost:5000` yazarsanız tüm istekler doğrudan bu adrese gider.
- Boş bırakırsanız istekler aynı origin’e (localhost:5173) gider ve Vite proxy ile backend’e iletilir (`vite.config.js` içinde `/api` → `http://localhost:5000`).

## Build

```bash
npm run build
npm run preview   # build çıktısını önizleme
```

## Testler

- **Birim testler (Vitest):** `npm run test` veya `npm run test:run`
  - `src/composables/useAuth.spec.js` — auth state, setAuth, logout
  - `src/api/client.spec.js` — istek interceptor (token), 401’de token temizleme
- **E2E (Playwright):** `npm run test:e2e` (uygulama otomatik başlatılır)
  - `e2e/login.spec.js` — login sayfası, demo giriş, yönlendirme, çıkış
- Tarayıcılı E2E UI: `npm run test:e2e:ui`

## Sayfalar

| Yol      | Açıklama        | API (özet)           |
|----------|------------------|----------------------|
| /login   | Giriş            | POST User/login      |
| /        | Dashboard        | Exchange/offices vb. |
| /user    | Kullanıcı        | User/*               |
| /exchange| Döviz / Ofis     | Exchange             |
| /vault   | Kasa             | VaultSnapshot        |
| /party   | Taraflar         | Party, PartyAccount  |
| /rates   | Kur / Auto Rate  | ExchangeAutoRate     |
| /site    | Site içerik      | Page, Menu, Theme    |
| /blog    | Blog             | Blog                 |
| /coin    | Coin             | Coin, SignalR       |
