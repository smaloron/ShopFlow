namespace OrderService.Application.Commands;

using MediatR;
using OrderService.Domain.Repositories;

// ── Command ───────────────────────────────────────────────────────────

/// <summary>Command pour annuler une commande. Retourne Unit (pas de valeur de retour).</summary>
public record CancelOrderCommand(Guid OrderId) : IRequest<Unit>;

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>Handler MediatR du cas d'usage "Annuler une commande"</summary>
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Unit>
{
    private readonly IOrderRepository _repository;

    public CancelOrderCommandHandler(IOrderRepository repository)
        => _repository = repository;

    public async Task<Unit> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Commande {command.OrderId} introuvable.");

        order.Cancel();

        await _repository.UpdateAsync(order, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
