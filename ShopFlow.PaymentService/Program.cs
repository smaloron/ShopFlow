using MassTransit;
using ShopFlow.PaymentService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// ── MassTransit + RabbitMQ + Consumer ────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentRequestedConsumer>();

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

        // ── Retry spécifique au consumer (correction exercice 4) ─────
        // Cross-cutting concern configuré en dehors du consumer
        // → même principe que ValidationBehavior dans MediatR
        cfg.ReceiveEndpoint("payment-requested", e =>
        {
            e.UseMessageRetry(r =>
            {
                r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5));

                // Log avant chaque retry
                r.Handle<Exception>(ex =>
                {
                    Console.WriteLine($"[RETRY] Paiement échoué : {ex.Message}. Nouvelle tentative...");
                    return true;
                });
            });

            e.ConfigureConsumer<PaymentRequestedConsumer>(context);
        });
    });
});

// ── API (minimal — ce service est principalement un worker) ───────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new()
    {
        Title   = "ShopFlow — Payment Service",
        Version = "v1",
        Description = "Consumer RabbitMQ pour PaymentRequested. Simule un PSP."
    });
});
builder.Services.AddHealthChecks();

// ── APP ───────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Console.WriteLine("✅ [PaymentService] En attente de messages PaymentRequested...");

app.Run();
