namespace ShopFlow.Orchestration.Data;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Orchestration.Sagas;

public class SagaDbContext : SagaDbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }
    protected override IEnumerable<ISagaClassMap> Configurations { get { yield return new OrderSagaStateMap(); } }
}

public class OrderSagaStateMap : SagaClassMap<OrderSagaState>
{
    protected override void Configure(EntityTypeBuilder<OrderSagaState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.TransactionId).HasMaxLength(128);
        entity.Property(x => x.FailureReason).HasMaxLength(500);
    }
}
