namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par OrderService quand une commande est créée et soumise.
/// Déclenche la Saga d'orchestration.
/// </summary>
public record OrderPlaced
{
    public Guid OrderId    { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime OrderDate  { get; init; }
    public List<OrderItem> Items { get; init; } = new();
}

/// <summary>Ligne de commande incluse dans OrderPlaced</summary>
public record OrderItem
{
    public Guid    ProductId { get; init; }
    public int     Quantity  { get; init; }
    public decimal UnitPrice { get; init; }
}
