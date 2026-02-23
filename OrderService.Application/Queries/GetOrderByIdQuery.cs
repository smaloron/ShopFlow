namespace OrderService.Application.Queries;

using OrderService.Application.Dtos;
using OrderService.Domain.Repositories;

// ── Query ─────────────────────────────────────────────────────────────

/// <summary>Query pour récupérer une commande par son ID</summary>
public record GetOrderByIdQuery(Guid OrderId);

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>
/// Handler pour traiter la query GetOrderById.
/// Responsable de mapper l'entité Domain vers un DTO.
/// </summary>
public class GetOrderByIdQueryHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    /// <summary>Traite la query et retourne le DTO, ou null si non trouvé</summary>
    public async Task<OrderDto?> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        // 1. Récupération
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order == null)
            return null;

        // 2. Mapping / Projection vers DTO
        return new OrderDto
        {
            Id          = order.Id,
            CustomerId  = order.CustomerId,
            OrderDate   = order.OrderDate,
            Status      = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Currency    = order.TotalAmount.Currency,
            Items       = order.Items.Select(item => new OrderItemDto
            {
                Id        = item.Id,
                ProductId = item.ProductId,
                Quantity  = item.Quantity,
                UnitPrice = item.UnitPrice.Amount,
                Currency  = item.UnitPrice.Currency,
                LineTotal = item.LineTotal.Amount
            }).ToList()
        };
    }
}
