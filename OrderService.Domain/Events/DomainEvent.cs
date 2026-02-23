namespace OrderService.Domain.Events;

/// <summary>
/// Classe de base abstraite pour tous les événements du domaine.
/// Un événement représente quelque chose qui s'est passé dans le passé.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>Identifiant unique de l'événement (pour la déduplication)</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>Date/heure UTC à laquelle l'événement s'est produit</summary>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
