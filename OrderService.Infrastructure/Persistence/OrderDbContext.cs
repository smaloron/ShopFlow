namespace OrderService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

/// <summary>
/// Contexte de base de données pour les commandes.
/// Configure le mapping objet-relationnel avec EF Core (Fluent API).
/// </summary>
public class OrderDbContext : DbContext
{
    /// <summary>Table des commandes</summary>
    public DbSet<Order> Orders { get; set; } = null!;

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Configuration de l'entité Order ──────────────────────────────

        var orderEntity = modelBuilder.Entity<Order>();

        orderEntity.HasKey(o => o.Id);
        orderEntity.HasIndex(o => o.CustomerId);

        // Conversion enum → int
        orderEntity.Property(o => o.Status).HasConversion<int>();

        // TotalAmount (Value Object) → colonnes aplaties dans Orders
        orderEntity.OwnsOne(o => o.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("TotalAmount")
                 .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Relation Order → OrderItems via le backing field privé _items
        orderEntity
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        orderEntity.Navigation("Items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Les DomainEvents ne sont pas persistés
        orderEntity.Ignore(o => o.DomainEvents);

        // ── Configuration de l'entité OrderItem ──────────────────────────

        var orderItemEntity = modelBuilder.Entity<OrderItem>();

        orderItemEntity.HasKey(i => i.Id);
        orderItemEntity.Property<Guid>("OrderId");

        // UnitPrice (Value Object) → colonnes aplaties dans OrderItems
        orderItemEntity.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("UnitPrice")
                 .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // LineTotal est une propriété calculée — non stockée
        orderItemEntity.Ignore(i => i.LineTotal);
    }
}
