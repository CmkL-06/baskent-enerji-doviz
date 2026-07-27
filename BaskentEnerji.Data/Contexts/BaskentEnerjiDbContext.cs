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
using BaskentEnerji.Entity.Entities.Telegram;
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
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.ToTable(tb => tb.HasTrigger("trg_SyncTgExchangeRates"));
                entity.HasOne(e => e.SourceCurrency)
                    .WithMany(c => c.SourceRates)
                    .HasForeignKey(e => e.SourceCurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.TargetCurrency)
                    .WithMany(c => c.TargetRates)
                    .HasForeignKey(e => e.TargetCurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

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

            // DayClosure FK
            modelBuilder.Entity<DayClosure>()
                .HasOne(d => d.ClosedByUser)
                .WithMany()
                .HasForeignKey(d => d.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DayClosure>()
                .HasOne(d => d.Vault)
                .WithMany()
                .HasForeignKey(d => d.VaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DayClosure>()
                .HasOne(d => d.Office)
                .WithMany()
                .HasForeignKey(d => d.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DayClosure>()
                .HasOne(d => d.VaultCount)
                .WithMany()
                .HasForeignKey(d => d.VaultCountId)
                .OnDelete(DeleteBehavior.NoAction);

            // DayClosureDetail FK
            modelBuilder.Entity<DayClosureDetail>()
                .HasOne(dd => dd.Currency)
                .WithMany()
                .HasForeignKey(dd => dd.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // CurrencyWac FK
            modelBuilder.Entity<CurrencyWac>()
                .HasOne(w => w.Vault)
                .WithMany()
                .HasForeignKey(w => w.VaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyWac>()
                .HasOne(w => w.Currency)
                .WithMany()
                .HasForeignKey(w => w.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // CurrencyWacHistory FK
            modelBuilder.Entity<CurrencyWacHistory>()
                .HasOne(h => h.Vault)
                .WithMany()
                .HasForeignKey(h => h.VaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyWacHistory>()
                .HasOne(h => h.Currency)
                .WithMany()
                .HasForeignKey(h => h.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyWacHistory>()
                .HasOne(h => h.Transaction)
                .WithMany()
                .HasForeignKey(h => h.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            // OfficeAlert FK
            modelBuilder.Entity<OfficeAlert>()
                .HasOne(a => a.Office)
                .WithMany()
                .HasForeignKey(a => a.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Denetim raporu sertleştirmesi: Transaction.User ve TransactionDetail.Currency FK'ları
            // convention gereği Cascade'e düşüyordu — bir kullanıcı veya para birimi silinirse tüm
            // işlem geçmişi (finansal kayıtlar) da silinirdi. Restrict'e çekildi.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionDetail>()
                .HasOne(d => d.Currency)
                .WithMany()
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Transaction.Vault: canlı veritabanında zaten NO_ACTION olarak duruyordu (migration
            // geçmişi Cascade diyordu — model/canlı sapması). Model burada canlı gerçeğe eşitleniyor.
            // WithMany(v => v.Transactions) — Vault.Transactions mevcut ters navigasyonla eşleşmeli,
            // aksi halde EF bunu ayrı bir ilişki sanıp gölge VaultId1 kolonu oluşturur.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Vault)
                .WithMany(v => v.Transactions)
                .HasForeignKey(t => t.VaultId)
                .OnDelete(DeleteBehavior.Restrict);

            // Telegram MTT FK'lar
            modelBuilder.Entity<TgTransaction>()
                .HasOne(t => t.Customer)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TgMessage>()
                .HasOne(m => m.Transaction)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TgChatSession>()
                .HasOne(s => s.Transaction)
                .WithMany(t => t.ChatSessions)
                .HasForeignKey(s => s.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TgCryptoDeposit>()
                .HasOne(d => d.Transaction)
                .WithMany(t => t.CryptoDeposits)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            // ============================================================
            // DECIMAL PRECISION — Rate fields: (18,6), Amount fields: (18,4)
            // ============================================================

            // --- ExchangeRate ---
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.Property(e => e.BuyRate).HasPrecision(18, 6);
                entity.Property(e => e.SellRate).HasPrecision(18, 6);
            });

            // --- ExchangeRateHistory ---
            modelBuilder.Entity<ExchangeRateHistory>(entity =>
            {
                entity.Property(e => e.OldBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.OldSellRate).HasPrecision(18, 6);
                entity.Property(e => e.NewBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.NewSellRate).HasPrecision(18, 6);
                entity.Property(e => e.ChangePercent).HasPrecision(18, 6);
            });

            // --- ExchangeSettings ---
            modelBuilder.Entity<ExchangeSettings>(entity =>
            {
                entity.Property(e => e.TryBasedMarginPercent).HasPrecision(18, 6);
                entity.Property(e => e.CrossFiatMarginPercent).HasPrecision(18, 6);
                entity.Property(e => e.CryptoMarginPercent).HasPrecision(18, 6);
                entity.Property(e => e.MaxPriceChangePercent).HasPrecision(18, 6);
            });

            // --- ExternalRateCache ---
            modelBuilder.Entity<ExternalRateCache>(entity =>
            {
                entity.Property(e => e.BuyRate).HasPrecision(18, 6);
                entity.Property(e => e.SellRate).HasPrecision(18, 6);
                entity.Property(e => e.SpreadPercent).HasPrecision(18, 6);
            });

            // --- PendingRateApproval ---
            modelBuilder.Entity<PendingRateApproval>(entity =>
            {
                entity.Property(e => e.CurrentBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.CurrentSellRate).HasPrecision(18, 6);
                entity.Property(e => e.ProposedBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.ProposedSellRate).HasPrecision(18, 6);
                entity.Property(e => e.ChangePercent).HasPrecision(18, 6);
            });

            // --- TransactionDetail ---
            modelBuilder.Entity<TransactionDetail>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);
                entity.Property(e => e.Rate).HasPrecision(18, 6);
                entity.Property(e => e.Commission).HasPrecision(18, 4);
                entity.Property(e => e.NetAmount).HasPrecision(18, 4);
                entity.Property(e => e.ActualBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.ActualSellRate).HasPrecision(18, 6);
                entity.Property(e => e.CustomRate).HasPrecision(18, 6);
            });

            // --- Transaction ---
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Profit).HasPrecision(18, 4);
            });

            // --- CurrencySale ---
            modelBuilder.Entity<CurrencySale>(entity =>
            {
                entity.Property(e => e.SaleRate).HasPrecision(18, 6);
                entity.Property(e => e.OriginalSaleRate).HasPrecision(18, 6);
                entity.Property(e => e.Revenue).HasPrecision(18, 4);
                entity.Property(e => e.RevenuePercent).HasPrecision(18, 6);
            });

            // --- DailySummary ---
            modelBuilder.Entity<DailySummary>(entity =>
            {
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 4);
                entity.Property(e => e.TotalIn).HasPrecision(18, 4);
                entity.Property(e => e.TotalOut).HasPrecision(18, 4);
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 4);
                entity.Property(e => e.ProfitLoss).HasPrecision(18, 4);
            });

            // --- CurrencyWac ---
            modelBuilder.Entity<CurrencyWac>(entity =>
            {
                entity.Property(e => e.Wac).HasPrecision(18, 6);
                entity.Property(e => e.Quantity).HasPrecision(18, 4);
                // Denetim raporu düzeltmesi: (VaultId, CurrencyId) üzerinde unique kısıt olmadığı için
                // eşzamanlı ilk-alış işlemleri iki ayrı satır oluşturabiliyordu (WacService.
                // GetOrCreateWacAsync'teki UPDLOCK, henüz var olmayan bir satırı koruyamaz). Bu kısıt,
                // veritabanı seviyesinde ikinci eşzamanlı INSERT'i reddederek WacService'teki retry
                // mantığının devreye girmesini sağlar.
                entity.HasIndex(e => new { e.VaultId, e.CurrencyId }).IsUnique();
            });

            // --- CurrencyWacHistory ---
            modelBuilder.Entity<CurrencyWacHistory>(entity =>
            {
                entity.Property(e => e.OldWac).HasPrecision(18, 6);
                entity.Property(e => e.NewWac).HasPrecision(18, 6);
                entity.Property(e => e.OldQuantity).HasPrecision(18, 4);
                entity.Property(e => e.NewQuantity).HasPrecision(18, 4);
                entity.Property(e => e.TransactionAmount).HasPrecision(18, 4);
                entity.Property(e => e.TransactionRate).HasPrecision(18, 6);
            });

            // --- Office ---
            modelBuilder.Entity<Office>(entity =>
            {
                entity.Property(e => e.DailyTransactionLimit).HasPrecision(18, 4);
                entity.Property(e => e.MonthlyTransactionLimit).HasPrecision(18, 4);
                entity.Property(e => e.CommissionRate).HasPrecision(18, 6);
                entity.Property(e => e.TransferApprovalThreshold).HasPrecision(18, 4);
            });

            // --- OfficeTransfer ---
            modelBuilder.Entity<OfficeTransfer>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);
            });

            // --- Vault ---
            modelBuilder.Entity<Vault>(entity =>
            {
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 4);
            });

            // --- VaultBalance ---
            modelBuilder.Entity<VaultBalance>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
                entity.Property(e => e.ReservedAmount).HasPrecision(18, 4);
            });


            // --- VaultBalanceHistory ---
            modelBuilder.Entity<VaultBalanceHistory>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
            });

            // --- VaultBalanceSnapshotDetail ---
            modelBuilder.Entity<VaultBalanceSnapshotDetail>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
                entity.Property(e => e.ReservedAmount).HasPrecision(18, 4);
            });

            // --- VaultCountDetail ---
            modelBuilder.Entity<VaultCountDetail>(entity =>
            {
                entity.Property(e => e.ActualAmount).HasPrecision(18, 4);
                entity.Property(e => e.SystemAmount).HasPrecision(18, 4);
                entity.Property(e => e.Discrepancy).HasPrecision(18, 4);
            });

            // --- DayClosure ---
            modelBuilder.Entity<DayClosure>(entity =>
            {
                entity.Property(e => e.TotalRealizedProfit).HasPrecision(18, 4);
            });

            // --- DayClosureDetail ---
            modelBuilder.Entity<DayClosureDetail>(entity =>
            {
                entity.Property(e => e.SystemBalance).HasPrecision(18, 4);
                entity.Property(e => e.PhysicalCount).HasPrecision(18, 4);
                entity.Property(e => e.Discrepancy).HasPrecision(18, 4);
                entity.Property(e => e.WacAtClose).HasPrecision(18, 6);
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 4);
                entity.Property(e => e.OpeningWac).HasPrecision(18, 6);
            });

            // --- Party ---
            modelBuilder.Entity<Party>(entity =>
            {
                entity.Property(e => e.TotalVolume).HasPrecision(18, 4);
            });

            // --- PartyAccount ---
            modelBuilder.Entity<PartyAccount>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
                entity.Property(e => e.BlockedAmount).HasPrecision(18, 4);
                entity.Property(e => e.CreditLimit).HasPrecision(18, 4);
                entity.Property(e => e.TotalDebits).HasPrecision(18, 4);
                entity.Property(e => e.TotalCredits).HasPrecision(18, 4);
            });

            // --- PartyAccountEntry ---
            modelBuilder.Entity<PartyAccountEntry>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);
                entity.Property(e => e.PaidAmount).HasPrecision(18, 4);
                entity.Property(e => e.RunningBalance).HasPrecision(18, 4);
                entity.Property(e => e.OriginalAmount).HasPrecision(18, 4);
                entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);

                // Ayni islem (ReferenceNumber = TransactionId) icin ayni cari hesaba iki kez
                // kayit dusulmesini DB seviyesinde engeller (Telegram bot tarafinin retry/kuyruk
                // mekanizmasi icin idempotency guvence altina alinir; uygulama seviyesinde zaten
                // bir AnyAsync kontrolu var -- TelegramDealerController.RecordEntry -- ama gercek
                // eszamanlilik altinda tek basina yeterli degildi).
                entity.HasIndex(e => new { e.PartyAccountId, e.ReferenceNumber })
                    .IsUnique()
                    .HasFilter("[ReferenceNumber] IS NOT NULL AND [IsReversed] = 0")
                    .HasDatabaseName("IX_PartyAccountEntries_PartyAccountId_ReferenceNumber");
            });

            // --- PartyCreditLimit ---
            modelBuilder.Entity<PartyCreditLimit>(entity =>
            {
                entity.Property(e => e.CreditLimit).HasPrecision(18, 4);
                entity.Property(e => e.UtilizedAmount).HasPrecision(18, 4);
                entity.Property(e => e.TemporaryLimit).HasPrecision(18, 4);
                entity.Property(e => e.InterestRate).HasPrecision(18, 6);
            });

            // --- PartyStatement ---
            modelBuilder.Entity<PartyStatement>(entity =>
            {
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 4);
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 4);
                entity.Property(e => e.TotalDebits).HasPrecision(18, 4);
                entity.Property(e => e.TotalCredits).HasPrecision(18, 4);
                entity.Property(e => e.CurrentAmount).HasPrecision(18, 4);
                entity.Property(e => e.Amount30Days).HasPrecision(18, 4);
                entity.Property(e => e.Amount60Days).HasPrecision(18, 4);
                entity.Property(e => e.Amount90Days).HasPrecision(18, 4);
                entity.Property(e => e.AmountOver90Days).HasPrecision(18, 4);
            });

            // --- GhostPartyAccount ---
            modelBuilder.Entity<GhostPartyAccount>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
                entity.Property(e => e.BlockedAmount).HasPrecision(18, 4);
                entity.Property(e => e.TotalDebits).HasPrecision(18, 4);
                entity.Property(e => e.TotalCredits).HasPrecision(18, 4);
            });

            // --- GhostPartyAccountEntry ---
            modelBuilder.Entity<GhostPartyAccountEntry>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);
                entity.Property(e => e.RunningBalance).HasPrecision(18, 4);
            });

            // --- ExpenseCategoryDefinition ---
            // Currency ile aynı desen (global, DB-backed, kullanıcı yönetimli) — Category silinmesi
            // ExpenseDefinition/ExpenseBudget'ta öksüz kayıt bırakmasın diye Restrict.
            modelBuilder.Entity<ExpenseCategoryDefinition>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // --- ExpenseDefinition ---
            modelBuilder.Entity<ExpenseDefinition>(entity =>
            {
                entity.Property(e => e.DefaultAmount).HasPrecision(18, 4);
                entity.Property(e => e.AccountReference).HasMaxLength(100);
                entity.HasIndex(e => new { e.OfficeId, e.Code }).IsUnique();

                entity.HasOne(ed => ed.Category)
                    .WithMany(c => c.ExpenseDefinitions)
                    .HasForeignKey(ed => ed.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --- ExpensePayment ---
            modelBuilder.Entity<ExpensePayment>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);

                // Bir ExpenseDefinition veya Vault silinirse ExpensePayments'ın Cascade ile
                // otomatik silinmesini engelle — finansal denetim izi kaybolmasın.
                entity.HasOne(ep => ep.ExpenseDefinition)
                    .WithMany(ed => ed.Payments)
                    .HasForeignKey(ep => ep.ExpenseDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ep => ep.Vault)
                    .WithMany()
                    .HasForeignKey(ep => ep.VaultId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --- ExpenseBudget ---
            modelBuilder.Entity<ExpenseBudget>(entity =>
            {
                entity.Property(e => e.BudgetAmount).HasPrecision(18, 4);
                entity.HasIndex(e => new { e.OfficeId, e.CategoryId, e.Year, e.Month }).IsUnique();

                entity.HasOne(eb => eb.Category)
                    .WithMany(c => c.ExpenseBudgets)
                    .HasForeignKey(eb => eb.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --- Coin_User ---
            modelBuilder.Entity<Coin_User>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 4);
                entity.Property(e => e.EffQuantity).HasPrecision(18, 4);
                entity.Property(e => e.BuyPrice).HasPrecision(18, 6);
                entity.Property(e => e.SellPrice).HasPrecision(18, 6);
                entity.Property(e => e.FeeRate).HasPrecision(18, 6);
                entity.Property(e => e.Profit).HasPrecision(18, 4);
            });

            // --- Coin_Profit ---
            modelBuilder.Entity<Coin_Profit>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 4);
                entity.Property(e => e.EffQuantity).HasPrecision(18, 4);
                entity.Property(e => e.BuyPrice).HasPrecision(18, 6);
                entity.Property(e => e.SellPrice).HasPrecision(18, 6);
                entity.Property(e => e.FeeRate).HasPrecision(18, 6);
                entity.Property(e => e.Profit).HasPrecision(18, 4);
            });

            // --- TgTransaction ---
            modelBuilder.Entity<TgTransaction>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 4);
                entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
                entity.Property(e => e.TryAmount).HasPrecision(18, 4);
            });

            // --- TgExchangeRate ---
            modelBuilder.Entity<TgExchangeRate>(entity =>
            {
                entity.Property(e => e.BuyRate).HasPrecision(18, 6);
                entity.Property(e => e.SellRate).HasPrecision(18, 6);
            });

            // --- TgDealer ---
            modelBuilder.Entity<TgDealer>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 4);
                entity.Property(e => e.CommissionRate).HasPrecision(18, 6);
            });

            // --- TgDealerRate ---
            modelBuilder.Entity<TgDealerRate>(entity =>
            {
                entity.Property(e => e.BuyRate).HasPrecision(18, 6);
                entity.Property(e => e.SellRate).HasPrecision(18, 6);
                entity.HasIndex(e => new { e.DealerId, e.Currency }).IsUnique();
            });

            // --- TgDealerRateHistory ---
            modelBuilder.Entity<TgDealerRateHistory>(entity =>
            {
                entity.Property(e => e.OldBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.OldSellRate).HasPrecision(18, 6);
                entity.Property(e => e.NewBuyRate).HasPrecision(18, 6);
                entity.Property(e => e.NewSellRate).HasPrecision(18, 6);
                entity.HasIndex(e => new { e.DealerId, e.Currency, e.ChangedAt });
            });

            // --- TgCryptoDeposit ---
            modelBuilder.Entity<TgCryptoDeposit>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 6);
            });

            // ============================================================
            // PERFORMANCE INDEXES
            // ============================================================

            // Transaction: vault + date + soft-delete filter
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new { t.VaultId, t.TransactionDate, t.IsDeleted })
                .HasDatabaseName("IX_Transaction_Vault_Date_Deleted");

            // Transaction: vault + date (without soft-delete)
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new { t.VaultId, t.TransactionDate })
                .HasDatabaseName("IX_Transaction_Vault_Date");

            // Transaction: number lookup
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TransactionNumber)
                .HasDatabaseName("IX_Transaction_Number");

            // VaultBalanceHistory: vault + currency + date range queries
            modelBuilder.Entity<VaultBalanceHistory>()
                .HasIndex(v => new { v.VaultId, v.CurrencyId, v.CreatedDate })
                .HasDatabaseName("IX_VaultBalanceHistory_Vault_Currency_Date");

            // VaultBalance: unique constraint per vault-currency pair
            modelBuilder.Entity<VaultBalance>()
                .HasIndex(v => new { v.VaultId, v.CurrencyId })
                .IsUnique()
                .HasDatabaseName("IX_VaultBalance_Vault_Currency_Unique");
        }

        // User
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; } = null!;
        public DbSet<UserActivityHistory> UserActivityHistories { get; set; } = null!;

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
        public DbSet<CurrencyWac> CurrencyWacs { get; set; } = null!;
        public DbSet<CurrencyWacHistory> CurrencyWacHistories { get; set; } = null!;
        public DbSet<DayClosure> DayClosures { get; set; } = null!;
        public DbSet<DayClosureDetail> DayClosureDetails { get; set; } = null!;
        public DbSet<OfficeAlert> OfficeAlerts { get; set; } = null!;

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
        public DbSet<ExpenseBudget> ExpenseBudgets { get; set; } = null!;
        public DbSet<ExpenseCategoryDefinition> ExpenseCategories { get; set; } = null!;

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

        // Telegram MTT
        public DbSet<TgCustomer> TgCustomers { get; set; } = null!;
        public DbSet<TgTransaction> TgTransactions { get; set; } = null!;
        public DbSet<TgMessage> TgMessages { get; set; } = null!;
        public DbSet<TgCryptoDeposit> TgCryptoDeposits { get; set; } = null!;
        public DbSet<TgChatSession> TgChatSessions { get; set; } = null!;
        public DbSet<TgBotHeartbeat> TgBotHeartbeats { get; set; } = null!;
        public DbSet<TgApiQueue> TgApiQueue { get; set; } = null!;
        public DbSet<TgBankProvider> TgBankProviders { get; set; } = null!;
        public DbSet<TgOperator> TgOperators { get; set; } = null!;
        public DbSet<TgLoginLog> TgLoginLogs { get; set; } = null!;
        public DbSet<TgExchangeRate> TgExchangeRates { get; set; } = null!;
        public DbSet<TgDealer> TgDealers { get; set; } = null!;
        public DbSet<TgDealerRate> TgDealerRates { get; set; } = null!;
        public DbSet<TgDealerRateHistory> TgDealerRateHistories { get; set; } = null!;
    }
}
