namespace OrderService.Application.Commands;

using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

/// <summary>
/// Handler pour traiter la command CreateOrder.
/// Orchestre : validation → Value Objects → Entité → persistance.
/// </summary>
public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    /// <summary>Traite la command et retourne l'ID de la commande créée</summary>
    public async Task<Guid> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation applicative (pas de logique métier)
        if (command.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId invalide", nameof(command.CustomerId));

        if (command.ProductId == Guid.Empty)
            throw new ArgumentException("ProductId invalide", nameof(command.ProductId));

        // 2. Création des Value Objects (la validation métier est dans Money)
        var unitPrice = new Money(command.UnitPrice, command.Currency);

        // 3. Création de l'entité via Factory Method
        var order = Order.Create(command.CustomerId);

        // 4. Ajout des items (règles métier dans Order.AddItem)
        order.AddItem(command.ProductId, command.Quantity, unitPrice);

        // 5. Confirmation (transition d'état + événement OrderPlaced)
        order.Confirm();

        // 6. Persistance (Unit of Work)
        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
