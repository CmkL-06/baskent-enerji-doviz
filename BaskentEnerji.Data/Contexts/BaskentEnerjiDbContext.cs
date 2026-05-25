using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Entity.Entities.User;
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
                                                                                                public DbSet<User> Users { get; set; }
                                                                                                    }
                                                                                                    }
