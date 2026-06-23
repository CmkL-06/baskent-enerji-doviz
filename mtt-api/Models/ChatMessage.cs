namespace MoneyTransfer.API.Models;

public class ChatMessage
{
    public int     Id            { get; set; }
    public int     TransactionId { get; set; }

    /// <summary>customer | operator</summary>
    public string  SenderType   { get; set; } = "operator";

    public string  Message      { get; set; } = null!;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation
    public Transaction Transaction { get; set; } = null!;
}
