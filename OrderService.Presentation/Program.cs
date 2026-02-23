using FluentValidation;
using MediatR;
using OrderService.Application.Behaviors;
using OrderService.Application.Clients;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Persistence;
using Refit;

// ── BUILDER ───────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Order Service API",
        Version     = "v1",
        Description = "API REST — DDD, CQRS, MediatR, Refit, gRPC (Jour 2)"
    });
});

// ── Infrastructure (DbContext + Repository) ───────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── MediatR + Pipeline Behaviors ─────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    // Découvre automatiquement tous les IRequestHandler du projet Application
    cfg.RegisterServicesFromAssembly(typeof(OrderService.Application.Commands.CreateOrderCommand).Assembly);

    // Découvre aussi les handlers de la couche Presentation (ex: ProcessPaymentCommandHandler)
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Pipeline : LoggingBehavior → ValidationBehavior → Handler
    // (ordre d'ajout = ordre d'exécution)
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation : découverte automatique des validateurs ─────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(OrderService.Application.Commands.CreateOrderCommandValidator).Assembly);

// ── Refit : client HTTP vers le Product Service ───────────────────────────────
builder.Services
    .AddRefitClient<IProductServiceClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(
            builder.Configuration["Services:ProductService"] ?? "http://localhost:3002");
    });

// ── gRPC : client vers le Payment Service ────────────────────────────────────
builder.Services.AddGrpcClient<ShopFlow.Payment.Grpc.PaymentService.PaymentServiceClient>(o =>
{
    o.Address = new Uri(
        builder.Configuration["Services:PaymentGrpc"] ?? "http://localhost:5001");
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── APP ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health"); // Utilisé par YARP pour le health checking

// Initialisation DB
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Base de données initialisée");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur DB : {ex.Message}");
    }
}

app.Run();
