namespace ShopFlow.Orchestration.Sagas;
using MassTransit;

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid     CorrelationId { get; set; }
    public string   CurrentState  { get; set; } = string.Empty;
    public Guid     CustomerId    { get; set; }
    public decimal  TotalAmount   { get; set; }
    public DateTime OrderDate     { get; set; }
    public Guid?    PaymentId     { get; set; }
    public string?  TransactionId { get; set; }
    public DateTime  CreatedAt    { get; set; }
    public DateTime? UpdatedAt    { get; set; }
    public string?   FailureReason { get; set; }
    public Guid?     PaymentTimeoutTokenId { get; set; }
}
