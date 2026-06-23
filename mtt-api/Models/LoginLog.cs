namespace MoneyTransfer.API.Models;

public class LoginLog
{
    public int     Id         { get; set; }
    public string  Username   { get; set; } = null!;
    public string  IpAddress  { get; set; } = null!;
    public string? PanelType  { get; set; }
    public bool    Success    { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
