using Microsoft.EntityFrameworkCore;
using ShopFlow.ProductService.Configuration;
using ShopFlow.ProductService.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Base de données ───────────────────────────────────────────────────
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ProductDb") ?? "Data Source=products.db"));

// ── Configuration de simulation de latence (LAB 4 — Étape 5) ─────────
// Singleton pour permettre la modification à chaud via l'endpoint /api/products/simulation
var simulationConfig = new LatencySimulationConfig();
builder.Configuration.GetSection("LatencySimulation").Bind(simulationConfig);
builder.Services.AddSingleton(simulationConfig);

// ── API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new()
    {
        Title       = "ShopFlow — Product Service",
        Version     = "v1",
        Description = "Catalogue produits avec simulation de latence (Lab 4 — Étape 5)"
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>();

// ── APP ───────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Initialisation de la base de données avec les données de seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ [ProductService] Base de données prête avec données de seed");
    Console.WriteLine("💡 [ProductService] Simuler une latence via POST /api/products/simulation");
}

app.Run();
