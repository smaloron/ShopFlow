namespace OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

public record OrderPlaced(Guid OrderId, Money TotalAmount) : DomainEvent;
