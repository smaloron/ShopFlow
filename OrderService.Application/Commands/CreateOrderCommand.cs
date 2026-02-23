namespace OrderService.Application.Commands;
using MediatR;

public record CreateOrderCommand(Guid CustomerId, Guid ProductId, int Quantity, decimal UnitPrice, string Currency) : IRequest<Guid>;
