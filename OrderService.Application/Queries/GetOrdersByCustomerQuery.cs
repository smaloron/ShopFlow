namespace OrderService.Application.Queries;

using OrderService.Application.Dtos;
using OrderService.Domain.Repositories;

// ── Query ─────────────────────────────────────────────────────────────

/// <summary>Query pour récupérer toutes les commandes d'un client</summary>
public record GetOrdersByCustomerQuery(Guid CustomerId);

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>Handler — retourne une liste de résumés de commandes</summary>
public class GetOrdersByCustomerQueryHandler
{
    private readonly IOrderRepository _repository;

    public GetOrdersByCustomerQueryHandler(IOrderRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<OrderSummaryDto>> HandleAsync(GetOrdersByCustomerQuery query)
    {
        var orders = await _repository.GetByCustomerIdAsync(query.CustomerId);

        return orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.OrderDate,
            o.Status.ToString(),
            o.TotalAmount.Amount,
            o.TotalAmount.Currency
        ));
    }
}
