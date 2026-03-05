namespace ShopFlow.Orchestration.Data;

using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFlow.Orchestration.Sagas;

/// <summary>
/// DbContext dédié à la persistance de l'état de la Saga.
/// Hérite de SagaDbContext (MassTransit) pour la configuration automatique.
/// </summary>
public class OrderSagaDbContext : SagaDbContext
{
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options)
        : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderSagaStateMap(); }
    }
}

/// <summary>
/// Configuration EF Core de l'entité OrderSagaState.
/// Définit les contraintes de longueur pour les colonnes string.
/// </summary>
public class OrderSagaStateMap : SagaClassMap<OrderSagaState>
{
    protected override void Configure(
        EntityTypeBuilder<OrderSagaState> entity,
        ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.TransactionId).HasMaxLength(128);
        entity.Property(x => x.FailureReason).HasMaxLength(500);
    }
}
