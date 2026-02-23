using MassTransit;
using ShopFlow.PaymentService.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentRequestedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(host, "/", h => { h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest"); h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest"); });
        cfg.ReceiveEndpoint("payment-requested", e =>
        {
            e.UseMessageRetry(r => { r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)); });
            e.ConfigureConsumer<PaymentRequestedConsumer>(context);
        });
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new() { Title = "ShopFlow — Payment Service", Version = "v1" }); });
builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
Console.WriteLine("✅ [PaymentService] En attente de messages PaymentRequested...");
app.Run();
