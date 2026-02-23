namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par la Saga quand le paiement échoue ou expire (compensation).
/// </summary>
public record OrderCancelled
{
    public Guid     OrderId     { get; init; }
    public string   Reason      { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
