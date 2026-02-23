namespace OrderService.Presentation.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Presentation.Infrastructure;

/// <summary>
/// Health Check agrégé avec dégradation partielle.
///
/// LAB 4 — Exercice 5 : Dégradation partielle
///
/// Logique de dégradation :
/// | Scénario                              | Statut    | Impact Kubernetes         |
/// |---------------------------------------|-----------|---------------------------|
/// | Tout disponible                       | Healthy   | Trafic normal             |
/// | Product indispo, cache disponible     | Degraded  | Trafic maintenu           |
/// | Product indispo, cache vide           | Unhealthy | Pod exclu du load balancer|
/// | Payment indispo                       | Unhealthy | Pod exclu du load balancer|
///
/// Pourquoi cette nuance ?
/// En Kubernetes, un pod en état Degraded reste dans la rotation du load balancer.
/// Un pod Unhealthy en est exclu. Exclure un pod qui fonctionne en mode dégradé
/// acceptable serait contre-productif (moins de capacité pour rien).
/// </summary>
public class DependenciesHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DependenciesHealthCheck> _logger;

    public DependenciesHealthCheck(IConfiguration configuration, ILogger<DependenciesHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Lire les URLs depuis la configuration pour rester cohérent avec appsettings.json
        var productServiceUrl = _configuration["Services:ProductService"] ?? "http://localhost:3002";
        var paymentGrpcUrl    = _configuration["Services:PaymentGrpc"]    ?? "http://localhost:5001";

        var productAvailable = await CheckServiceAsync($"{productServiceUrl}/health", cancellationToken);
        var paymentAvailable = await CheckServiceAsync($"{paymentGrpcUrl}/health", cancellationToken);

        // ── Cas 1 : tout fonctionne ──────────────────────────────────────────
        if (productAvailable && paymentAvailable)
        {
            return HealthCheckResult.Healthy(
                "Tous les services dépendants sont disponibles.",
                data: BuildData(productAvailable, paymentAvailable));
        }

        // ── Cas 2 : Payment indisponible → Unhealthy (pas de fallback) ───────
        if (!paymentAvailable)
        {
            _logger.LogError("Health check: Payment Service indisponible — aucun fallback disponible.");
            return HealthCheckResult.Unhealthy(
                "Payment Service indisponible. Les commandes ne peuvent pas être traitées.",
                data: BuildData(productAvailable, paymentAvailable));
        }

        // ── Cas 3 : Product indisponible + cache disponible → Degraded ───────
        var cachedProducts = ProductFallbackData.GetAllCachedProducts();

        if (!productAvailable && cachedProducts.Count > 0)
        {
            _logger.LogWarning(
                "Health check: Product Service indisponible — fallback cache actif avec {Count} produits.",
                cachedProducts.Count);

            return HealthCheckResult.Degraded(
                $"Product Service indisponible mais {cachedProducts.Count} produits disponibles en cache. " +
                "Fonctionnement en mode dégradé.",
                data: BuildData(productAvailable, paymentAvailable, cacheCount: cachedProducts.Count));
        }

        // ── Cas 4 : Product indisponible + cache vide → Unhealthy ────────────
        return HealthCheckResult.Unhealthy(
            "Product Service indisponible et cache vide. Impossible de répondre aux requêtes produits.",
            data: BuildData(productAvailable, paymentAvailable, cacheCount: 0));
    }

    private static async Task<bool> CheckServiceAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            // Timeout court : le health check ne doit pas bloquer longtemps
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await client.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, object> BuildData(bool productOk, bool paymentOk, int cacheCount = -1)
    {
        var data = new Dictionary<string, object>
        {
            ["productServiceAvailable"] = productOk,
            ["paymentServiceAvailable"] = paymentOk,
            ["checkedAt"]               = DateTime.UtcNow
        };
        if (cacheCount >= 0) data["cachedProductsCount"] = cacheCount;
        return data;
    }
}
