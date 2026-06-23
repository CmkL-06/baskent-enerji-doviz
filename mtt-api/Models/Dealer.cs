namespace MoneyTransfer.API.Models;

public class Dealer
{
    public int     Id         { get; set; }
    public int     UserId     { get; set; }
    public string  Name       { get; set; } = null!;
    public decimal Balance    { get; set; } = 0;

    /// <summary>BaskentEnerji entegrasyonu için Vault ID</summary>
    public string? VaultId    { get; set; }

    /// <summary>Telegram bot deep-link kodu</summary>
    public string  DealerCode { get; set; } = null!;

    public string? TelegramId { get; set; }
    public bool    IsActive   { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User              User         { get; set; } = null!;
    public List<Transaction> Transactions { get; set; } = [];
}
