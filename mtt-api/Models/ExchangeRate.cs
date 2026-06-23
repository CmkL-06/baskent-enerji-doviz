namespace MoneyTransfer.API.Models;

public class ExchangeRate
{
    public int     Id        { get; set; }

    /// <summary>USDT | RUB</summary>
    public string  Currency  { get; set; } = null!;

    public decimal BuyRate   { get; set; }
    public decimal SellRate  { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
