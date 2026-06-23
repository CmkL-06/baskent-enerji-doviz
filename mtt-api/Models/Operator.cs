namespace MoneyTransfer.API.Models;

public class Operator
{
    public int     Id         { get; set; }
    public int     UserId     { get; set; }
    public string  Name       { get; set; } = null!;
    public string? TelegramId { get; set; }
    public bool    IsAdmin    { get; set; } = false;
    public bool    IsActive   { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User              User         { get; set; } = null!;
    public List<Transaction> Transactions { get; set; } = [];
}
