namespace ShopFlow.ProductService.Configuration;

/// <summary>
/// Configuration de la simulation de latence.
/// LAB 4 — Étape 5 : Simulation de latence pour tester Timeout/Retry
///
/// Activez depuis appsettings.Development.json ou via l'endpoint POST /api/products/simulation
/// </summary>
public class LatencySimulationConfig
{
    /// <summary>Active/désactive la simulation. False par défaut.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Délai minimum en secondes (doit être > timeout Polly pour déclencher TimeoutRejectedException)</summary>
    public int MinDelaySeconds { get; set; } = 4;

    /// <summary>Délai maximum en secondes</summary>
    public int MaxDelaySeconds { get; set; } = 6;

    /// <summary>Taux d'échec 0.0–1.0 (0.3 = 30% des requêtes retournent 500)</summary>
    public double FailureRate { get; set; } = 0.0;

    /// <summary>Mode intermittent : alterne succès/échec au lieu d'un délai fixe</summary>
    public bool IntermittentMode { get; set; } = false;
}
