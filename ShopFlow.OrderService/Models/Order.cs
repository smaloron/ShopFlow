namespace ShopFlow.OrderService.Models;

/// <summary>Entité Order persistée dans la base SQLite du OrderService</summary>
public class Order
{
    public Guid     Id          { get; set; }
    public Guid     CustomerId  { get; set; }
    public DateTime OrderDate   { get; set; }
    public string   Status      { get; set; } = "Pending";
    public decimal  TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>Entité OrderItem persistée dans la base SQLite du OrderService</summary>
public class OrderItem
{
    public Guid    Id        { get; set; }
    public Guid    OrderId   { get; set; }
    public Guid    ProductId { get; set; }
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
}
