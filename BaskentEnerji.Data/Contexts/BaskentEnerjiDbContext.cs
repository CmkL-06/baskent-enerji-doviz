using Microsoft.EntityFrameworkCore;
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
        public DbSet<User> Users { get; set; } = null!;

        // Blog Entities
        public DbSet<Blog_Article> Blog_Articles { get; set; } = null!;
        public DbSet<Blog_Article_Category> Blog_Article_Categories { get; set; } = null!;
        public DbSet<Blog_Article_Comment> Blog_Article_Comments { get; set; } = null!;
        public DbSet<Blog_Article_Tag> Blog_Article_Tags { get; set; } = null!;
        public DbSet<Blog_Article_Visit> Blog_Article_Visits { get; set; } = null!;
        public DbSet<Blog_Category> Blog_Categories { get; set; } = null!;
        public DbSet<Blog_Category_Tag> Blog_Category_Tags { get; set; } = null!;

        // Coin Entities
        public DbSet<Coin> Coins { get; set; } = null!;
        public DbSet<Coin_User_Favorite> Coin_User_Favorites { get; set; } = null!;
        public DbSet<Coin_User_Table> Coin_User_Tables { get; set; } = null!;

        // ExchangeOffice Entities
        public DbSet<ExchangeGate> ExchangeGates { get; set; } = null!;
        public DbSet<ExchangeOffice> ExchangeOffices { get; set; } = null!;
        public DbSet<Currency> Currencies { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Office> Offices { get; set; } = null!;
        public DbSet<Party> Parties { get; set; } = null!;

        // Site Entities
        public DbSet<Slider> Sliders { get; set; } = null!;
        public DbSet<Slider_Item> Slider_Items { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Theme> Themes { get; set; } = null!;
    }
}
