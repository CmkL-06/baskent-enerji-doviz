using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Coin;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using BaskentEnerji.Entity.Entities.Site;
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
                                                                        modelBuilder.HasDefaultSchema("mtturkey_exchange");
                                                                                }

                                                                                        // User
                                                                                                public DbSet<User> Users { get; set; }

                                                                                                        // Coin
                                                                                                                public DbSet<Coin> Coins { get; set; }
                                                                                                                
                                                                                                                        // ExchangeOffice
                                                                                                                                public DbSet<Currency> Currencies { get; set; }
                                                                                                                                        public DbSet<ExchangeRate> ExchangeRates { get; set; }
                                                                                                                                                public DbSet<ExchangeRateHistory> ExchangeRateHistories { get; set; }
                                                                                                                                                        public DbSet<ExchangeSettings> ExchangeSettings { get; set; }
                                                                                                                                                                public DbSet<ExternalDataSource> ExternalDataSources { get; set; }
                                                                                                                                                                        public DbSet<ExternalRateCache> ExternalRateCaches { get; set; }
                                                                                                                                                                                public DbSet<PendingRateApproval> PendingRateApprovals { get; set; }
                                                                                                                                                                                        public DbSet<Expense> Expenses { get; set; }
                                                                                                                                                                                                public DbSet<Office> Offices { get; set; }
                                                                                                                                                                                                        public DbSet<Party> Parties { get; set; }
                                                                                                                                                                                                            }
                                                                                                                                                                                                            }
