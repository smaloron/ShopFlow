namespace OrderService.Application.Commands;

using MediatR;
using OrderService.Application.Clients;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

/// <summary>
/// Handler MediatR pour CreateOrderCommand.
/// Orchestre : vérification stock (Refit) → Value Objects → Entité → persistance.
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository      _orderRepository;
    private readonly IProductServiceClient _productClient;

    public CreateOrderCommandHandler(
        IOrderRepository      orderRepository,
        IProductServiceClient productClient)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productClient   = productClient   ?? throw new ArgumentNullException(nameof(productClient));
    }

    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Vérification du stock via Refit (appel HTTP vers Product Service)
        try
        {
            var stock = await _productClient.GetStockAsync(command.ProductId, cancellationToken);

            if (stock.Available < command.Quantity)
                throw new InvalidOperationException(
                    $"Stock insuffisant pour le produit {command.ProductId}. " +
                    $"Disponible : {stock.Available}, demandé : {command.Quantity}.");
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Produit {command.ProductId} introuvable dans le catalogue.");
        }
        catch (HttpRequestException)
        {
            // Product Service indisponible → on laisse passer (dégradé gracieux)
            // En production : circuit breaker Polly
        }

        // 2. Création du Value Object (validation métier dans Money)
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
