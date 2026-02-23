namespace ShopFlow.Contracts.Events;
public record PaymentRequested { public Guid OrderId { get; init; } public Guid CustomerId { get; init; } public decimal Amount { get; init; } public string PaymentMethod { get; init; } = "CreditCard"; public DateTime RequestedAt { get; init; } }
