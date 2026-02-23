namespace OrderService.Domain.Events;

using OrderService.Domain.ValueObjects;

/// <summary>
/// Événement levé quand une commande est confirmée (placée).
/// Peut déclencher : envoi d'email, déduction du stock, notification, etc.
/// </summary>
public record OrderPlaced(Guid OrderId, Money TotalAmount) : DomainEvent;
