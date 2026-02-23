namespace ShopFlow.ProductService.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFlow.ProductService.Configuration;
using ShopFlow.ProductService.Data;

/// <summary>
/// Contrôleur produits avec simulation de latence configurable.
/// LAB 4 — Étape 5 : Simulation de latence pour tester les pipelines Polly
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext           _context;
    private readonly LatencySimulationConfig    _simulation;
    private readonly ILogger<ProductsController> _logger;
    private static readonly Random _rng = new();

    public ProductsController(ProductDbContext context, LatencySimulationConfig simulation, ILogger<ProductsController> logger)
    {
        _context    = context;
        _simulation = simulation;
        _logger     = logger;
    }

    // ── GET /api/products/{productId} ─────────────────────────────────

    /// <summary>Récupère les détails d'un produit (avec latence simulable)</summary>
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        await SimulateLatencyAsync(cancellationToken);

        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product is null)
            return NotFound(new { error = $"Produit {productId} introuvable." });

        return Ok(new { id = product.Id, name = product.Name, price = product.Price, currency = product.Currency });
    }

    // ── GET /api/products/{productId}/stock ───────────────────────────

    /// <summary>Récupère le stock disponible d'un produit</summary>
    [HttpGet("{productId:guid}/stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStock(Guid productId, CancellationToken cancellationToken)
    {
        await SimulateLatencyAsync(cancellationToken);

        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product is null)
            return NotFound(new { error = $"Produit {productId} introuvable." });

        return Ok(new { productId = product.Id, available = product.Available, reserved = product.Reserved });
    }

    // ── PUT /api/products/{productId}/stock/reserve ───────────────────

    /// <summary>Réserve du stock pour une commande</summary>
    [HttpPut("{productId:guid}/stock/reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReserveStock(Guid productId, [FromBody] ReserveStockRequest request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product is null)
            return NotFound(new { error = $"Produit {productId} introuvable." });

        if (product.Available < request.Quantity)
            return BadRequest(new { error = $"Stock insuffisant. Disponible : {product.Available}, demandé : {request.Quantity}." });

        product.Reserved += request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { productId = product.Id, reserved = request.Quantity, newAvailable = product.Available });
    }

    // ── GET /api/products ─────────────────────────────────────────────

    /// <summary>Liste tous les produits</summary>
    [HttpGet]
    public async Task<IActionResult> ListProducts(CancellationToken cancellationToken)
    {
        var products = await _context.Products.AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Price, p.Currency, available = p.Stock - p.Reserved, p.Category })
            .ToListAsync(cancellationToken);
        return Ok(products);
    }

    // ── POST /api/products/simulation ─────────────────────────────────

    /// <summary>
    /// Configure la simulation de latence à chaud.
    /// LAB 4 — Étape 5 : tester Timeout / Retry sans redémarrer le service
    ///
    /// Exemple pour déclencher des timeouts (délai > 3s = timeout Polly):
    /// { "enabled": true, "minDelaySeconds": 4, "maxDelaySeconds": 6 }
    /// </summary>
    [HttpPost("simulation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ConfigureSimulation([FromBody] LatencySimulationConfig config)
    {
        _simulation.Enabled          = config.Enabled;
        _simulation.MinDelaySeconds  = config.MinDelaySeconds;
        _simulation.MaxDelaySeconds  = config.MaxDelaySeconds;
        _simulation.FailureRate      = config.FailureRate;
        _simulation.IntermittentMode = config.IntermittentMode;

        _logger.LogInformation(
            "⚙️ Simulation configurée : Enabled={Enabled}, Delay={Min}-{Max}s, FailureRate={Rate}",
            config.Enabled, config.MinDelaySeconds, config.MaxDelaySeconds, config.FailureRate);

        return Ok(new
        {
            message = "Configuration de simulation mise à jour.",
            config  = _simulation
        });
    }

    // ── Simulation de latence ─────────────────────────────────────────

    private async Task SimulateLatencyAsync(CancellationToken cancellationToken)
    {
        if (!_simulation.Enabled) return;

        // Simulation d'échec aléatoire
        if (_simulation.FailureRate > 0 && _rng.NextDouble() < _simulation.FailureRate)
        {
            _logger.LogWarning("💥 [SIMULATION] Échec forcé (taux d'échec : {Rate:P0})", _simulation.FailureRate);
            throw new Exception("Erreur simulée pour les tests de résilience");
        }

        // Mode intermittent : une requête sur deux échoue
        if (_simulation.IntermittentMode && DateTime.UtcNow.Second % 2 == 0)
        {
            _logger.LogWarning("💥 [SIMULATION] Échec intermittent");
            Response.StatusCode = 500;
            await Response.WriteAsJsonAsync(new { error = "Erreur intermittente simulée" }, cancellationToken);
            return;
        }

        // Délai simulé (supérieur au timeout Polly pour déclencher TimeoutRejectedException)
        var delay = _rng.Next(_simulation.MinDelaySeconds * 1000, _simulation.MaxDelaySeconds * 1000);
        _logger.LogWarning("🐌 [SIMULATION] Délai artificiel : {Delay}ms", delay);
        await Task.Delay(delay, cancellationToken);
    }
}

public record ReserveStockRequest(int Quantity);
