namespace ShopFlow.Contracts.Events;
public record OrderCancelled { public Guid OrderId { get; init; } public string Reason { get; init; } = string.Empty; public DateTime CancelledAt { get; init; } }
