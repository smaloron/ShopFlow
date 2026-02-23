namespace ShopFlow.Contracts.Events;
public record PaymentFailed { public Guid OrderId { get; init; } public string Reason { get; init; } = string.Empty; public string ErrorCode { get; init; } = string.Empty; public DateTime FailedAt { get; init; } }
