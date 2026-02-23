namespace ShopFlow.Orchestration.Sagas;

using MassTransit;

/// <summary>
/// État persisté en base de données pour chaque instance de la Saga.
/// Une instance = une commande en cours de traitement.
/// 
/// CorrelationId = OrderId → clé primaire utilisée par MassTransit
/// pour retrouver la bonne instance lors de la réception d'un événement.
/// </summary>
public class OrderSagaState : SagaStateMachineInstance
{
    /// <summary>Clé primaire = OrderId (CorrelationId MassTransit)</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>État courant sérialisé en chaîne : "Initial", "PaymentPending", "Completed", "Cancelled"</summary>
    public string CurrentState { get; set; } = string.Empty;

    // ── Données de la commande ────────────────────────────────────────
    public Guid     CustomerId  { get; set; }
    public decimal  TotalAmount { get; set; }
    public DateTime OrderDate   { get; set; }

    // ── Données du paiement (renseignées après PaymentCompleted) ─────
    public Guid?   PaymentId     { get; set; }
    public string? TransactionId { get; set; }

    // ── Métadonnées ────────────────────────────────────────────────────
    public DateTime  CreatedAt     { get; set; }
    public DateTime? UpdatedAt     { get; set; }
    public string?   FailureReason { get; set; }

    /// <summary>Token du timer de timeout paiement (5 min)</summary>
    public Guid? PaymentTimeoutTokenId { get; set; }
}
