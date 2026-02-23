namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par PaymentService quand un paiement échoue.
/// Fait transiter la Saga vers l'état Cancelled (compensation).
/// </summary>
public record PaymentFailed
{
    public Guid     OrderId   { get; init; }
    public string   Reason    { get; init; } = string.Empty;
    public string   ErrorCode { get; init; } = string.Empty;
    public DateTime FailedAt  { get; init; }
}
