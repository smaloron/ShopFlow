namespace OrderService.Application.Commands;

using MediatR;
using OrderService.Application.Clients;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

/// <summary>
/// Handler MediatR pour CreateOrderCommand.
/// Orchestre : vérification stock (Refit) → Value Objects → Entité → persistance.
///
/// Jour 3 : après persistance, la publication MassTransit est faite
/// dans la couche Presentation (CreateOrderCommandHandler dans Program.cs)
/// pour ne pas coupler Application à MassTransit.
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
            // Product Service indisponible → dégradé gracieux
        }

        // 2. Création du Value Object
        var unitPrice = new Money(command.UnitPrice, command.Currency);

        // 3. Création de l'entité via Factory Method
        var order = Order.Create(command.CustomerId);
        order.AddItem(command.ProductId, command.Quantity, unitPrice);
        order.Confirm();

        // 4. Persistance
        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
