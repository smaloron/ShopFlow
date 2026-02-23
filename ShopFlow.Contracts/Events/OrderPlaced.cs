namespace ShopFlow.Contracts.Events;

public record OrderPlaced
{
    public Guid OrderId    { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTimeOffset OrderDate  { get; init; }
    public List<OrderItem> Items { get; init; } = new();
}

public record OrderItem
{
    public Guid    ProductId { get; init; }
    public int     Quantity  { get; init; }
    public decimal UnitPrice { get; init; }
}
