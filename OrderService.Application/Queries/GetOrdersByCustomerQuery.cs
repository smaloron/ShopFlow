namespace OrderService.Application.Queries;
using MediatR;
using OrderService.Application.Dtos;
using OrderService.Domain.Repositories;

public record GetOrdersByCustomerQuery(Guid CustomerId) : IRequest<IEnumerable<OrderSummaryDto>>;

public class GetOrdersByCustomerQueryHandler : IRequestHandler<GetOrdersByCustomerQuery, IEnumerable<OrderSummaryDto>>
{
    private readonly IOrderRepository _repository;
    public GetOrdersByCustomerQueryHandler(IOrderRepository repository) => _repository = repository;

    public async Task<IEnumerable<OrderSummaryDto>> Handle(GetOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);
        return orders.Select(o => new OrderSummaryDto(o.Id, o.OrderDate, o.Status.ToString(), o.TotalAmount.Amount, o.TotalAmount.Currency));
    }
}
