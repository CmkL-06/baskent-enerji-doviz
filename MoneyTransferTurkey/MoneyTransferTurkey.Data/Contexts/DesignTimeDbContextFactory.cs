using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MoneyTransferTurkey.Data.Contexts
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MoneyTransferTurkeyDbContext>
    {
        public MoneyTransferTurkeyDbContext CreateDbContext(string[] args)
        {
            // Build the configuration object
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Get project root directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Load appsettings.json
                .Build();

            // Get the connection string using its name (replace "DefaultConnection" if different)
            string connectionString = configuration.GetConnectionString("SQL");

            // Use the connection string to create the DbContext
            DbContextOptionsBuilder<MoneyTransferTurkeyDbContext> dbContextOptionsBuilder = new();
            dbContextOptionsBuilder.UseSqlServer(connectionString);

            return new MoneyTransferTurkeyDbContext(dbContextOptionsBuilder.Options);
        }
    }

}
