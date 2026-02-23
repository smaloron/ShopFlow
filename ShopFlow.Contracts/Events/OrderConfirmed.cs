namespace ShopFlow.Contracts.Events;
public record OrderConfirmed { public Guid OrderId { get; init; } public Guid CustomerId { get; init; } public string TransactionId { get; init; } = string.Empty; public DateTime ConfirmedAt { get; init; } }
