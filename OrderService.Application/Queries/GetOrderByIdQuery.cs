namespace OrderService.Application.Queries;

using MediatR;
using OrderService.Application.Dtos;
using OrderService.Domain.Repositories;

// ── Query ─────────────────────────────────────────────────────────────

/// <summary>Query pour récupérer une commande par son ID</summary>
public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>
/// Handler MediatR pour GetOrderByIdQuery.
/// Responsable de mapper l'entité Domain vers un DTO.
/// </summary>
public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order == null)
            return null;

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
