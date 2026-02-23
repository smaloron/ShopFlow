namespace OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

public class Order
{
    private readonly List<OrderItem> _items = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        OrderDate = DateTimeOffset.UtcNow;
        Status = OrderStatus.Pending;
        TotalAmount = new Money(0, "EUR");
    }

    public static Order Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("L'identifiant du client ne peut pas être vide", nameof(customerId));
        return new Order(customerId);
    }

    public void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Impossible d'ajouter des items à une commande au statut {Status}.");
        if (unitPrice.Currency != TotalAmount.Currency)
            throw new InvalidOperationException($"La devise du produit ({unitPrice.Currency}) ne correspond pas à celle de la commande ({TotalAmount.Currency}).");
        var item = OrderItem.Create(productId, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (!Status.CanTransitionTo(OrderStatus.Confirmed))
            throw new InvalidOperationException($"Impossible de confirmer une commande au statut {Status}.");
        if (_items.Count == 0)
            throw new InvalidOperationException("Impossible de confirmer une commande sans articles.");
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderPlaced(Id, TotalAmount));
    }

    public void MarkAsPaid()
    {
        if (!Status.CanTransitionTo(OrderStatus.Paid))
            throw new InvalidOperationException($"Impossible de marquer comme payée une commande au statut {Status}.");
        Status = OrderStatus.Paid;
    }

    public void Cancel(string? reason = null)
    {
        if (!Status.CanTransitionTo(OrderStatus.Cancelled))
            throw new InvalidOperationException($"Impossible d'annuler une commande au statut {Status}.");
        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelled(Id));
    }

    private void RecalculateTotal()
    {
        if (_items.Count == 0) { TotalAmount = new Money(0, "EUR"); return; }
        var total = _items.Select(item => item.LineTotal).Aggregate((acc, current) => acc.Add(current));
        TotalAmount = total;
    }

    private void AddDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Order() { TotalAmount = null!; }
}
