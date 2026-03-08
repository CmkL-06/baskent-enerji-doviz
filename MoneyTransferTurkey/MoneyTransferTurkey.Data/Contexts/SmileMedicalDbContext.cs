using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MoneyTransferTurkey.Entity.Entities.Blog;
using MoneyTransferTurkey.Entity.Entities.Coin;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Currency;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Expense;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using MoneyTransferTurkey.Entity.Entities.Site;
using MoneyTransferTurkey.Entity.Entities.Site.Form;
using MoneyTransferTurkey.Entity.Entities.Site.Menu;
using MoneyTransferTurkey.Entity.Entities.Site.Page;
using MoneyTransferTurkey.Entity.Entities.Site.Slider;
using MoneyTransferTurkey.Entity.Entities.User;
using MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Data.Contexts
{
    public class MoneyTransferTurkeyDbContext : DbContext
    {
        public MoneyTransferTurkeyDbContext(DbContextOptions<MoneyTransferTurkeyDbContext> options) : base(options)
        { }

        public DbSet<BlackList> BlackList { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Blog_Category> Blog_Categories { get; set; }
        public DbSet<Blog_Article> Blog_Articles { get; set; }
        public DbSet<Blog_Article_Visit> Blog_Article_Visits { get; set; }
        public DbSet<Blog_Article_Comment> Blog_Article_Comments { get; set; }
        public DbSet<Blog_Article_Category> Blog_Article_Categories { get; set; }
        public DbSet<Blog_Article_Tag> Blog_Article_Tags { get; set; }
        public DbSet<Blog_Category_Tag> Blog_Category_Tags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Menu_Item> Menu_Items { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Slider_Item> Slider_Items { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<BuilderComponent> BuilderComponents { get; set; }
        public DbSet<GlobalColor> GlobalColors { get; set; }
        public DbSet<Form> Forms { get; set; }
        public DbSet<Form_Submit> Form_Submits { get; set; }
        public DbSet<Coin> Coins { get; set; }
        public DbSet<Coin_Pair> Coin_Pairs { get; set; }
        public DbSet<Coin_User> Coin_Users { get; set; }
        public DbSet<Coin_User_Table> Coin_User_Tables { get; set; }
        public DbSet<Coin_Profit> Coin_Profits { get; set; }
        public DbSet<Coin_User_Favorite> Coin_User_Favorites { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<Site_Settings> Site_Settings { get; set; }
        public DbSet<Analytics> Analytics { get; set; }
        public DbSet<SeoSettings> SeoSettings { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ExchangeRate> ExchangeRates  { get; set; }
        public DbSet<Office> Offices { get; set; }
        public DbSet<Vault> Vaults { get; set; }
        public DbSet<CurrencySale> CurrencySales { get; set; }
        public DbSet<DailySummary> DailySummaries { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }
        public DbSet<VaultBalance> VaultBalances { get; set; }
        public DbSet<VaultBalanceHistory> VaultBalanceHistories { get; set; }
        public DbSet<VaultCount> VaultCounts { get; set; }
        public DbSet<VaultCountDetail> VaultCountDetails { get; set; }
        public DbSet<User_Office> User_Offices { get; set; }
        public DbSet<VaultBalanceSnapshot> VaultBalanceSnapshots { get; set; }
        public DbSet<VaultBalanceSnapshotDetail> VaultBalanceSnapshotDetails { get; set; }

        // Auto Rate entities
        public DbSet<ExchangeSettings> ExchangeSettings { get; set; }
        public DbSet<ExchangeRateHistory> ExchangeRateHistories { get; set; }
        public DbSet<PendingRateApproval> PendingRateApprovals { get; set; }
        public DbSet<ExternalRateCache> ExternalRateCaches { get; set; }
        public DbSet<ExternalDataSource> ExternalDataSources { get; set; }

        // Party Account entities
        public DbSet<Party> Parties { get; set; }
        public DbSet<PartyAccount> PartyAccounts { get; set; }
        public DbSet<PartyAccountEntry> PartyAccountEntries { get; set; }
        public DbSet<PartyCreditLimit> PartyCreditLimits { get; set; }
        public DbSet<PartyContact> PartyContacts { get; set; }
        public DbSet<PartyStatement> PartyStatements { get; set; }
        public DbSet<ActionLog> Logs { get; set; }
        
        // Expense entities
        public DbSet<ExpenseDefinition> ExpenseDefinitions { get; set; }
        public DbSet<ExpensePayment> ExpensePayments { get; set; }
        
        // Ghost Party entities
        public DbSet<GhostPartyAccount> GhostPartyAccounts { get; set; }
        public DbSet<GhostPartyAccountEntry> GhostPartyAccountEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("mtturkey_exchange");
            // Configure DateTime conversion for all entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.Kind == DateTimeKind.Local ? v : v.ToLocalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Local)));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Local ? v : new DateTime?(v.Value.ToLocalTime())) : v,
                                v => v.HasValue ? new DateTime?(DateTime.SpecifyKind(v.Value, DateTimeKind.Local)) : v));
                    }
                }
            }
            modelBuilder.Entity<Office>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OfficeName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.OfficeName).IsUnique();

                entity.HasMany(e => e.Vaults)
                    .WithOne(v => v.Office)
                    .HasForeignKey(v => v.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Vault Configuration
            modelBuilder.Entity<Vault>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.OfficeId, e.Name }).IsUnique();

                entity.HasMany(e => e.Balances)
                    .WithOne(b => b.Vault)
                    .HasForeignKey(b => b.VaultId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Transactions)
                    .WithOne(t => t.Vault)
                    .HasForeignKey(t => t.VaultId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // VaultBalance Configuration
            modelBuilder.Entity<VaultBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.VaultId, e.CurrencyId }).IsUnique();
                entity.Property(e => e.Balance).HasPrecision(18, 6);
                entity.Property(e => e.ReservedAmount).HasPrecision(18, 6);
            });

            modelBuilder.Entity<VaultBalanceHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.VaultId, e.CurrencyId });
                entity.Property(e => e.Balance).HasPrecision(18, 6);
            });

            // VaultBalanceSnapshot Configuration
            modelBuilder.Entity<VaultBalanceSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OfficeId, e.SnapshotDate });
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Details)
                    .WithOne(d => d.Snapshot)
                    .HasForeignKey(d => d.SnapshotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // VaultBalanceSnapshotDetail Configuration
            modelBuilder.Entity<VaultBalanceSnapshotDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SnapshotId, e.VaultId, e.CurrencyId });
                entity.Property(e => e.Balance).HasPrecision(18, 6);
                entity.Property(e => e.ReservedAmount).HasPrecision(18, 6);

                entity.HasOne(e => e.Vault)
                    .WithMany()
                    .HasForeignKey(e => e.VaultId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Currency Configuration
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(4);
                entity.Property(e => e.CurrencyName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.CurrencyCode).IsUnique();

                
            });

            // ExchangeRate Configuration
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BuyRate).HasPrecision(18, 6);
                entity.Property(e => e.SellRate).HasPrecision(18, 6);
               

                entity.HasIndex(e => new { e.OfficeId, e.SourceCurrencyId, e.TargetCurrencyId, e.EffectiveFrom });

                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasOne(e => e.SourceCurrency)
                    .WithMany(c => c.SourceRates)
                    .HasForeignKey(e => e.SourceCurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TargetCurrency)
                    .WithMany(c => c.TargetRates)
                    .HasForeignKey(e => e.TargetCurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Transaction Configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.TransactionNumber).IsUnique();
                entity.HasIndex(e => e.TransactionDate);
                entity.Property(e => e.Profit).HasPrecision(18, 6);
               

                entity.HasMany(e => e.Details)
                    .WithOne(d => d.Transaction)
                    .HasForeignKey(d => d.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Party)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TransactionDetail Configuration
            modelBuilder.Entity<TransactionDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 6);
                entity.Property(e => e.Rate).HasPrecision(18, 6);
                entity.Property(e => e.Commission).HasPrecision(18, 6);
                entity.Property(e => e.NetAmount).HasPrecision(18, 6);
                entity.Property(e => e.ActualBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.ActualSellRate).HasPrecision(18, 6);
                entity.Property(e => e.CustomRate).HasPrecision(18, 6);
            });

         

            // Ghost Party Configuration
            modelBuilder.Entity<GhostPartyAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Balance).HasColumnType("decimal(18,4)");
                entity.Property(e => e.BlockedAmount).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TotalDebits).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TotalCredits).HasColumnType("decimal(18,4)");
                
                entity.HasOne(e => e.Party)
                    .WithMany()
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<GhostPartyAccountEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,4)");
                entity.Property(e => e.RunningBalance).HasColumnType("decimal(18,4)");
                
                entity.HasOne(e => e.GhostAccount)
                    .WithMany(a => a.Entries)
                    .HasForeignKey(e => e.GhostAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Transaction)
                    .WithMany()
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Vault)
                    .WithMany()
                    .HasForeignKey(e => e.VaultId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DailySummary Configuration
            modelBuilder.Entity<DailySummary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.VaultId, e.CurrencyId, e.SummaryDate }).IsUnique();
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 6);
                entity.Property(e => e.TotalIn).HasPrecision(18, 6);
                entity.Property(e => e.TotalOut).HasPrecision(18, 6);
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 6);
                entity.Property(e => e.ProfitLoss).HasPrecision(18, 6);
            });

            modelBuilder.Entity<CurrencySale>(entity =>
            {
                entity.Property(e => e.SaleRate)
                 .HasPrecision(18, 4); // Precision: 18 digits, Scale: 4 decimal places

                entity.Property(e => e.OriginalSaleRate)
                      .HasPrecision(18, 4);

                entity.Property(e => e.Revenue)
                      .HasPrecision(18, 4);

                entity.Property(e => e.RevenuePercent)
                      .HasPrecision(5, 2);

                entity.HasOne(e => e.SourceCurrency)
                 .WithMany()  // Assuming you don't have a reverse navigation on Currency
                 .HasForeignKey(e => e.SourceCurrencyId)
                 .OnDelete(DeleteBehavior.Cascade);  // You can use Cascade or NoAction here

                entity.HasOne(e => e.TargetCurrency)
                      .WithMany()
                      .HasForeignKey(e => e.TargetCurrencyId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevents cascade here

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Vault)
                      .WithMany()
                      .HasForeignKey(e => e.VaultId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevents cascade here
            });

            // Party Configuration
            modelBuilder.Entity<Party>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PartyCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.TaxNumber).HasMaxLength(50);
                entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
                entity.HasIndex(e => new { e.OfficeId, e.PartyCode }).IsUnique();
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.TaxNumber);

                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ModifiedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.TotalVolume).HasPrecision(18, 6);
            });

            // PartyAccount Configuration
            modelBuilder.Entity<PartyAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.AccountNumber).IsUnique();
                entity.HasIndex(e => new { e.PartyId, e.CurrencyId }).IsUnique();
                entity.Property(e => e.Balance).HasPrecision(18, 6);
                entity.Property(e => e.BlockedAmount).HasPrecision(18, 6);
                entity.Property(e => e.TotalDebits).HasPrecision(18, 6);
                entity.Property(e => e.TotalCredits).HasPrecision(18, 6);
                entity.Property(e => e.CreditLimit).HasPrecision(18, 6);

                entity.HasOne(e => e.Party)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PartyAccountEntry Configuration
            modelBuilder.Entity<PartyAccountEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntryNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.EntryNumber).IsUnique();
                entity.HasIndex(e => new { e.PartyAccountId, e.EntryDate });
                entity.HasIndex(e => e.PaymentStatus);
                entity.HasIndex(e => e.DueDate);
                entity.Property(e => e.Amount).HasPrecision(18, 6);
                entity.Property(e => e.RunningBalance).HasPrecision(18, 6);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(100);

                entity.HasOne(e => e.PartyAccount)
                    .WithMany(a => a.Entries)
                    .HasForeignKey(e => e.PartyAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Transaction)
                    .WithMany(t => t.PartyAccountEntries)
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReconciledByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReconciledByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure self-referencing foreign keys without navigation properties
                entity.HasIndex(e => e.ReversalEntryId);
                entity.HasIndex(e => e.PaymentLinkId);
            });

            // PartyCreditLimit Configuration
            modelBuilder.Entity<PartyCreditLimit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PartyId, e.CurrencyId, e.EffectiveFrom });
                entity.Property(e => e.CreditLimit).HasPrecision(18, 6);
                entity.Property(e => e.UtilizedAmount).HasPrecision(18, 6);
                entity.Property(e => e.TemporaryLimit).HasPrecision(18, 6);
                entity.Property(e => e.InterestRate).HasPrecision(5, 2);

                entity.HasOne(e => e.Party)
                    .WithMany(p => p.CreditLimits)
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PartyContact Configuration
            modelBuilder.Entity<PartyContact>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContactName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Mobile).HasMaxLength(50);
                entity.HasIndex(e => new { e.PartyId, e.Email });

                entity.HasOne(e => e.Party)
                    .WithMany(p => p.Contacts)
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PartyStatement Configuration
            modelBuilder.Entity<PartyStatement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StatementNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.StatementNumber).IsUnique();
                entity.HasIndex(e => new { e.PartyId, e.CurrencyId, e.StatementDate });
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 6);
                entity.Property(e => e.TotalDebits).HasPrecision(18, 6);
                entity.Property(e => e.TotalCredits).HasPrecision(18, 6);
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 6);
                entity.Property(e => e.CurrentAmount).HasPrecision(18, 6);
                entity.Property(e => e.Amount30Days).HasPrecision(18, 6);
                entity.Property(e => e.Amount60Days).HasPrecision(18, 6);
                entity.Property(e => e.Amount90Days).HasPrecision(18, 6);
                entity.Property(e => e.AmountOver90Days).HasPrecision(18, 6);

                entity.HasOne(e => e.Party)
                    .WithMany()
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Currency)
                    .WithMany()
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GeneratedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.GeneratedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

     

            modelBuilder.Entity<Coin_User_Table>()
       .HasOne(cut => cut.User)
       .WithMany()
       .HasForeignKey(cut => cut.UserId)
       .OnDelete(DeleteBehavior.NoAction); // or DeleteBehavior.NoAction

           
            modelBuilder.Entity<Coin_User>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.NoAction); // or DeleteBehavior.NoAction
            modelBuilder.Entity<Coin_User_Favorite>()
               .HasOne(cu => cu.User)
               .WithMany()
               .HasForeignKey(cu => cu.UserId)
               .OnDelete(DeleteBehavior.NoAction); // or DeleteBehavior.NoAction



            modelBuilder.Entity<Coin_User>(entity =>
            {
                entity.Property(e => e.BuyPrice).HasPrecision(38, 18);
                entity.Property(e => e.SellPrice).HasPrecision(38, 18);
                entity.Property(e => e.FeeRate).HasPrecision(38, 18);
                entity.Property(e => e.Quantity).HasPrecision(38, 18);
                entity.Property(e => e.EffQuantity).HasPrecision(38, 18);
                entity.Property(e => e.Profit).HasPrecision(38, 18);
            });
            modelBuilder.Entity<Coin_Profit>(entity =>
            {
                entity.Property(e => e.BuyPrice).HasPrecision(38, 18);
                entity.Property(e => e.SellPrice).HasPrecision(38, 18);
                entity.Property(e => e.FeeRate).HasPrecision(38, 18);
                entity.Property(e => e.Quantity).HasPrecision(38, 18);
                entity.Property(e => e.Profit).HasPrecision(38, 18);
                entity.Property(e => e.EffQuantity).HasPrecision(38, 18);
            });

            // Configure decimal precision for Auto Rate entities
            modelBuilder.Entity<ExchangeRateHistory>(entity =>
            {
                entity.Property(e => e.OldBuyRate).HasPrecision(18, 4);
                entity.Property(e => e.OldSellRate).HasPrecision(18, 4);
                entity.Property(e => e.NewBuyRate).HasPrecision(18, 4);
                entity.Property(e => e.NewSellRate).HasPrecision(18, 4);
                entity.Property(e => e.ChangePercent).HasPrecision(5, 2);

                // Fix cascade path issue - use NO ACTION for Currency relationships
                entity.HasOne(e => e.SourceCurrency)
                    .WithMany()
                    .HasForeignKey(e => e.SourceCurrencyId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.TargetCurrency)
                    .WithMany()
                    .HasForeignKey(e => e.TargetCurrencyId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ExchangeSettings>(entity =>
            {
                entity.Property(e => e.TryBasedMarginPercent).HasPrecision(5, 2);
                entity.Property(e => e.CrossFiatMarginPercent).HasPrecision(5, 2);
                entity.Property(e => e.CryptoMarginPercent).HasPrecision(5, 2);
                entity.Property(e => e.MaxPriceChangePercent).HasPrecision(5, 2);
            });

            modelBuilder.Entity<ExternalRateCache>(entity =>
            {
                entity.Property(e => e.BuyRate).HasPrecision(18, 4);
                entity.Property(e => e.SellRate).HasPrecision(18, 4);
                entity.Property(e => e.SpreadPercent).HasPrecision(5, 2);
            });

            modelBuilder.Entity<PendingRateApproval>(entity =>
            {
                entity.Property(e => e.CurrentBuyRate).HasPrecision(18, 4);
                entity.Property(e => e.CurrentSellRate).HasPrecision(18, 4);
                entity.Property(e => e.ProposedBuyRate).HasPrecision(18, 4);
                entity.Property(e => e.ProposedSellRate).HasPrecision(18, 4);
                entity.Property(e => e.ChangePercent).HasPrecision(5, 2);

                // Fix cascade path issue - use NO ACTION instead of CASCADE
                // Specify navigation properties to avoid shadow properties
                entity.HasOne(e => e.SourceCurrency)
                    .WithMany()
                    .HasForeignKey(e => e.SourceCurrencyId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.TargetCurrency)
                    .WithMany()
                    .HasForeignKey(e => e.TargetCurrencyId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Office)
                    .WithMany()
                    .HasForeignKey(e => e.OfficeId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Tag>()
            .HasOne(tag => tag.Language)
            .WithMany()
            .HasForeignKey(tag => tag.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Blog_Article_Category>()
        .HasKey(bac => new { bac.ArticleId, bac.CategoryId });

            modelBuilder.Entity<Blog_Article_Category>()
                .HasOne(bac => bac.Article)
                .WithMany(a => a.Blog_Article_Categories)
                .HasForeignKey(bac => bac.ArticleId);

            modelBuilder.Entity<Blog_Article_Category>()
                .HasOne(bac => bac.Category)
                .WithMany(c => c.Blog_Article_Categories)
                .HasForeignKey(bac => bac.CategoryId);



            modelBuilder.Entity<Blog_Article_Tag>()
                .HasOne(bat => bat.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(bat => bat.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blog_Article_Tag>()
                .HasOne(bat => bat.Article)
                .WithMany(a => a.Blog_Article_Tags)
                .HasForeignKey(bat => bat.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // ----- 

            modelBuilder.Entity<Blog_Category_Tag>()
                .HasOne(bct => bct.Tag)
                .WithMany(c => c.CategoryTags)
                .HasForeignKey(bct => bct.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blog_Category_Tag>()
                .HasOne(bct => bct.Category)
                .WithMany(c => c.CategoryTags)
                .HasForeignKey(bct => bct.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Page>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Slug)
                      .IsRequired()
                      .HasMaxLength(200)
                      .HasDefaultValue(string.Empty);
                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(200)
                      .HasDefaultValue(string.Empty);
                entity.Property(e => e.LanguageCode)
                      .IsRequired()
                      .HasMaxLength(5)
                      .HasDefaultValue("en");
                entity.HasIndex(e => new { e.Slug, e.LanguageCode })
                      .IsUnique()
                      .HasFilter("[Slug] != '' AND [LanguageCode] != ''");
                entity.HasMany(e => e.Components)
                      .WithOne(c => c.Page)
                      .HasForeignKey(c => c.PageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BuilderComponent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PropsJson).HasColumnType("nvarchar(max)");
                entity.Ignore(e => e.Props);
                entity.Ignore(e => e.Styles);
                entity.HasOne(e => e.Parent)
                      .WithMany(p => p.Children)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

        public override int SaveChanges()
        {
            ConvertDateTimesToLocal();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ConvertDateTimesToLocal();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDateTimesToLocal()
        {
            var entities = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entity in entities)
            {
                // Handle common timestamp properties
                var properties = entity.Properties.Where(p => 
                    p.Metadata.Name == "CreatedDate" || 
                    p.Metadata.Name == "ModifiedDate" || 
                    p.Metadata.Name == "UpdatedDate" ||
                    p.Metadata.Name == "TransactionDate" ||
                    p.Metadata.Name == "EffectiveFrom" ||
                    p.Metadata.Name == "EffectiveTo" ||
                    p.Metadata.Name == "EntryDate" ||
                    p.Metadata.Name == "PaymentDate" ||
                    p.Metadata.Name == "DueDate");

                foreach (var property in properties)
                {
                    if (entity.State == EntityState.Added && 
                        (property.Metadata.Name == "CreatedDate" || property.Metadata.Name == "TransactionDate" || property.Metadata.Name == "EntryDate") &&
                        (property.CurrentValue == null || (DateTime)property.CurrentValue == default(DateTime)))
                    {
                        property.CurrentValue = DateTime.Now;
                    }
                    else if (entity.State == EntityState.Modified && 
                        (property.Metadata.Name == "ModifiedDate" || property.Metadata.Name == "UpdatedDate"))
                    {
                        property.CurrentValue = DateTime.Now;
                    }
                }

                // Convert any DateTime values that are still in UTC
                foreach (var property in entity.Properties)
                {
                    if (property.CurrentValue != null)
                    {
                        // Handle DateTime properties
                        if (property.CurrentValue is DateTime dateTimeValue)
                        {
                            // If the DateTime is in UTC, convert to local time
                            if (dateTimeValue.Kind == DateTimeKind.Utc)
                            {
                                property.CurrentValue = dateTimeValue.ToLocalTime();
                            }
                            else if (dateTimeValue.Kind == DateTimeKind.Unspecified)
                            {
                                // Assume unspecified times are local
                                property.CurrentValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Local);
                            }
                        }
                        // Handle DateTime? (nullable) properties
                        else if (property.CurrentValue is DateTime?)
                        {
                            var nullableDateTime = (DateTime?)property.CurrentValue;
                            if (nullableDateTime.HasValue)
                            {
                                var dateTime = nullableDateTime.Value;
                                if (dateTime.Kind == DateTimeKind.Utc)
                                {
                                    property.CurrentValue = dateTime.ToLocalTime();
                                }
                                else if (dateTime.Kind == DateTimeKind.Unspecified)
                                {
                                    property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Optional: Add a method to configure timezone if needed
        private static TimeZoneInfo GetLocalTimeZone()
        {
            // You can customize this to use a specific timezone
            // For Turkey: return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.Local;
        }

    }
}
