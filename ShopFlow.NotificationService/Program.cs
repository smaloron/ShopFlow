using MassTransit;
using ShopFlow.NotificationService.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMassTransit(x => { x.AddConsumer<OrderConfirmedConsumer>(); x.UsingRabbitMq((context, cfg) => { var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost"; cfg.Host(host, "/", h => { h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest"); h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest"); }); cfg.ConfigureEndpoints(context); }); });
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health");
Console.WriteLine("✅ [NotificationService] En attente d'événements OrderConfirmed...");
app.Run();
