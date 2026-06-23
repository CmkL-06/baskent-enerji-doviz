namespace MoneyTransfer.API.Models;

public class ErrorLog
{
    public int     Id        { get; set; }
    public string  Type      { get; set; } = "ERROR";
    public string  Message   { get; set; } = null!;
    public string? Endpoint  { get; set; }
    public string? StackTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
