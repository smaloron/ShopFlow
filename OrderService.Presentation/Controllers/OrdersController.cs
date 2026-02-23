namespace OrderService.Presentation.Controllers;

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Queries;

/// <summary>
/// Contrôleur REST pour gérer les commandes.
/// Jour 2 : utilise IMediator au lieu d'injecter chaque Handler directement.
/// Une seule dépendance — découplage maximal.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
        => _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    // ── POST /api/orders ─────────────────────────────────────────────────

    /// <summary>Crée une nouvelle commande</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderId = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = orderId },
                new CreateOrderResponse { OrderId = orderId });
        }
        catch (ValidationException ex)
        {
            // Erreurs FluentValidation → 400 avec le détail de chaque règle
            return BadRequest(new
            {
                error  = "Données invalides.",
                errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { error = "Une erreur inattendue s'est produite." });
        }
    }

    // ── GET /api/orders/{id} ─────────────────────────────────────────────

    /// <summary>Récupère une commande par son identifiant</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var orderDto = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);

        return orderDto is null
            ? NotFound(new { error = $"Commande {id} introuvable." })
            : Ok(orderDto);
    }

    // ── GET /api/orders?customerId={guid} ────────────────────────────────

    /// <summary>Récupère toutes les commandes d'un client</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(
        [FromQuery] Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrdersByCustomerQuery(customerId), cancellationToken);
        return Ok(result);
    }

    // ── DELETE /api/orders/{id} ──────────────────────────────────────────

    /// <summary>Annule une commande</summary>
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── POST /api/orders/{id}/refund ─────────────────────────────────────

    /// <summary>Calcule le montant restant après remboursement partiel</summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateRefund(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CalculateRefundQuery(id, request.Amount, request.Currency),
                cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// ── DTOs de requête/réponse ───────────────────────────────────────────

/// <summary>Réponse après création d'une commande</summary>
public class CreateOrderResponse
{
    public Guid OrderId { get; init; }
}

/// <summary>Corps de la requête de remboursement</summary>
public record RefundRequest(decimal Amount, string Currency);
