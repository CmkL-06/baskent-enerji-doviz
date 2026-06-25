using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Coin;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Entities.Site.Slider;
using BaskentEnerji.Entity.Entities.Site.Menu;
using BaskentEnerji.Entity.Entities.Site.Page;
using BaskentEnerji.Entity.Entities.Site.Form;
using BaskentEnerji.Entity.Entities.Site.Custom;
using System;

namespace BaskentEnerji.Data.Contexts
{
    public class BaskentEnerjiDbContext : DbContext
    {
        public BaskentEnerjiDbContext(DbContextOptions<BaskentEnerjiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // HasDefaultSchema kaldirildi — tablolar dbo schemasi altinda
            // modelBuilder.HasDefaultSchema("mtturkey_exchange");

            // Currency <-> ExchangeRate cok-yonlu iliski
            modelBuilder.Entity<ExchangeRate>()
                .HasOne(e => e.SourceCurrency)
                .WithMany(c => c.SourceRates)
                .HasForeignKey(e => e.SourceCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExchangeRate>()
                .HasOne(e => e.TargetCurrency)
                .WithMany(c => c.TargetRates)
                .HasForeignKey(e => e.TargetCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Party <-> User (CreatedBy / ModifiedBy) FK esleme
            modelBuilder.Entity<Party>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Party>()
                .HasOne(p => p.ModifiedBy)
                .WithMany()
                .HasForeignKey(p => p.ModifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Office self-referential hiyerarşi
            modelBuilder.Entity<Office>()
                .HasOne(o => o.ParentOffice)
                .WithMany(o => o.ChildOffices)
                .HasForeignKey(o => o.ParentOfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            // OfficeTransfer — kaynak ve hedef vault FK çakışmasını önle
            modelBuilder.Entity<OfficeTransfer>()
                .HasOne(t => t.SourceVault)
                .WithMany()
                .HasForeignKey(t => t.SourceVaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfficeTransfer>()
                .HasOne(t => t.TargetVault)
                .WithMany()
                .HasForeignKey(t => t.TargetVaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfficeTransfer>()
                .HasOne(t => t.RequestedBy)
                .WithMany()
                .HasForeignKey(t => t.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfficeTransfer>()
                .HasOne(t => t.ApprovedBy)
                .WithMany()
                .HasForeignKey(t => t.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        // User
        public DbSet<User> Users { get; set; } = null!;

        // Blog
        public DbSet<Blog_Article> Blog_Articles { get; set; } = null!;
        public DbSet<Blog_Article_Category> Blog_Article_Categories { get; set; } = null!;
        public DbSet<Blog_Article_Comment> Blog_Article_Comments { get; set; } = null!;
        public DbSet<Blog_Article_Tag> Blog_Article_Tags { get; set; } = null!;
        public DbSet<Blog_Article_Visit> Blog_Article_Visits { get; set; } = null!;
        public DbSet<Blog_Category> Blog_Categories { get; set; } = null!;
        public DbSet<Blog_Category_Tag> Blog_Category_Tags { get; set; } = null!;

        // Coin
        public DbSet<Coin> Coins { get; set; } = null!;
        public DbSet<Coin_Pair> Coin_Pairs { get; set; } = null!;
        public DbSet<Coin_Profit> Coin_Profits { get; set; } = null!;
        public DbSet<Coin_User> Coin_Users { get; set; } = null!;
        public DbSet<Coin_User_Favorite> Coin_User_Favorites { get; set; } = null!;
        public DbSet<Coin_User_Table> Coin_User_Tables { get; set; } = null!;

        // ExchangeOffice — Currency
        public DbSet<Currency> Currencies { get; set; } = null!;
        public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;
        public DbSet<ExchangeRateHistory> ExchangeRateHistories { get; set; } = null!;
        public DbSet<ExchangeSettings> ExchangeSettings { get; set; } = null!;
        public DbSet<ExternalDataSource> ExternalDataSources { get; set; } = null!;
        public DbSet<ExternalRateCache> ExternalRateCaches { get; set; } = null!;
        public DbSet<PendingRateApproval> PendingRateApprovals { get; set; } = null!;

        // ExchangeOffice — Office
        public DbSet<Office> Offices { get; set; } = null!;
        public DbSet<User_Office> User_Offices { get; set; } = null!;
        public DbSet<OfficeTransfer> OfficeTransfers { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<TransactionDetail> TransactionDetails { get; set; } = null!;
        public DbSet<CurrencySale> CurrencySales { get; set; } = null!;
        public DbSet<DailySummary> DailySummaries { get; set; } = null!;
        public DbSet<Vault> Vaults { get; set; } = null!;
        public DbSet<VaultBalance> VaultBalances { get; set; } = null!;
        public DbSet<VaultBalanceHistory> VaultBalanceHistories { get; set; } = null!;
        public DbSet<VaultBalanceSnapshot> VaultBalanceSnapshots { get; set; } = null!;
        public DbSet<VaultBalanceSnapshotDetail> VaultBalanceSnapshotDetails { get; set; } = null!;
        public DbSet<VaultCount> VaultCounts { get; set; } = null!;

        // ExchangeOffice — Party
        public DbSet<Party> Parties { get; set; } = null!;
        public DbSet<PartyAccount> PartyAccounts { get; set; } = null!;
        public DbSet<PartyAccountEntry> PartyAccountEntries { get; set; } = null!;
        public DbSet<PartyContact> PartyContacts { get; set; } = null!;
        public DbSet<PartyCreditLimit> PartyCreditLimits { get; set; } = null!;
        public DbSet<PartyStatement> PartyStatements { get; set; } = null!;
        public DbSet<GhostPartyAccount> GhostPartyAccounts { get; set; } = null!;
        public DbSet<GhostPartyAccountEntry> GhostPartyAccountEntries { get; set; } = null!;

        // ExchangeOffice — Expense
        public DbSet<ExpenseDefinition> ExpenseDefinitions { get; set; } = null!;
        public DbSet<ExpensePayment> ExpensePayments { get; set; } = null!;

        // Site — Genel
        public DbSet<Analytics> Analytics { get; set; } = null!;
        public DbSet<BlackList> BlackList { get; set; } = null!;
        public DbSet<GlobalColor> GlobalColors { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;
        public DbSet<MediaFile> MediaFiles { get; set; } = null!;
        public DbSet<SeoSettings> SeoSettings { get; set; } = null!;
        public DbSet<Site_Settings> Site_Settings { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Theme> Themes { get; set; } = null!;

        // Site — Slider
        public DbSet<Slider> Sliders { get; set; } = null!;
        public DbSet<Slider_Item> Slider_Items { get; set; } = null!;

        // Site — Menu
        public DbSet<Menu> Menus { get; set; } = null!;
        public DbSet<Menu_Item> Menu_Items { get; set; } = null!;

        // Site — Page
        public DbSet<Page> Pages { get; set; } = null!;
        public DbSet<BuilderComponent> BuilderComponents { get; set; } = null!;

        // Site — Form
        public DbSet<Form> Forms { get; set; } = null!;
        public DbSet<Form_Submit> Form_Submits { get; set; } = null!;

        // Site — Custom
        public DbSet<Department> Departments { get; set; } = null!;
    }
}
