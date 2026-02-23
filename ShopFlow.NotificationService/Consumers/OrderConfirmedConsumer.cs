namespace ShopFlow.NotificationService.Consumers;

using MassTransit;
using ShopFlow.Contracts.Events;

/// <summary>
/// Consumer MassTransit — écoute OrderConfirmed et simule l'envoi d'un email.
///
/// Exercice 5 du Lab 3 : démontre le principe Open/Closed appliqué
/// à l'architecture événementielle.
///
/// Ce service s'est ajouté SANS toucher à la Saga ni au OrderService.
/// MassTransit crée automatiquement une nouvelle queue dédiée à ce consumer.
/// </summary>
public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger)
        => _logger = logger;

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var msg = context.Message;

        // Simulation d'envoi d'email (en production : SendGrid, SMTP, etc.)
        _logger.LogInformation(
            "📧 Email envoyé au client {CustomerId} pour la commande confirmée {OrderId}. " +
            "Transaction : {TransactionId}",
            msg.CustomerId,
            msg.OrderId,
            msg.TransactionId);

        // Simulation d'un léger délai d'envoi SMTP
        await Task.Delay(100, context.CancellationToken);

        _logger.LogInformation(
            "✅ Notification envoyée — Commande {OrderId} confirmée à {ConfirmedAt:HH:mm:ss}",
            msg.OrderId,
            msg.ConfirmedAt);
    }
}
