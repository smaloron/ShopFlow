namespace OrderService.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.Application.Queries;

/// <summary>
/// Contrôleur REST pour gérer les commandes.
/// Responsabilités : validation HTTP, délégation aux Handlers, codes HTTP.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderCommandHandler       _createOrderHandler;
    private readonly GetOrderByIdQueryHandler        _getOrderByIdHandler;
    private readonly CancelOrderCommandHandler       _cancelHandler;
    private readonly GetOrdersByCustomerQueryHandler _getByCustomerHandler;
    private readonly CalculateRefundQueryHandler     _refundHandler;

    public OrdersController(
        CreateOrderCommandHandler       createOrderHandler,
        GetOrderByIdQueryHandler        getOrderByIdHandler,
        CancelOrderCommandHandler       cancelHandler,
        GetOrdersByCustomerQueryHandler getByCustomerHandler,
        CalculateRefundQueryHandler     refundHandler)
    {
        _createOrderHandler   = createOrderHandler   ?? throw new ArgumentNullException(nameof(createOrderHandler));
        _getOrderByIdHandler  = getOrderByIdHandler  ?? throw new ArgumentNullException(nameof(getOrderByIdHandler));
        _cancelHandler        = cancelHandler        ?? throw new ArgumentNullException(nameof(cancelHandler));
        _getByCustomerHandler = getByCustomerHandler ?? throw new ArgumentNullException(nameof(getByCustomerHandler));
        _refundHandler        = refundHandler        ?? throw new ArgumentNullException(nameof(refundHandler));
    }

    // ── POST /api/orders ─────────────────────────────────────────────────

    /// <summary>Crée une nouvelle commande</summary>
    /// <response code="201">Commande créée avec succès</response>
    /// <response code="400">Données invalides ou règle métier violée</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var orderId = await _createOrderHandler.Handle(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = orderId },
                new CreateOrderResponse { OrderId = orderId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { error = "Une erreur inattendue s'est produite" });
        }
    }

    // ── GET /api/orders/{id} ─────────────────────────────────────────────

    /// <summary>Récupère une commande par son identifiant</summary>
    /// <response code="200">Commande trouvée</response>
    /// <response code="404">Commande introuvable</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query    = new GetOrderByIdQuery(id);
            var orderDto = await _getOrderByIdHandler.Handle(query, cancellationToken);

            if (orderDto == null)
                return NotFound(new { error = $"Commande {id} introuvable" });

            return Ok(orderDto);
        }
        catch
        {
            return StatusCode(500, new { error = "Une erreur inattendue s'est produite" });
        }
    }

    // ── GET /api/orders?customerId={guid} ────────────────────────────────

    /// <summary>Récupère toutes les commandes d'un client (exercice 3)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer([FromQuery] Guid customerId)
    {
        var query  = new GetOrdersByCustomerQuery(customerId);
        var result = await _getByCustomerHandler.HandleAsync(query);
        return Ok(result);
    }

    // ── DELETE /api/orders/{id} ──────────────────────────────────────────

    /// <summary>Annule une commande (exercice 2)</summary>
    /// <response code="204">Commande annulée</response>
    /// <response code="400">Annulation impossible (état terminal)</response>
    /// <response code="404">Commande introuvable</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        try
        {
            var command = new CancelOrderCommand(id);
            await _cancelHandler.HandleAsync(command);
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

    /// <summary>Calcule le montant restant après remboursement partiel (exercice 5)</summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateRefund(Guid id, [FromBody] RefundRequest request)
    {
        try
        {
            var query  = new CalculateRefundQuery(id, request.Amount, request.Currency);
            var result = await _refundHandler.HandleAsync(query);
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
