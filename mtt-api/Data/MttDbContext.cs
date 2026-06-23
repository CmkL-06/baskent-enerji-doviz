using Microsoft.EntityFrameworkCore;
using MoneyTransfer.API.Models;

namespace MoneyTransfer.API.Data;

public class MttDbContext(DbContextOptions<MttDbContext> options) : DbContext(options)
{
    public DbSet<User>          Users          { get; set; }
    public DbSet<Dealer>        Dealers        { get; set; }
    public DbSet<Operator>      Operators      { get; set; }
    public DbSet<Transaction>   Transactions   { get; set; }
    public DbSet<ChatMessage>   ChatMessages   { get; set; }
    public DbSet<ExchangeRate>  ExchangeRates  { get; set; }
    public DbSet<CryptoDeposit> CryptoDeposits { get; set; }
    public DbSet<LoginLog>      LoginLogs      { get; set; }
    public DbSet<ErrorLog>      ErrorLogs      { get; set; }

    protected override void OnModelCreating(ModelBuilder m)
    {
        // User → Dealer (1:1)
        m.Entity<Dealer>()
            .HasOne(d => d.User)
            .WithOne(u => u.Dealer)
            .HasForeignKey<Dealer>(d => d.UserId);

        // User → Operator (1:1)
        m.Entity<Operator>()
            .HasOne(o => o.User)
            .WithOne(u => u.Operator)
            .HasForeignKey<Operator>(o => o.UserId);

        // Transaction → Dealer
        m.Entity<Transaction>()
            .HasOne(t => t.Dealer)
            .WithMany(d => d.Transactions)
            .HasForeignKey(t => t.DealerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction → Operator (nullable)
        m.Entity<Transaction>()
            .HasOne(t => t.Operator)
            .WithMany(o => o.Transactions)
            .HasForeignKey(t => t.OperatorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ChatMessage → Transaction
        m.Entity<ChatMessage>()
            .HasOne(c => c.Transaction)
            .WithMany(t => t.Messages)
            .HasForeignKey(c => c.TransactionId);

        // CryptoDeposit → Dealer (nullable)
        m.Entity<CryptoDeposit>()
            .HasOne(c => c.Dealer)
            .WithMany()
            .HasForeignKey(c => c.DealerId)
            .IsRequired(false);

        // Decimal precision
        m.Entity<Dealer>()
            .Property(d => d.Balance).HasPrecision(18, 4);
        m.Entity<Transaction>()
            .Property(t => t.Amount).HasPrecision(18, 8);
        m.Entity<Transaction>()
            .Property(t => t.TlAmount).HasPrecision(18, 4);
        m.Entity<Transaction>()
            .Property(t => t.Rate).HasPrecision(18, 6);
        m.Entity<ExchangeRate>()
            .Property(e => e.BuyRate).HasPrecision(18, 6);
        m.Entity<ExchangeRate>()
            .Property(e => e.SellRate).HasPrecision(18, 6);
        m.Entity<CryptoDeposit>()
            .Property(c => c.Amount).HasPrecision(18, 8);

        // Unique kısıtlamalar
        m.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        m.Entity<Dealer>()
            .HasIndex(d => d.DealerCode).IsUnique();
        m.Entity<ExchangeRate>()
            .HasIndex(e => e.Currency).IsUnique();
        m.Entity<CryptoDeposit>()
            .HasIndex(c => c.Txid).IsUnique();

        // Seed: varsayılan kurlar
        m.Entity<ExchangeRate>().HasData(
            new ExchangeRate { Id = 1, Currency = "USDT", BuyRate = 32.50m, SellRate = 33.00m, UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new ExchangeRate { Id = 2, Currency = "RUB",  BuyRate = 0.350m, SellRate = 0.370m, UpdatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) }
        );
    }
}
