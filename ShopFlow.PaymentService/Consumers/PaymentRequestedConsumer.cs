namespace ShopFlow.PaymentService.Consumers;

using MassTransit;
using ShopFlow.Contracts.Events;

/// <summary>
/// Consumer MassTransit qui écoute la queue "payment-requested".
/// Simule le traitement par un PSP (Payment Service Provider).
/// 
/// Règles de simulation :
///   - Montant > 1000 € → PaymentFailed (AMOUNT_TOO_HIGH)
///   - 10 % de chance aléatoire → PaymentFailed (GATEWAY_ERROR)
///   - Sinon → PaymentCompleted
/// </summary>
public class PaymentRequestedConsumer : IConsumer<PaymentRequested>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PaymentRequestedConsumer> _logger;

    public PaymentRequestedConsumer(
        IPublishEndpoint                   publishEndpoint,
        ILogger<PaymentRequestedConsumer>  logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Consume(ConsumeContext<PaymentRequested> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "💳 Traitement paiement — Commande {OrderId} — {Amount:C} via {Method}",
            msg.OrderId, msg.Amount, msg.PaymentMethod);

        // Simuler un délai de traitement PSP (2 secondes)
        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);

        // ── Règle 1 : montant trop élevé ─────────────────────────────
        if (msg.Amount > 1000m)
        {
            _logger.LogWarning(
                "❌ Paiement refusé — Commande {OrderId} — Montant {Amount:C} dépasse la limite",
                msg.OrderId, msg.Amount);

            await _publishEndpoint.Publish(new PaymentFailed
            {
                OrderId   = msg.OrderId,
                Reason    = $"Montant {msg.Amount:C} dépasse la limite autorisée de 1 000 €",
                ErrorCode = "AMOUNT_TOO_HIGH",
                FailedAt  = DateTime.UtcNow
            });
            return;
        }

        // ── Règle 2 : échec aléatoire (10 % de chance) ───────────────
        if (new Random().Next(100) < 10)
        {
            _logger.LogWarning(
                "❌ Paiement refusé — Commande {OrderId} — Erreur passerelle simulée",
                msg.OrderId);

            await _publishEndpoint.Publish(new PaymentFailed
            {
                OrderId   = msg.OrderId,
                Reason    = "Erreur temporaire de la passerelle de paiement",
                ErrorCode = "GATEWAY_ERROR",
                FailedAt  = DateTime.UtcNow
            });
            return;
        }

        // ── Paiement accepté ─────────────────────────────────────────
        var paymentId     = Guid.NewGuid();
        var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{paymentId.ToString()[..8].ToUpper()}";

        _logger.LogInformation(
            "✅ Paiement accepté — Commande {OrderId} — Transaction {TransactionId}",
            msg.OrderId, transactionId);

        await _publishEndpoint.Publish(new PaymentCompleted
        {
            OrderId       = msg.OrderId,
            PaymentId     = paymentId,
            Amount        = msg.Amount,
            TransactionId = transactionId,
            CompletedAt   = DateTime.UtcNow
        });
    }
}
