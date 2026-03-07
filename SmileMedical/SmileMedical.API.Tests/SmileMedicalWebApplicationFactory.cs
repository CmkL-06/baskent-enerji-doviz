using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity;
using SmileMedical.Entity.Entities.User;
using SmileMedical.Entity.Entities.Site;

namespace SmileMedical.API.Tests;

/// <summary>
/// Test için in-memory veritabanı ve seed kullanıcı ile API host.
/// </summary>
public class SmileMedicalWebApplicationFactory : WebApplicationFactory<Program>
{
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmileMedicalDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
            var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(SmileMedicalDbContext));
            if (contextDescriptor != null)
                services.Remove(contextDescriptor);

            services.AddDbContext<SmileMedicalDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmileMedicalDbContext>();
            db.Database.EnsureCreated();

            if (db.Users.Any())
                return;

            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Mail = "test@smilemedical.test",
                Password = HashPassword("Test123!"),
                Firstname = "Test",
                Lastname = "User",
                IsEmailVerified = true,
                Rank = Rank.User,
                CreatedDate = DateTime.UtcNow,
            });
            db.SaveChanges();
        });
    }
}
