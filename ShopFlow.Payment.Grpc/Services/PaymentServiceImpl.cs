namespace ShopFlow.Payment.Grpc.Services;
using global::Grpc.Core;

public class PaymentServiceImpl : PaymentService.PaymentServiceBase
{
    private readonly ILogger<PaymentServiceImpl> _logger;
    public PaymentServiceImpl(ILogger<PaymentServiceImpl> logger) => _logger = logger;

    public override async Task<PaymentResponse> ProcessPayment(PaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation("💳 Traitement paiement — Commande {OrderId}, Montant {Amount} {Currency}", request.OrderId, request.Amount, request.Currency);
        if (string.IsNullOrWhiteSpace(request.OrderId)) throw new RpcException(new Status(StatusCode.InvalidArgument, "L'identifiant de commande est obligatoire."));
        if (request.Amount <= 0) throw new RpcException(new Status(StatusCode.InvalidArgument, "Le montant doit être positif."));
        if (request.Currency.Length != 3) throw new RpcException(new Status(StatusCode.InvalidArgument, "La devise doit être un code ISO 4217 (ex: EUR)."));
        await Task.Delay(200, context.CancellationToken);
        var transactionId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("✅ Paiement accepté — Transaction {TransactionId}", transactionId);
        return new PaymentResponse { Success = true, TransactionId = transactionId, Status = PaymentStatus.Completed };
    }

    public override async Task<PaymentResponse> RefundPayment(RefundRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionId)) throw new RpcException(new Status(StatusCode.InvalidArgument, "L'identifiant de transaction est obligatoire."));
        await Task.Delay(150, context.CancellationToken);
        return new PaymentResponse { Success = true, TransactionId = request.TransactionId, Status = PaymentStatus.Refunded };
    }
}
