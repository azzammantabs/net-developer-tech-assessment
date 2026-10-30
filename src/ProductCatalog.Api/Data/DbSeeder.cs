using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!context.Users.Any())
        {
            context.Users.Add(new AppUser
            {
                Username = "admin",
                Email = "admin@catalog.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product
                {
                    Name = "Enterprise Cloud Router X200",
                    Description = "High-throughput dual-WAN gigabit router with hardware VPN acceleration.",
                    Price = 349.99m,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Name = "Ergonomic Mechanical Keyboard Pro",
                    Description = "Hot-swappable tactile switches with aircraft-grade aluminum frame.",
                    Price = 149.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Product
                {
                    Name = "27-Inch 4K IPS Developer Monitor",
                    Description = "Factory-calibrated ultra-sharp display with USB-C 90W power delivery.",
                    Price = 499.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            );
        }

        await context.SaveChangesAsync();
    }
}
