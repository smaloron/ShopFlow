using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShopFlow.OrderService.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Base de données ───────────────────────────────────────────────────
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db"));

// ── MassTransit + RabbitMQ ────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Ce service ne fait que PUBLIER des événements (pas de consumers)
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

// ── API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new()
    {
        Title       = "ShopFlow — Order Service",
        Version     = "v1",
        Description = "Publie OrderPlaced dans RabbitMQ après création de commande"
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

// ── APP ───────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Initialisation de la base de données
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ [OrderService] Base de données prête");
}

app.Run();
