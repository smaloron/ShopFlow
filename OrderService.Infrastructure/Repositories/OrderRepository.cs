namespace OrderService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Persistence;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    public OrderRepository(OrderDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await _context.Orders.Include("_items").FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await _context.Orders.Include("_items").Where(o => o.CustomerId == customerId).OrderByDescending(o => o.OrderDate).ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await _context.Orders.AddAsync(order, cancellationToken);

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Remove(order);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
