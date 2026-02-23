namespace OrderService.Application.Queries;
using MediatR;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

public record CalculateRefundQuery(Guid OrderId, decimal RefundAmount, string Currency) : IRequest<RefundResultDto>;
public record RefundResultDto(decimal RemainingAmount, string Currency, bool IsFullyRefunded);

public class CalculateRefundQueryHandler : IRequestHandler<CalculateRefundQuery, RefundResultDto>
{
    private readonly IOrderRepository _repository;
    public CalculateRefundQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<RefundResultDto> Handle(CalculateRefundQuery query, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Commande {query.OrderId} introuvable.");
        var refund = new Money(query.RefundAmount, query.Currency);
        var remaining = order.TotalAmount.Subtract(refund);
        return new RefundResultDto(remaining.Amount, remaining.Currency, remaining.Amount == 0);
    }
}
