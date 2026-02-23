namespace ShopFlow.Contracts.Events;
public record PaymentCompleted { public Guid OrderId { get; init; } public Guid PaymentId { get; init; } public decimal Amount { get; init; } public string TransactionId { get; init; } = string.Empty; public DateTime CompletedAt { get; init; } }
