namespace OrderService.Application.Dtos;

public class OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public DateTimeOffset OrderDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal LineTotal { get; init; }
}

public record OrderSummaryDto(Guid Id, DateTimeOffset OrderDate, string Status, decimal TotalAmount, string Currency);
