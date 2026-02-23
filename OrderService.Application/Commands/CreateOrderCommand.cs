namespace OrderService.Application.Commands;

using MediatR;

/// <summary>
/// Command pour créer une nouvelle commande.
/// Implémente IRequest&lt;Guid&gt; pour MediatR → retourne l'ID créé.
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string Currency
) : IRequest<Guid>;
