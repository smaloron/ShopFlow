namespace OrderService.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderService.Presentation.Resilience;

/// <summary>
/// Expose les métriques de résilience Polly.
/// LAB 4 — Exercice 4 : Métriques de résilience
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ResilienceMetrics _metrics;

    public MetricsController(ResilienceMetrics metrics) => _metrics = metrics;

    /// <summary>
    /// Retourne un snapshot des métriques de résilience en temps réel.
    /// Inclut : nombre de retries, ouvertures de circuit breaker, activations du fallback.
    /// </summary>
    /// <response code="200">Snapshot des métriques</response>
    [HttpGet("resilience")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetResilienceMetrics()
        => Ok(_metrics.GetSnapshot());

    /// <summary>Remet les compteurs à zéro (utile entre les tests)</summary>
    [HttpDelete("resilience")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetMetrics()
    {
        _metrics.Reset();
        return NoContent();
    }
}
