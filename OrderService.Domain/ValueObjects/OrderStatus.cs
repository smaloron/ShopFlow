namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Représente les différents états possibles d'une commande.
/// Suit un workflow métier précis (machine à états).
/// </summary>
public enum OrderStatus
{
    /// <summary>Commande créée, en attente de confirmation</summary>
    Pending = 0,

    /// <summary>Commande confirmée par le client, en attente de paiement</summary>
    Confirmed = 1,

    /// <summary>Commande payée (état terminal)</summary>
    Paid = 2,

    /// <summary>Commande annulée (état terminal)</summary>
    Cancelled = 3
}

/// <summary>
/// Extension methods pour gérer les transitions d'état valides.
/// Implémente une machine à états finis (FSM).
/// </summary>
public static class OrderStatusExtensions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Transitions = new()
    {
        [OrderStatus.Pending]   = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid]      = new(),
        [OrderStatus.Cancelled] = new()
    };

    /// <summary>Vérifie si une transition d'état est autorisée</summary>
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        => Transitions[current].Contains(next);

    /// <summary>Retourne les états accessibles depuis l'état actuel</summary>
    public static IReadOnlySet<OrderStatus> GetAllowedTransitions(this OrderStatus current)
        => Transitions[current];

    /// <summary>Vérifie si l'état est terminal</summary>
    public static bool IsTerminal(this OrderStatus status)
        => status == OrderStatus.Paid || status == OrderStatus.Cancelled;
}
