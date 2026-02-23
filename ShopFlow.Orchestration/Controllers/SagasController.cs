namespace ShopFlow.Orchestration.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Orchestration.Data;

/// <summary>
/// Endpoints de consultation de l'état des Sagas.
/// Utile pour le debugging et le monitoring en production.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SagasController : ControllerBase
{
    private readonly SagaDbContext _db;

    public SagasController(SagaDbContext db) => _db = db;

    // ── GET /api/sagas/{orderId} ──────────────────────────────────────

    /// <summary>Retourne l'état actuel d'une Saga par OrderId</summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSagaState(Guid orderId)
    {
        var state = await _db.Set<Sagas.OrderSagaState>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == orderId);

        if (state is null)
            return NotFound(new { error = $"Aucune Saga trouvée pour la commande {orderId}." });

        return Ok(new
        {
            orderId       = state.CorrelationId,
            currentState  = state.CurrentState,
            customerId    = state.CustomerId,
            totalAmount   = state.TotalAmount,
            paymentId     = state.PaymentId,
            transactionId = state.TransactionId,
            failureReason = state.FailureReason,
            createdAt     = state.CreatedAt,
            updatedAt     = state.UpdatedAt
        });
    }

    // ── GET /api/sagas ────────────────────────────────────────────────

    /// <summary>Liste les 20 dernières Sagas (utile en développement)</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSagas([FromQuery] int take = 20)
    {
        var states = await _db.Set<Sagas.OrderSagaState>()
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(Math.Min(take, 100))
            .Select(s => new
            {
                orderId      = s.CorrelationId,
                currentState = s.CurrentState,
                totalAmount  = s.TotalAmount,
                createdAt    = s.CreatedAt,
                failureReason = s.FailureReason
            })
            .ToListAsync();

        return Ok(states);
    }
}
