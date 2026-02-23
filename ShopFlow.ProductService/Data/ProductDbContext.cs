namespace ShopFlow.ProductService.Data;
using Microsoft.EntityFrameworkCore;
using ShopFlow.ProductService.Models;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(p => p.Id);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);

        // ── Données de seed synchronisées avec ProductFallbackData ────────
        // Les IDs doivent correspondre aux Guids dans ProductFallbackData.cs
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa1"), Name = "Laptop Pro 15",       Price = 1299.99m, Currency = "EUR", Stock = 10, Reserved = 0, Category = "Electronics",  Description = "Laptop haute performance" },
            new Product { Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa2"), Name = "Clavier Mécanique",   Price = 89.99m,  Currency = "EUR", Stock = 25, Reserved = 0, Category = "Accessories",  Description = "Clavier mécanique RGB" },
            new Product { Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa3"), Name = "Souris Ergonomique",  Price = 45.00m,  Currency = "EUR", Stock = 30, Reserved = 0, Category = "Accessories",  Description = "Souris sans fil ergonomique" },
            new Product { Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa4"), Name = "Écran 27 pouces",     Price = 349.99m, Currency = "EUR", Stock = 5,  Reserved = 0, Category = "Electronics",  Description = "Écran IPS 4K 27\"" },
            new Product { Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa5"), Name = "Casque Audio Pro",   Price = 199.99m, Currency = "EUR", Stock = 15, Reserved = 0, Category = "Accessories",  Description = "Casque audio sans fil" }
        );
    }
}
