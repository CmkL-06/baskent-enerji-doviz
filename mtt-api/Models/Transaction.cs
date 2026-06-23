namespace MoneyTransfer.API.Models;

public class Transaction
{
    public int     Id           { get; set; }
    public string  CustomerName { get; set; } = null!;
    public decimal Amount       { get; set; }

    /// <summary>USDT | RUB</summary>
    public string  Currency     { get; set; } = "USDT";

    public decimal TlAmount     { get; set; }
    public decimal Rate         { get; set; }

    /// <summary>pending | processing | completed | cancelled</summary>
    public string  Status       { get; set; } = "pending";

    /// <summary>Müşteriye verilen benzersiz işlem kodu</summary>
    public string  Code         { get; set; } = null!;

    public int     DealerId     { get; set; }
    public int?    OperatorId   { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt  { get; set; }

    // Navigation
    public Dealer         Dealer       { get; set; } = null!;
    public Operator?      Operator     { get; set; }
    public List<ChatMessage> Messages  { get; set; } = [];
}
