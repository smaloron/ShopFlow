using FluentValidation;
using MassTransit;
using MediatR;
using OrderService.Application.Behaviors;
using OrderService.Application.Clients;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Persistence;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Order Service API",
        Version     = "v1",
        Description = "DDD, CQRS, MediatR, Refit, gRPC, MassTransit (Jour 2+3)"
    });
});

// ── Infrastructure ────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── MediatR + Behaviors ───────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(OrderService.Application.Commands.CreateOrderCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation ──────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(OrderService.Application.Commands.CreateOrderCommandValidator).Assembly);

// ── Refit — Product Service ───────────────────────────────────────────
builder.Services
    .AddRefitClient<IProductServiceClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(
            builder.Configuration["Services:ProductService"] ?? "http://localhost:3002");
    });

// ── gRPC — Payment Service ────────────────────────────────────────────
builder.Services.AddGrpcClient<ShopFlow.Payment.Grpc.PaymentService.PaymentServiceClient>(o =>
{
    o.Address = new Uri(
        builder.Configuration["Services:PaymentGrpc"] ?? "http://localhost:5001");
});

// ── MassTransit + RabbitMQ (Jour 3) ──────────────────────────────────
// L'OrderService.Presentation publie OrderPlaced après chaque création
builder.Services.AddMassTransit(x =>
{
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

        cfg.ConfigureEndpoints(context);
    });
});

// ── Health Checks ─────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ [OrderService.Presentation] Base de données initialisée");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur DB : {ex.Message}");
    }
}

app.Run();
