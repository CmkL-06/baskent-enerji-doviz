namespace MoneyTransfer.API.Models;

public class User
{
    public int    Id           { get; set; }
    public string Username     { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Name         { get; set; } = null!;

    /// <summary>admin | operator | dealer</summary>
    public string Role         { get; set; } = "dealer";

    public bool   IsActive     { get; set; } = true;
    public string? TelegramId  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Dealer?   Dealer   { get; set; }
    public Operator? Operator { get; set; }
}
