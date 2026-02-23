namespace OrderService.Application.Commands;

using OrderService.Domain.Repositories;

// ── Command ───────────────────────────────────────────────────────────

/// <summary>Command pour annuler une commande existante</summary>
public record CancelOrderCommand(Guid OrderId);

// ── Handler ───────────────────────────────────────────────────────────

/// <summary>Handler du cas d'usage "Annuler une commande"</summary>
public class CancelOrderCommandHandler
{
    private readonly IOrderRepository _repository;

    public CancelOrderCommandHandler(IOrderRepository repository)
        => _repository = repository;

    public async Task HandleAsync(CancelOrderCommand command)
    {
        var order = await _repository.GetByIdAsync(command.OrderId)
            ?? throw new KeyNotFoundException($"Commande {command.OrderId} introuvable.");

        order.Cancel(); // La règle métier est dans le Domain

        await _repository.UpdateAsync(order);
        await _repository.SaveChangesAsync();
    }
}
