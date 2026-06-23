namespace MoneyTransfer.API.Models;

public class CryptoDeposit
{
    public int     Id        { get; set; }
    public string  Txid      { get; set; } = null!;

    /// <summary>TRC20 | BEP20 | ERC20</summary>
    public string  Network   { get; set; } = null!;

    public decimal Amount    { get; set; }
    public string  Address   { get; set; } = null!;
    public string? Tag       { get; set; }
    public int?    DealerId  { get; set; }

    /// <summary>pending | confirmed | rejected</summary>
    public string  Status    { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Dealer? Dealer { get; set; }
}
