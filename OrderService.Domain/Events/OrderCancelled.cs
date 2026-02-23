namespace OrderService.Domain.Events;

/// <summary>
/// Événement levé quand une commande est annulée.
/// Peut déclencher : remboursement, libération du stock, notification, etc.
/// </summary>
public record OrderCancelled(Guid OrderId) : DomainEvent;
