namespace ShopFlow.NotificationService.Consumers;
using MassTransit;
using ShopFlow.Contracts.Events;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;
    public OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger) => _logger = logger;

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var msg = context.Message;
        _logger.LogInformation("📧 Email envoyé au client {CustomerId} pour la commande {OrderId}. Transaction : {TransactionId}", msg.CustomerId, msg.OrderId, msg.TransactionId);
        await Task.Delay(100, context.CancellationToken);
        _logger.LogInformation("✅ Notification envoyée — Commande {OrderId} confirmée à {ConfirmedAt:HH:mm:ss}", msg.OrderId, msg.ConfirmedAt);
    }
}
