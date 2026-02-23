namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par la Saga quand le paiement est accepté et la commande confirmée.
/// Consommé par le NotificationService (exercice 5) pour envoyer un email.
/// Enrichi avec CustomerId et TransactionId (correction exercice 5).
/// </summary>
public record OrderConfirmed
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }    // ajouté exercice 5
    public string   TransactionId { get; init; } = string.Empty; // ajouté exercice 5
    public DateTime ConfirmedAt   { get; init; }
}
