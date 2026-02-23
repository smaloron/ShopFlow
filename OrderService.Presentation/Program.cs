using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Persistence;

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
        Description = "API REST pour la gestion des commandes avec DDD et CQRS"
    });
});

// Infrastructure (DbContext + Repository)
builder.Services.AddInfrastructure(builder.Configuration);

// Application — Handlers CQRS (manuel, sans MediatR)
builder.Services.AddScoped<CreateOrderCommandHandler>();
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<CancelOrderCommandHandler>();
builder.Services.AddScoped<GetOrdersByCustomerQueryHandler>();
builder.Services.AddScoped<CalculateRefundQueryHandler>();

// CORS
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
        options.RoutePrefix = string.Empty; // Swagger à la racine
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Initialisation de la base de données (OK pour le lab, migrations en production)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Base de données créée avec succès");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur lors de la création de la DB : {ex.Message}");
    }
}

app.Run();
