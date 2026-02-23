namespace ShopFlow.OrderService.Models;
public class Order { public Guid Id { get; set; } public Guid CustomerId { get; set; } public DateTime OrderDate { get; set; } public string Status { get; set; } = "Pending"; public decimal TotalAmount { get; set; } public List<OrderItem> Items { get; set; } = new(); }
public class OrderItem { public Guid Id { get; set; } public Guid OrderId { get; set; } public Guid ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
