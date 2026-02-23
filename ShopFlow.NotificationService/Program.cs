using MassTransit;
using ShopFlow.NotificationService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// ── MassTransit + RabbitMQ ────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderConfirmedConsumer>();

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

        // ConfigureEndpoints crée automatiquement la queue pour OrderConfirmedConsumer
        // sans aucune modification des autres services → principe Open/Closed
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

Console.WriteLine("✅ [NotificationService] En attente d'événements OrderConfirmed...");

app.Run();
