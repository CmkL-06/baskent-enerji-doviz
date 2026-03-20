# Öneriler — BASKENT Birleşik Proje

**Tarih:** 18 Mart 2026

---

## 1. Hemen yapılabilecekler

- **Kalan kopyayı temizle:** `Documents\Projeler\baskent-enerji-doviz` hâlâ duruyorsa, Cursor/VS Code ve tüm terminalleri kapatıp bu klasörü **elle çöp kutusuna sürükleyin**. Artık tek kaynak `BASKENT_PROJE_TEKBIRLESIK\ana-repo` ve GitHub ile aynı.
- **Çalışmayı hep buradan yapın:** Yeni geliştirme ve commit’leri **sadece** `BASKENT_PROJE_TEKBIRLESIK\ana-repo` içinde yapın; böylece tek kaynak karışmaz.

---

## 2. GitHub ile çalışma düzeni

- **Güncel kalmak:** Değişiklik yapmadan önce `ana-repo` içinde `git pull origin BASKENT-DOVIZ` çalıştırın.
- **Push sonrası:** Yerel commit’leri mutlaka `git push origin BASKENT-DOVIZ` ile GitHub’a gönderin; böylece “en güçlü yapı” her zaman GitHub’da olur.
- **Branch:** Ana branch `BASKENT-DOVIZ`; yeni özellik için `git checkout -b feature/adi` kullanıp bitince merge edebilirsiniz.

---

## 3. Telegram bot ve hassas veriler

- **`.env`:** `telegram-bot-calistir\.env` GitHub’a **hiç** eklenmemeli. `ana-repo` veya başka repoda `.env` varsa `.gitignore`’da olduğundan emin olun.
- **Token / PAT:** GitHub PAT ve bot token’ları sadece yerel `.env` veya sistem ortam değişkenlerinde kalsın; dokümanlara yazmayın.

---

## 4. Yedekler ve dokümanlar

- **_yedekler:** Arşiv niteliğinde; büyük yer kaplıyorsa uzun vadede harici diske veya buluta taşıyıp `_yedekler` içini sadeleştirebilirsiniz.
- **dokumanlar:** PAT rehberi, raporlar ve TARAMA listesi burada. Yeni ortak dokümanları `dokumanlar` altına ekleyin; böylece her şey tek yerde kalır.

---

## 5. İsteğe bağlı

- **Workspace:** Cursor/VS Code’da kök klasör olarak `BASKENT_PROJE_TEKBIRLESIK` açın; `ana-repo` ve `telegram-bot-calistir` aynı workspace’te olsun.
- **Periyodik kontrol:** Ayda bir `ana-repo` içinde `git fetch origin` ve `git status` ile yerel/GitHub farkını kontrol edin; raporu `RAPOR_Birlesik_GitHub_Karsilastirma.md` gibi güncelleyebilirsiniz.

---

**Özet:** Tek kaynak = `BASKENT_PROJE_TEKBIRLESIK`; en güçlü yapı = GitHub. Çalışmayı buradan yapın, push’u ihmal etmeyin, `.env`’i güvende tutun.

---

## Uygulama durumu (1. madde hariç)

- **Madde 1** — Kalan kopyayı temizle: *atlandı (istenen)*
- **Madde 2** — `ana-repo` içinde `git pull origin BASKENT-DOVIZ` çalıştırıldı; güncel.
- **Madde 3** — `ana-repo\.gitignore` içinde `.env` / `.env.*` zaten var; hassas veriler repoya girmeyecek.
- **Madde 5** — Workspace: `BASKENT_PROJE.code-workspace` eklendi (Cursor/VS Code ile bu dosyayı açın). Periyodik kontrol: `Kontrol_GitHub_Senkron.ps1` eklendi; ayda bir çalıştırın.
