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
    private readonly OrderDbContext   _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderDbContext            context,
        IPublishEndpoint          publishEndpoint,
        ILogger<OrdersController> logger)
    {
        _context         = context;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    // ── POST /api/orders ──────────────────────────────────────────────

    /// <summary>
    /// Crée une commande et publie OrderPlaced dans RabbitMQ.
    /// Retourne 202 Accepted (traitement asynchrone via Saga).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            return BadRequest(new { error = "La commande doit contenir au moins un article." });

        // 1. Créer et persister la commande
        var order = new Order
        {
            Id         = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            OrderDate  = DateTime.UtcNow,
            Status     = "Pending",
            Items      = request.Items.Select(i => new Models.OrderItem
            {
                Id        = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity  = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "✅ Commande {OrderId} créée — Client {CustomerId} — {Total:C}",
            order.Id, order.CustomerId, order.TotalAmount);

        // 2. Publier l'événement OrderPlaced → RabbitMQ → Saga
        await _publishEndpoint.Publish(new OrderPlaced
        {
            OrderId     = order.Id,
            CustomerId  = order.CustomerId,
            TotalAmount = order.TotalAmount,
            OrderDate   = order.OrderDate,
            Items       = order.Items.Select(i => new Contracts.Events.OrderItem
            {
                ProductId = i.ProductId,
                Quantity  = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        _logger.LogInformation("📤 OrderPlaced publié pour commande {OrderId}", order.Id);

        // 202 Accepted : la réponse définitive arrivera via les événements (async)
        return Accepted(new
        {
            orderId = order.Id,
            status  = "Processing",
            message = "Commande reçue. Traitement du paiement en cours."
        });
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────

    /// <summary>Récupère une commande par son ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null
            ? NotFound(new { error = $"Commande {id} introuvable." })
            : Ok(order);
    }

    // ── GET /api/orders?customerId={guid} ─────────────────────────────

    /// <summary>Liste les commandes d'un client</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer([FromQuery] Guid customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(orders);
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────

public record CreateOrderRequest
{
    public Guid CustomerId { get; init; }
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

public record CreateOrderItemRequest
{
    public Guid    ProductId { get; init; }
    public int     Quantity  { get; init; }
    public decimal UnitPrice { get; init; }
}
