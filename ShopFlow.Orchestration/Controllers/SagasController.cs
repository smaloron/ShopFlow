namespace ShopFlow.Orchestration.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Orchestration.Data;
using ShopFlow.Orchestration.Sagas;

[ApiController]
[Route("api/[controller]")]
public class SagasController : ControllerBase
{
    private readonly SagaDbContext _db;
    public SagasController(SagaDbContext db) => _db = db;

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetSagaState(Guid orderId)
    {
        var state = await _db.Set<OrderSagaState>().AsNoTracking().FirstOrDefaultAsync(s => s.CorrelationId == orderId);
        if (state is null) return NotFound(new { error = $"Aucune Saga pour {orderId}." });
        return Ok(new { orderId = state.CorrelationId, currentState = state.CurrentState, customerId = state.CustomerId, totalAmount = state.TotalAmount, transactionId = state.TransactionId, failureReason = state.FailureReason, createdAt = state.CreatedAt, updatedAt = state.UpdatedAt });
    }

    [HttpGet]
    public async Task<IActionResult> ListSagas([FromQuery] int take = 20)
    {
        var states = await _db.Set<OrderSagaState>().AsNoTracking().OrderByDescending(s => s.CreatedAt).Take(Math.Min(take, 100)).Select(s => new { orderId = s.CorrelationId, currentState = s.CurrentState, totalAmount = s.TotalAmount, createdAt = s.CreatedAt, failureReason = s.FailureReason }).ToListAsync();
        return Ok(states);
    }
}
