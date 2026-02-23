namespace OrderService.Presentation.Resilience;

using System.Collections.Concurrent;

/// <summary>
/// Service Singleton de suivi des métriques de résilience Polly.
///
/// LAB 4 — Exercice 4 : Métriques
///
/// THREAD-SAFETY :
/// Les pipelines Polly s'exécutent sur des threads du pool ASP.NET Core.
/// Sans synchronisation, deux incréments simultanés pourraient se "perdre"
/// (race condition). Solutions utilisées :
/// - ConcurrentDictionary.AddOrUpdate : opération atomique sur le dictionnaire
/// - Interlocked.Increment : incrément atomique 64 bits sans verrou explicite
/// </summary>
public sealed class ResilienceMetrics
{
    // Compteurs de retries par service (ex: "product" → 4 retries)
    private readonly ConcurrentDictionary<string, long> _retries = new();

    // Compteurs d'ouvertures de circuit breaker par service
    private readonly ConcurrentDictionary<string, long> _circuitBreakersOpened = new();

    // Compteur total d'activations du fallback produit
    private long _fallbackActivations;

    // ── Méthodes d'enregistrement ──────────────────────────────────────

    /// <summary>Incrémente le compteur de retries pour un service</summary>
    public void IncrementRetry(string service)
        => _retries.AddOrUpdate(service, 1, (_, current) => Interlocked.Increment(ref current));

    /// <summary>Incrémente le compteur d'ouvertures de circuit breaker pour un service</summary>
    public void IncrementCircuitOpen(string service)
        => _circuitBreakersOpened.AddOrUpdate(service, 1, (_, current) => Interlocked.Increment(ref current));

    /// <summary>Incrémente le compteur d'activations du fallback</summary>
    public void IncrementFallback()
        => Interlocked.Increment(ref _fallbackActivations);

    // ── Snapshot ────────────────────────────────────────────────────────

    /// <summary>Retourne un snapshot immuable des métriques actuelles</summary>
    public ResilienceMetricsSnapshot GetSnapshot() => new(
        Timestamp: DateTime.UtcNow,
        Retries: new Dictionary<string, long>(_retries),
        CircuitBreakersOpened: new Dictionary<string, long>(_circuitBreakersOpened),
        FallbackActivations: Interlocked.Read(ref _fallbackActivations)
    );

    /// <summary>Remet tous les compteurs à zéro (utile pour les tests)</summary>
    public void Reset()
    {
        _retries.Clear();
        _circuitBreakersOpened.Clear();
        Interlocked.Exchange(ref _fallbackActivations, 0);
        Console.WriteLine("[METRICS] Compteurs remis à zéro");
    }
}

/// <summary>Snapshot immuable des métriques — retourné par l'API</summary>
public record ResilienceMetricsSnapshot(
    DateTime                 Timestamp,
    Dictionary<string, long> Retries,
    Dictionary<string, long> CircuitBreakersOpened,
    long                     FallbackActivations
);
