namespace OrderService.Application.Commands;
using MediatR;

public record ProcessPaymentCommand(Guid OrderId, decimal Amount, string Currency = "EUR") : IRequest<PaymentResult>;
public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage = null);
