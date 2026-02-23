namespace OrderService.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;

/// <summary>
/// Contrôleur pour les opérations de paiement d'une commande.
/// Délègue au ProcessPaymentCommandHandler via IMediator → gRPC → Payment Service.
/// </summary>
[ApiController]
[Route("api/orders/{orderId:guid}/payment")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
        => _mediator = mediator;

    // ── POST /api/orders/{orderId}/payment ────────────────────────────────

    /// <summary>
    /// Lance le paiement d'une commande via le Payment Service (gRPC).
    /// La commande doit être à l'état Confirmed.
    /// </summary>
    /// <response code="200">Paiement accepté</response>
    /// <response code="400">Paiement refusé ou commande non payable</response>
    /// <response code="503">Payment Service indisponible</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Pay(
        Guid                orderId,
        [FromBody] PayRequest request,
        CancellationToken   cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new ProcessPaymentCommand(orderId, request.Amount, request.Currency),
                cancellationToken);

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

/// <summary>Corps de la requête de paiement</summary>
public record PayRequest(decimal Amount, string Currency = "EUR");
