namespace OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = null!;
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var orderEntity = modelBuilder.Entity<Order>();
        orderEntity.HasKey(o => o.Id);
        orderEntity.HasIndex(o => o.CustomerId);
        orderEntity.Property(o => o.Status).HasConversion<int>();
        orderEntity.OwnsOne(o => o.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        orderEntity.HasMany<OrderItem>("_items").WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
        orderEntity.Ignore(o => o.DomainEvents);

        var orderItemEntity = modelBuilder.Entity<OrderItem>();
        orderItemEntity.HasKey(i => i.Id);
        orderItemEntity.Property<Guid>("OrderId");
        orderItemEntity.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        orderItemEntity.Ignore(i => i.LineTotal);
    }
}
