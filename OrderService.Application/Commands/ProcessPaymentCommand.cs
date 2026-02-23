namespace OrderService.Application.Commands;

using MediatR;

// ── Command ───────────────────────────────────────────────────────────

/// <summary>Command pour traiter le paiement d'une commande via gRPC</summary>
public record ProcessPaymentCommand(
    Guid    OrderId,
    decimal Amount,
    string  Currency = "EUR"
) : IRequest<PaymentResult>;

/// <summary>Résultat du traitement de paiement</summary>
public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage = null);
