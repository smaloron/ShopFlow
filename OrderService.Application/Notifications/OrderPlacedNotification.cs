namespace OrderService.Application.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

public record OrderPlacedNotification(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Currency) : INotification;

public class LogOrderPlacedHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly Microsoft.Extensions.Logging.ILogger<LogOrderPlacedHandler> _logger;
    public LogOrderPlacedHandler(Microsoft.Extensions.Logging.ILogger<LogOrderPlacedHandler> logger) => _logger = logger;

    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📦 Nouvelle commande {OrderId} — Client {CustomerId} — {Amount} {Currency}",
            notification.OrderId, notification.CustomerId, notification.TotalAmount, notification.Currency);
        return Task.CompletedTask;
    }
}

public class SendConfirmationEmailHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly Microsoft.Extensions.Logging.ILogger<SendConfirmationEmailHandler> _logger;
    public SendConfirmationEmailHandler(Microsoft.Extensions.Logging.ILogger<SendConfirmationEmailHandler> logger) => _logger = logger;

    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📧 [SIMULATION] Email de confirmation envoyé pour commande {OrderId}", notification.OrderId);
        return Task.CompletedTask;
    }
}
