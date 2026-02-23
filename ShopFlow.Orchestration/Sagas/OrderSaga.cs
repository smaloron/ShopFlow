namespace ShopFlow.Orchestration.Sagas;
using MassTransit;
using ShopFlow.Contracts.Events;

public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public OrderSaga()
    {
        InstanceState(x => x.CurrentState);
        Event(() => OrderPlaced,      x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailed,    x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Schedule(() => PaymentTimeout, x => x.PaymentTimeoutTokenId, s => { s.Delay = TimeSpan.FromMinutes(5); s.Received = r => r.CorrelateById(ctx => ctx.Message.OrderId); });

        Initially(
            When(OrderPlaced)
                .Then(ctx => { ctx.Saga.CustomerId = ctx.Message.CustomerId; ctx.Saga.TotalAmount = ctx.Message.TotalAmount; ctx.Saga.OrderDate = ctx.Message.OrderDate; ctx.Saga.CreatedAt = DateTime.UtcNow; Console.WriteLine($"[SAGA] 📥 OrderPlaced — Commande {ctx.Saga.CorrelationId}"); })
                .PublishAsync(ctx => ctx.Init<PaymentRequested>(new { OrderId = ctx.Saga.CorrelationId, CustomerId = ctx.Message.CustomerId, Amount = ctx.Message.TotalAmount, PaymentMethod = "CreditCard", RequestedAt = DateTime.UtcNow }))
                .Schedule(PaymentTimeout, ctx => new PaymentTimeoutExpired { OrderId = ctx.Saga.CorrelationId })
                .TransitionTo(PaymentPending)
                .Then(ctx => Console.WriteLine($"[SAGA] ⏳ → PaymentPending pour commande {ctx.Saga.CorrelationId}"))
        );

        During(PaymentPending,
            When(PaymentCompleted)
                .Unschedule(PaymentTimeout)
                .Then(ctx => { ctx.Saga.PaymentId = ctx.Message.PaymentId; ctx.Saga.TransactionId = ctx.Message.TransactionId; ctx.Saga.UpdatedAt = DateTime.UtcNow; Console.WriteLine($"[SAGA] ✅ Paiement accepté — {ctx.Saga.CorrelationId} — {ctx.Message.TransactionId}"); })
                .PublishAsync(ctx => ctx.Init<OrderConfirmed>(new { OrderId = ctx.Saga.CorrelationId, CustomerId = ctx.Saga.CustomerId, TransactionId = ctx.Saga.TransactionId, ConfirmedAt = DateTime.UtcNow }))
                .TransitionTo(Completed).Finalize(),

            When(PaymentFailed)
                .Unschedule(PaymentTimeout)
                .Then(ctx => { ctx.Saga.FailureReason = ctx.Message.Reason; ctx.Saga.UpdatedAt = DateTime.UtcNow; Console.WriteLine($"[SAGA] ❌ Paiement refusé — {ctx.Saga.CorrelationId} — {ctx.Message.Reason}"); })
                .PublishAsync(ctx => ctx.Init<OrderCancelled>(new { OrderId = ctx.Saga.CorrelationId, Reason = $"Paiement refusé : {ctx.Message.Reason}", CancelledAt = DateTime.UtcNow }))
                .TransitionTo(Cancelled).Finalize(),

            When(PaymentTimeout.Received)
                .Then(ctx => { ctx.Saga.FailureReason = "Timeout : aucune réponse du paiement après 5 minutes"; ctx.Saga.UpdatedAt = DateTime.UtcNow; Console.WriteLine($"[SAGA] ⏰ Timeout paiement — {ctx.Saga.CorrelationId}"); })
                .PublishAsync(ctx => ctx.Init<OrderCancelled>(new { OrderId = ctx.Saga.CorrelationId, Reason = "Timeout paiement", CancelledAt = DateTime.UtcNow }))
                .TransitionTo(Cancelled).Finalize()
        );

        SetCompletedWhenFinalized();
    }

    public State PaymentPending { get; private set; } = null!;
    public State Completed      { get; private set; } = null!;
    public State Cancelled      { get; private set; } = null!;
    public Event<OrderPlaced>      OrderPlaced      { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailed>    PaymentFailed    { get; private set; } = null!;
    public Schedule<OrderSagaState, PaymentTimeoutExpired> PaymentTimeout { get; private set; } = null!;
}

public record PaymentTimeoutExpired { public Guid OrderId { get; init; } }
