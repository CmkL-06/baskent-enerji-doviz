husing Microsoft.EntityFrameworkCore;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Coin;
using BaskentEnerji.Entity.Entities.ExchangeOffice;
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

                                                                                        // User - Temel Entity

                                                                                                        // User - Temel Entity
                                                                                                                public DbSet<User> Users { get; set; }
                                                                                                                
                                                                                                                        // Blog Entities
                                                                                                                                public DbSet<Blog_Article> Blog_Articles { get; set; }
                                                                                                                                        public DbSet<Blog_Article_Category> Blog_Article_Categories { get; set; }
                                                                                                                                                public DbSet<Blog_Article_Comment> Blog_Article_Comments { get; set; }
                                                                                                                                                        public DbSet<Blog_Article_Tag> Blog_Article_Tags { get; set; }
                                                                                                                                                                public DbSet<Blog_Article_Visit> Blog_Article_Visits { get; set; }
                                                                                                                                                                        public DbSet<Blog_Category> Blog_Categories { get; set; }
                                                                                                                                                                                public DbSet<Blog_Category_Tag> Blog_Category_Tags { get; set; }
                                                                                                                                                                                
                                                                                                                                                                                        // Coin Entities
                                                                                                                                                                                                public DbSet<Coin> Coins { get; set; }
                                                                                                                                                                                                        public DbSet<Coin_User_Favorite> Coin_User_Favorites { get; set; }
                                                                                                                                                                                                                public DbSet<Coin_User_Table> Coin_User_Tables { get; set; }
                                                                                                                                                                                                                
                                                                                                                                                                                                                        // ExchangeOffice Entities
                                                                                                                                                                                                                                public DbSet<ExchangeGate> ExchangeGates { get; set; }
                                                                                                                                                                                                                                        public DbSet<ExchangeOffice> ExchangeOffices { get; set; }
                                                                                                                                                                                                                                                public DbSet<Currency> Currencies { get; set; }
                                                                                                                                                                                                                                                        public DbSet<Expense> Expenses { get; set; }
                                                                                                                                                                                                                                                                public DbSet<Office> Offices { get; set; }
                                                                                                                                                                                                                                                                        public DbSet<Party> Parties { get; set; }
                                                                                                                                                                                                                                                                        
                                                                                                                                                                                                                                                                                // Site Entities
                                                                                                                                                                                                                                                                                        public DbSet<Slider> Sliders { get; set; }
                                                                                                                                                                                                                                                                                                public DbSet<Slider_Item> Slider_Items { get; set; }
                                                                                                                                                                                                                                                                                                        public DbSet<Tag> Tags { get; set; }
                                                                                                                                                                                                                                                                                                                public DbSet<Theme> Themes { get; set; }
                                                                                                                                                                                                                                                                                                                    }
                                                                                                                                                                                                                                                                                                                    }
