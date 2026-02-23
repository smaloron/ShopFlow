namespace OrderService.Application.Notifications;

using MediatR;

/// <summary>
/// Notification MediatR publiée après confirmation d'une commande.
/// Plusieurs handlers peuvent réagir (email, log, stock, analytics...).
/// </summary>
public record OrderPlacedNotification(
    Guid   OrderId,
    Guid   CustomerId,
    decimal TotalAmount,
    string Currency
) : INotification;

// ── Handler 1 : Log ────────────────────────────────────────────────────

/// <summary>Journalise chaque nouvelle commande</summary>
public class LogOrderPlacedHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly Microsoft.Extensions.Logging.ILogger<LogOrderPlacedHandler> _logger;

    public LogOrderPlacedHandler(Microsoft.Extensions.Logging.ILogger<LogOrderPlacedHandler> logger)
        => _logger = logger;

    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📦 Nouvelle commande {OrderId} — Client {CustomerId} — {Amount} {Currency}",
            notification.OrderId,
            notification.CustomerId,
            notification.TotalAmount,
            notification.Currency);

        return Task.CompletedTask;
    }
}

// ── Handler 2 : Email (simulé) ─────────────────────────────────────────

/// <summary>Simule l'envoi d'un email de confirmation</summary>
public class SendConfirmationEmailHandler : INotificationHandler<OrderPlacedNotification>
{
    private readonly Microsoft.Extensions.Logging.ILogger<SendConfirmationEmailHandler> _logger;

    public SendConfirmationEmailHandler(
        Microsoft.Extensions.Logging.ILogger<SendConfirmationEmailHandler> logger)
        => _logger = logger;

    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        // TODO : intégrer un vrai service d'email (SendGrid, SMTP, etc.)
        _logger.LogInformation(
            "📧 [SIMULATION] Email de confirmation envoyé pour commande {OrderId}",
            notification.OrderId);

        return Task.CompletedTask;
    }
}
