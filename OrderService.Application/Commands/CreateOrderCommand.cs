namespace OrderService.Application.Commands;

/// <summary>
/// Command pour créer une nouvelle commande.
/// Immutable (record) — représente l'intention de l'utilisateur.
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string Currency
);
