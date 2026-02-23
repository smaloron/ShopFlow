namespace ShopFlow.OrderService.Controllers;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Contracts.Events;
using ShopFlow.OrderService.Data;
using ShopFlow.OrderService.Models;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext context, IPublishEndpoint publishEndpoint, ILogger<OrdersController> logger)
    { _context = context; _publishEndpoint = publishEndpoint; _logger = logger; }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any()) return BadRequest(new { error = "La commande doit contenir au moins un article." });
        var order = new Order { Id = Guid.NewGuid(), CustomerId = request.CustomerId, OrderDate = DateTime.UtcNow, Status = "Pending", Items = request.Items.Select(i => new OrderItem { Id = Guid.NewGuid(), ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList() };
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await _publishEndpoint.Publish(new OrderPlaced { OrderId = order.Id, CustomerId = order.CustomerId, TotalAmount = order.TotalAmount, OrderDate = order.OrderDate, Items = order.Items.Select(i => new Contracts.Events.OrderItem { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList() });
        return Accepted(new { orderId = order.Id, status = "Processing", message = "Commande reçue. Traitement du paiement en cours." });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound(new { error = $"Commande {id} introuvable." }) : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetByCustomer([FromQuery] Guid customerId)
    {
        var orders = await _context.Orders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.OrderDate).ToListAsync();
        return Ok(orders);
    }
}

public record CreateOrderRequest { public Guid CustomerId { get; init; } public List<CreateOrderItemRequest> Items { get; init; } = new(); }
public record CreateOrderItemRequest { public Guid ProductId { get; init; } public int Quantity { get; init; } public decimal UnitPrice { get; init; } }
