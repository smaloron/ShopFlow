namespace OrderService.Domain.Events;

public record OrderCancelled(Guid OrderId) : DomainEvent;
