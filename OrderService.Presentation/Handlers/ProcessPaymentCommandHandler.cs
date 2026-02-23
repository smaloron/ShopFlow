namespace OrderService.Presentation.Handlers;

using Grpc.Core;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Domain.Repositories;
using ShopFlow.Payment.Grpc;

/// <summary>
/// Handler MediatR pour ProcessPaymentCommand.
/// Utilise le client gRPC auto-généré pour appeler le Payment Service.
/// Placé dans Presentation car il dépend du stub gRPC généré.
/// </summary>
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResult>
{
    private readonly PaymentService.PaymentServiceClient _paymentClient;
    private readonly IOrderRepository                    _orderRepository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        PaymentService.PaymentServiceClient    paymentClient,
        IOrderRepository                       orderRepository,
        ILogger<ProcessPaymentCommandHandler>  logger)
    {
        _paymentClient   = paymentClient;
        _orderRepository = orderRepository;
        _logger          = logger;
    }

    public async Task<PaymentResult> Handle(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "💳 Envoi paiement gRPC — Commande {OrderId} — {Amount} {Currency}",
            command.OrderId, command.Amount, command.Currency);

        try
        {
            var grpcRequest = new PaymentRequest
            {
                OrderId  = command.OrderId.ToString(),
                Amount   = (double)command.Amount,
                Currency = command.Currency,
                Method   = "CARD"
            };

            var response = await _paymentClient.ProcessPaymentAsync(
                grpcRequest, cancellationToken: cancellationToken);

            if (response.Success)
            {
                // Marquer la commande comme payée dans le Domain
                var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
                if (order != null)
                {
                    order.MarkAsPaid();
                    await _orderRepository.UpdateAsync(order, cancellationToken);
                    await _orderRepository.SaveChangesAsync(cancellationToken);
                }
            }

            return new PaymentResult(response.Success, response.TransactionId, response.ErrorMessage);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError("❌ Payment Service indisponible : {Detail}", ex.Status.Detail);
            throw new InvalidOperationException(
                "Le service de paiement est temporairement indisponible. Réessayez dans un instant.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException($"Données de paiement invalides : {ex.Status.Detail}");
        }
    }
}
