using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Orchestration.Data;
using ShopFlow.Orchestration.Sagas;

var builder = WebApplication.CreateBuilder(args);

// ── Base de données pour l'état de la Saga ────────────────────────────
builder.Services.AddDbContext<OrderSagaDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("SagaDb") ?? "Data Source=saga.db"));

// ── MassTransit + Saga + RabbitMQ ─────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Enregistrement de la State Machine + persistance EF Core
    x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderSagaDbContext>();
            r.UseSqlite();

            // Optimistic concurrency pour éviter les doubles traitements
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        var host     = builder.Configuration["RabbitMQ:Host"]     ?? "localhost";
        var user     = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(password);
        });

        // Retry policy pour la Saga (transitoire)
        cfg.UseMessageRetry(r =>
        {
            r.Intervals(
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2));
        });

        // Configure automatiquement les queues pour la Saga
        cfg.ConfigureEndpoints(context);
    });
});

// ── API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new()
    {
        Title       = "ShopFlow — Orchestration Service",
        Version     = "v1",
        Description = "State Machine Saga — interroger l'état des commandes en cours"
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderSagaDbContext>();

// ── APP ───────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Créer la base de données au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("[Orchestration] Base de données Saga prête");
}

app.Run();
