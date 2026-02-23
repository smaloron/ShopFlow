namespace OrderService.Domain.Entities;

using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

/// <summary>
/// Entité principale (Aggregate Root) représentant une commande.
/// Protège la cohérence de toutes ses lignes de commande.
/// </summary>
public class Order
{
    // ── CHAMPS PRIVÉS ─────────────────────────────────────────────────────

    private readonly List<OrderItem> _items = new();
    private readonly List<DomainEvent> _domainEvents = new();

    // ── PROPRIÉTÉS ────────────────────────────────────────────────────────

    /// <summary>Identifiant unique de la commande</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifiant du client ayant passé la commande</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Date de création de la commande</summary>
    public DateTimeOffset OrderDate { get; private set; }

    /// <summary>État actuel de la commande</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Montant total de la commande</summary>
    public Money TotalAmount { get; private set; }

    /// <summary>Lignes de commande (lecture seule pour l'extérieur)</summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>Événements du domaine en attente de publication</summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // ── CONSTRUCTEUR PRIVÉ ────────────────────────────────────────────────

    private Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        OrderDate = DateTimeOffset.UtcNow;
        Status = OrderStatus.Pending;
        TotalAmount = new Money(0, "EUR");
    }

    // ── FACTORY METHOD ────────────────────────────────────────────────────

    /// <summary>Crée une nouvelle commande pour un client</summary>
    public static Order Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "L'identifiant du client ne peut pas être vide",
                nameof(customerId));

        return new Order(customerId);
    }

    // ── MÉTHODES MÉTIER ───────────────────────────────────────────────────

    /// <summary>Ajoute un produit à la commande</summary>
    public void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException(
                $"Impossible d'ajouter des items à une commande au statut {Status}. " +
                $"Seules les commandes Pending peuvent être modifiées.");

        if (unitPrice.Currency != TotalAmount.Currency)
            throw new InvalidOperationException(
                $"La devise du produit ({unitPrice.Currency}) ne correspond pas " +
                $"à celle de la commande ({TotalAmount.Currency}).");

        var item = OrderItem.Create(productId, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    /// <summary>Confirme la commande (Pending → Confirmed)</summary>
    public void Confirm()
    {
        if (!Status.CanTransitionTo(OrderStatus.Confirmed))
            throw new InvalidOperationException(
                $"Impossible de confirmer une commande au statut {Status}. " +
                $"Seules les commandes Pending peuvent être confirmées.");

        if (_items.Count == 0)
            throw new InvalidOperationException(
                "Impossible de confirmer une commande sans articles.");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderPlaced(Id, TotalAmount));
    }

    /// <summary>Marque la commande comme payée (Confirmed → Paid)</summary>
    public void MarkAsPaid()
    {
        if (!Status.CanTransitionTo(OrderStatus.Paid))
            throw new InvalidOperationException(
                $"Impossible de marquer comme payée une commande au statut {Status}.");

        Status = OrderStatus.Paid;
    }

    /// <summary>Annule la commande (depuis Pending ou Confirmed)</summary>
    public void Cancel(string? reason = null)
    {
        if (!Status.CanTransitionTo(OrderStatus.Cancelled))
            throw new InvalidOperationException(
                $"Impossible d'annuler une commande au statut {Status}. " +
                $"Les commandes Paid ou déjà Cancelled ne peuvent être annulées.");

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelled(Id));
    }

    // ── MÉTHODES PRIVÉES ──────────────────────────────────────────────────

    private void RecalculateTotal()
    {
        if (_items.Count == 0)
        {
            TotalAmount = new Money(0, "EUR");
            return;
        }

        var total = _items
            .Select(item => item.LineTotal)
            .Aggregate((acc, current) => acc.Add(current));

        TotalAmount = total;
    }

    private void AddDomainEvent(DomainEvent @event)
        => _domainEvents.Add(@event);

    /// <summary>Vide la file d'événements (appelé après publication)</summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();

    // ── CONSTRUCTEUR EF CORE ──────────────────────────────────────────────

    private Order()
    {
        TotalAmount = null!;
    }
}
