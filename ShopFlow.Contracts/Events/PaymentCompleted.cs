namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par PaymentService quand un paiement est accepté.
/// Fait transiter la Saga vers l'état Completed.
/// </summary>
public record PaymentCompleted
{
    public Guid     OrderId       { get; init; }
    public Guid     PaymentId     { get; init; }
    public decimal  Amount        { get; init; }
    public string   TransactionId { get; init; } = string.Empty;
    public DateTime CompletedAt   { get; init; }
}
