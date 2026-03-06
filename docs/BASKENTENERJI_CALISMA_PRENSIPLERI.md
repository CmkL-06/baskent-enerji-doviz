# baskentenerji.com – Çalışma Prensipleri

**Tarih:** 25.02.2026

---

## 1. Site giriş ekranı ile başlar

- **baskentenerji.com** açıldığında kullanıcı **önce giriş ekranını** görür.
- Ana sayfa (`/`) ve `/login` adresleri **panel köküne** (`/ihtiyar/`) yönlendirilir. Vue uygulaması (ihtiyar) token yoksa **giriş ekranını** gösterir; girişten sonra dashboard vb. sayfalara geçilir.

---

## 2. Yönlendirme özeti

| İstek      | Yönlendirme   | Amaç              |
|-----------|----------------|-------------------|
| `/`       | → `/index.html` → `/ihtiyar/` | Giriş ekranı      |
| `/login`  | → `/ihtiyar/`  | Giriş ekranı      |
| `/ihtiyar/` | SPA yüklenir | Token yoksa giriş, varsa panel |

---

## 3. Dosya konumları

- **Kök index:** `C:\Inetpub\vhosts\baskentenerji.com\httpdocs\index.html` — yönlendirme `/ihtiyar/` olmalı (dashboard değil).
- **URL Rewrite:** `httpdocs\web.config` — kurallar `Root-Redirect-To-Ihtiyar` ve `Root-Login-Redirect` hedefi `/ihtiyar/` olmalı.

---

## 4. Değiştirilmemesi gerekenler

- Ana sayfa veya `/login` hedefi **giriş ekranı** (`/ihtiyar/`) olarak kalmalı; doğrudan `/ihtiyar/dashboard` yapılmamalı ki site giriş ekranı ile başlasın.
