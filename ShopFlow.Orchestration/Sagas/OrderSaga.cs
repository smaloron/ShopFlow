namespace ShopFlow.Orchestration.Sagas;

using MassTransit;
using ShopFlow.Contracts.Events;

/// <summary>
/// Saga d'orchestration du processus de commande ShopFlow.
/// 
/// États :
///   Initial → PaymentPending → Completed (succès)
///                            → Cancelled (échec ou timeout)
/// 
/// La State Machine persiste chaque transition en base (SQLite via EF Core),
/// ce qui garantit la reprise après crash.
/// </summary>
public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public OrderSaga()
    {
        // ── Propriété qui stocke l'état courant ───────────────────────
        InstanceState(x => x.CurrentState);

        // ── Corrélation des événements sur OrderId ────────────────────
        // MassTransit retrouve l'instance via CorrelationId == Message.OrderId
        Event(() => OrderPlaced,      x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailed,    x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // ── Timer de timeout : annule si paiement > 5 min ────────────
        Schedule(() => PaymentTimeout, x => x.PaymentTimeoutTokenId, s =>
        {
            s.Delay    = TimeSpan.FromMinutes(5);
            s.Received = r => r.CorrelateById(ctx => ctx.Message.OrderId);
        });

        // ─────────────────────────────────────────────────────────────
        // Initial → PaymentPending
        // Déclenché par OrderPlaced
        // ─────────────────────────────────────────────────────────────
        Initially(
            When(OrderPlaced)
                .Then(ctx =>
                {
                    ctx.Saga.CustomerId  = ctx.Message.CustomerId;
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.OrderDate   = ctx.Message.OrderDate;
                    ctx.Saga.CreatedAt   = DateTime.UtcNow;

                    Console.WriteLine(
                        $"[SAGA] 📥 OrderPlaced reçu — Commande {ctx.Saga.CorrelationId} " +
                        $"— Montant {ctx.Message.TotalAmount:C}");
                })
                // Demande le paiement au PaymentService
                .PublishAsync(ctx => ctx.Init<PaymentRequested>(new
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerId    = ctx.Message.CustomerId,
                    Amount        = ctx.Message.TotalAmount,
                    PaymentMethod = "CreditCard",
                    RequestedAt   = DateTime.UtcNow
                }))
                // Arme le timer de 5 minutes
                .Schedule(PaymentTimeout, ctx => new PaymentTimeoutExpired
                {
                    OrderId = ctx.Saga.CorrelationId
                })
                .TransitionTo(PaymentPending)
                .Then(ctx => Console.WriteLine(
                    $"[SAGA] ⏳ → PaymentPending pour commande {ctx.Saga.CorrelationId}"))
        );

        // ─────────────────────────────────────────────────────────────
        // PaymentPending → Completed  (paiement accepté)
        // PaymentPending → Cancelled  (paiement refusé ou timeout)
        // ─────────────────────────────────────────────────────────────
        During(PaymentPending,

            // ✅ Paiement réussi
            When(PaymentCompleted)
                .Unschedule(PaymentTimeout)   // annule le timer
                .Then(ctx =>
                {
                    ctx.Saga.PaymentId     = ctx.Message.PaymentId;
                    ctx.Saga.TransactionId = ctx.Message.TransactionId;
                    ctx.Saga.UpdatedAt     = DateTime.UtcNow;

                    Console.WriteLine(
                        $"[SAGA] ✅ Paiement accepté — Commande {ctx.Saga.CorrelationId} " +
                        $"— Transaction {ctx.Message.TransactionId}");
                })
                .PublishAsync(ctx => ctx.Init<OrderConfirmed>(new
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerId    = ctx.Saga.CustomerId,       // exercice 5
                    TransactionId = ctx.Saga.TransactionId,    // exercice 5
                    ConfirmedAt   = DateTime.UtcNow
                }))
                .TransitionTo(Completed)
                .Finalize(),

            // ❌ Paiement refusé
            When(PaymentFailed)
                .Unschedule(PaymentTimeout)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt     = DateTime.UtcNow;

                    Console.WriteLine(
                        $"[SAGA] ❌ Paiement refusé — Commande {ctx.Saga.CorrelationId} " +
                        $"— Raison : {ctx.Message.Reason}");
                })
                .PublishAsync(ctx => ctx.Init<OrderCancelled>(new
                {
                    OrderId     = ctx.Saga.CorrelationId,
                    Reason      = $"Paiement refusé : {ctx.Message.Reason}",
                    CancelledAt = DateTime.UtcNow
                }))
                .TransitionTo(Cancelled)
                .Finalize(),

            // ⏰ Timeout : 5 minutes sans réponse
            When(PaymentTimeout.Received)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = "Timeout : aucune réponse du paiement après 5 minutes";
                    ctx.Saga.UpdatedAt     = DateTime.UtcNow;

                    Console.WriteLine(
                        $"[SAGA] ⏰ Timeout paiement — Commande {ctx.Saga.CorrelationId}");
                })
                .PublishAsync(ctx => ctx.Init<OrderCancelled>(new
                {
                    OrderId     = ctx.Saga.CorrelationId,
                    Reason      = "Timeout : paiement non reçu dans les 5 minutes",
                    CancelledAt = DateTime.UtcNow
                }))
                .TransitionTo(Cancelled)
                .Finalize()
        );

        // Supprime l'instance en base quand la Saga se termine
        SetCompletedWhenFinalized();
    }

    // ── États ─────────────────────────────────────────────────────────
    public State PaymentPending { get; private set; } = null!;
    public State Completed      { get; private set; } = null!;
    public State Cancelled      { get; private set; } = null!;

    // ── Événements ────────────────────────────────────────────────────
    public Event<OrderPlaced>      OrderPlaced      { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailed>    PaymentFailed    { get; private set; } = null!;

    // ── Schedule (timeout) ────────────────────────────────────────────
    public Schedule<OrderSagaState, PaymentTimeoutExpired> PaymentTimeout { get; private set; } = null!;
}

/// <summary>Message interne publié par le scheduler après 5 minutes</summary>
public record PaymentTimeoutExpired
{
    public Guid OrderId { get; init; }
}
