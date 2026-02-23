namespace ShopFlow.PaymentService.Consumers;
using MassTransit;
using ShopFlow.Contracts.Events;

public class PaymentRequestedConsumer : IConsumer<PaymentRequested>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PaymentRequestedConsumer> _logger;

    public PaymentRequestedConsumer(IPublishEndpoint publishEndpoint, ILogger<PaymentRequestedConsumer> logger)
    { _publishEndpoint = publishEndpoint; _logger = logger; }

    public async Task Consume(ConsumeContext<PaymentRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("💳 Traitement paiement — Commande {OrderId} — {Amount:C}", msg.OrderId, msg.Amount);
        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);

        if (msg.Amount > 1000m)
        {
            _logger.LogWarning("❌ Paiement refusé — Montant {Amount:C} dépasse la limite", msg.Amount);
            await _publishEndpoint.Publish(new PaymentFailed { OrderId = msg.OrderId, Reason = $"Montant {msg.Amount:C} dépasse la limite de 1 000 €", ErrorCode = "AMOUNT_TOO_HIGH", FailedAt = DateTime.UtcNow });
            return;
        }

        if (new Random().Next(100) < 10)
        {
            _logger.LogWarning("❌ Paiement refusé — Erreur passerelle simulée");
            await _publishEndpoint.Publish(new PaymentFailed { OrderId = msg.OrderId, Reason = "Erreur temporaire de la passerelle de paiement", ErrorCode = "GATEWAY_ERROR", FailedAt = DateTime.UtcNow });
            return;
        }

        var paymentId     = Guid.NewGuid();
        var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{paymentId.ToString()[..8].ToUpper()}";
        _logger.LogInformation("✅ Paiement accepté — Transaction {TransactionId}", transactionId);
        await _publishEndpoint.Publish(new PaymentCompleted { OrderId = msg.OrderId, PaymentId = paymentId, Amount = msg.Amount, TransactionId = transactionId, CompletedAt = DateTime.UtcNow });
    }
}
