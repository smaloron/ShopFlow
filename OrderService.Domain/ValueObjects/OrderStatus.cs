namespace OrderService.Domain.ValueObjects;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Paid = 2,
    Cancelled = 3
}

public static class OrderStatusExtensions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Transitions = new()
    {
        [OrderStatus.Pending]   = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new() { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid]      = new(),
        [OrderStatus.Cancelled] = new()
    };

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        => Transitions[current].Contains(next);

    public static IReadOnlySet<OrderStatus> GetAllowedTransitions(this OrderStatus current)
        => Transitions[current];

    public static bool IsTerminal(this OrderStatus status)
        => status == OrderStatus.Paid || status == OrderStatus.Cancelled;
}
