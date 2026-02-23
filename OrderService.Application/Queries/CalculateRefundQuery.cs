namespace OrderService.Application.Queries;

using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

// ── DTOs ──────────────────────────────────────────────────────────────

public record CalculateRefundQuery(Guid OrderId, decimal RefundAmount, string Currency);

public record RefundResultDto(decimal RemainingAmount, string Currency, bool IsFullyRefunded);

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>
/// Handler de calcul de remboursement partiel.
/// C'est une Query (pas de modification en DB).
/// </summary>
public class CalculateRefundQueryHandler
{
    private readonly IOrderRepository _repository;

    public CalculateRefundQueryHandler(IOrderRepository repository)
        => _repository = repository;

    public async Task<RefundResultDto> HandleAsync(CalculateRefundQuery query)
    {
        var order = await _repository.GetByIdAsync(query.OrderId)
            ?? throw new KeyNotFoundException($"Commande {query.OrderId} introuvable.");

        var refund    = new Money(query.RefundAmount, query.Currency);
        var remaining = order.TotalAmount.Subtract(refund);

        return new RefundResultDto(
            remaining.Amount,
            remaining.Currency,
            remaining.Amount == 0
        );
    }
}
