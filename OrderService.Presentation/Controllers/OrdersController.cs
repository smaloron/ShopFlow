namespace OrderService.Presentation.Controllers;

using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Queries;
using OrderService.Domain.Repositories;
using ShopFlow.Contracts.Events;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator        _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOrderRepository _orderRepository;

    public OrdersController(IMediator mediator, IPublishEndpoint publishEndpoint, IOrderRepository orderRepository)
    {
        _mediator        = mediator;
        _publishEndpoint = publishEndpoint;
        _orderRepository = orderRepository;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var orderId = await _mediator.Send(command, cancellationToken);
            var order   = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

            if (order is not null)
            {
                await _publishEndpoint.Publish(new OrderPlaced
                {
                    OrderId     = order.Id,
                    CustomerId  = order.CustomerId,
                    TotalAmount = order.TotalAmount.Amount,
                    OrderDate   = order.OrderDate,
                    Items       = order.Items.Select(i => new Contracts.Events.OrderItem
                    {
                        ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice.Amount
                    }).ToList()
                }, cancellationToken);
            }

            return AcceptedAtAction(nameof(GetOrderById), new { id = orderId },
                new CreateOrderResponse { OrderId = orderId, Message = "Commande créée. Traitement du paiement en cours via la Saga." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = "Données invalides.", errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }
        catch (ArgumentException ex)        { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch { return StatusCode(500, new { error = "Erreur inattendue." }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return dto is null ? NotFound(new { error = $"Commande {id} introuvable." }) : Ok(dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer([FromQuery] Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrdersByCustomerQuery(customerId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new CancelOrderCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateRefund(Guid id, [FromBody] RefundRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CalculateRefundQuery(id, request.Amount, request.Currency), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public class CreateOrderResponse { public Guid OrderId { get; init; } public string Message { get; init; } = string.Empty; }
public record RefundRequest(decimal Amount, string Currency);
