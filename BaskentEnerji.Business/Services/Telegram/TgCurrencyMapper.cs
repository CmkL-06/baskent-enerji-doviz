namespace BaskentEnerji.Business.Services.Telegram
{
    /// <summary>
    /// Telegram bot/TgTransaction tarafında kullanılan para birimi kodlarını
    /// (USDT, RUBLE, RUB) ana sistemin Currency.CurrencyCode alanına eşler.
    /// Tüm Telegram akışlarında (TelegramOperatorController, TelegramDealerController)
    /// bu tek, merkezi eşleme kullanılmalı — aksi halde RUBLE/RUB gibi kodlar
    /// sessizce yanlış (veya hiç) para birimine eşlenebilir.
    /// </summary>
    public static class TgCurrencyMapper
    {
        // RUBLE, bu Telegram akışının ürünü olan Kart Ruble (KRUB)'ye karşılık gelir — normal RUB'dan farklıdır.
        public static string ToSystemCurrencyCode(string? tgCurrency)
        {
            var c = (tgCurrency ?? "").Trim().ToUpperInvariant();
            if (c == "RUBLE" || c == "RUB") return "KRUB";
            return c; // USDT vb. olduğu gibi
        }
    }
}
