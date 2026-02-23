namespace ShopFlow.OrderService.Data;
using Microsoft.EntityFrameworkCore;
using ShopFlow.OrderService.Models;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<Order>().HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderItem>().HasKey(i => i.Id);
    }
}
