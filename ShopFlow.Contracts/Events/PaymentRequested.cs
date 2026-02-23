namespace ShopFlow.Contracts.Events;

/// <summary>
/// Publié par la Saga vers le PaymentService.
/// Sémantiquement une "commande" (demande d'action), même si l'API MassTransit est identique.
/// </summary>
public record PaymentRequested
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }
    public decimal  Amount        { get; init; }
    public string   PaymentMethod { get; init; } = "CreditCard";
    public DateTime RequestedAt   { get; init; }
}
