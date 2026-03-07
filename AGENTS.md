# AGENTS.md

## Repository çalışma kuralı (zorunlu)

- Varsayılan geliştirme hedefi `AnasıTAS_Deniz.sln` ve `AnasıTAS_Deniz.*` projeleridir.
- `SmileMedical/` klasörü **arşiv/geçiş içeriği** kabul edilir; kullanıcı açıkça istemedikçe bu klasörde kod değişikliği yapılmaz.
- `AnasıTAS_Deniz.*` projelerine `SmileMedical.*` proje referansı, namespace bağımlılığı veya dosya bağımlılığı eklenmez.
- TODO/fix/feature isteklerinde önce `AnasıTAS_Deniz.*` içinde çözüm aranır.
- `SmileMedical` tarafında yanlışlıkla değişiklik yapılırsa, aynı oturumda geri alınır ve ana çözüm (`AnasıTAS_Deniz.sln`) derlemesi doğrulanır.

## Doğrulama kuralı

- `AnasıTAS_Deniz` tarafında değişiklikten sonra en azından şu doğrulama yapılır:
  - `dotnet build AnasıTAS_Deniz.sln`
- İlgili ise `AnasıTAS_Deniz.*` içinde `SmileMedical` referansı olmadığı kontrol edilir.

