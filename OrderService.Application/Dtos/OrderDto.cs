namespace OrderService.Application.Dtos;

/// <summary>
/// DTO pour représenter une commande côté API (lecture).
/// Simplifié par rapport à l'entité Order du Domain.
/// </summary>
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

/// <summary>DTO pour représenter une ligne de commande</summary>
public class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal LineTotal { get; init; }
}

/// <summary>DTO de résumé de commande (sans les items)</summary>
public record OrderSummaryDto(
    Guid Id,
    DateTimeOffset OrderDate,
    string Status,
    decimal TotalAmount,
    string Currency
);
