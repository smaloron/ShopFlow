namespace OrderService.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;

[ApiController]
[Route("api/orders/{orderId:guid}/payment")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Pay(Guid orderId, [FromBody] PayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new ProcessPaymentCommand(orderId, request.Amount, request.Currency), cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("indisponible"))
        {
            return StatusCode(503, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record PayRequest(decimal Amount, string Currency = "EUR");
