using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Configuration;

// ── BUILDER ───────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── YARP Reverse Proxy ────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters());

// ── Rate Limiting (100 req/min par défaut) ────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api-limit", opt =>
    {
        opt.Window      = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });

    // Réponse quand la limite est dépassée
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Trop de requêtes. Réessayez dans une minute." }, ct);
    };
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── APP ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Middleware de logging des requêtes entrantes
app.Use(async (context, next) =>
{
    var requestId = Guid.NewGuid().ToString("N")[..8];
    app.Logger.LogInformation(
        "[Gateway] [{RequestId}] {Method} {Path}",
        requestId,
        context.Request.Method,
        context.Request.Path);

    await next();

    app.Logger.LogInformation(
        "[Gateway] [{RequestId}] → {StatusCode}",
        requestId,
        context.Response.StatusCode);
});

app.UseCors();
app.UseRateLimiter();
app.MapHealthChecks("/health");

// Route la requête vers le bon micro-service
app.MapReverseProxy();

app.Run();

// ── Configuration des Routes ─────────────────────────────────────────────────

static RouteConfig[] GetRoutes() => new[]
{
    new RouteConfig
    {
        RouteId   = "orders-route",
        ClusterId = "order-cluster",
        Match     = new RouteMatch { Path = "/api/orders/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId   = "products-route",
        ClusterId = "product-cluster",
        Match     = new RouteMatch { Path = "/api/products/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId   = "payments-route",
        ClusterId = "payment-cluster",
        Match     = new RouteMatch { Path = "/api/payments/{**catch-all}" }
    }
};

// ── Configuration des Clusters ────────────────────────────────────────────────

static ClusterConfig[] GetClusters() => new[]
{
    new ClusterConfig
    {
        ClusterId           = "order-cluster",
        LoadBalancingPolicy = "RoundRobin",
        HealthCheck         = new HealthCheckConfig
        {
            Active = new ActiveHealthCheckConfig
            {
                Enabled  = true,
                Interval = TimeSpan.FromSeconds(30),
                Path     = "/health"
            }
        },
        Destinations = new Dictionary<string, DestinationConfig>
        {
            // En développement : une seule instance
            ["order-1"] = new DestinationConfig { Address = "http://localhost:5000" }

            // En production (load balancing) :
            // ["order-2"] = new DestinationConfig { Address = "http://localhost:5002" }
        }
    },
    new ClusterConfig
    {
        ClusterId    = "product-cluster",
        HealthCheck  = new HealthCheckConfig
        {
            Active = new ActiveHealthCheckConfig
            {
                Enabled  = true,
                Interval = TimeSpan.FromSeconds(30),
                Path     = "/health"
            }
        },
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["product-1"] = new DestinationConfig { Address = "http://localhost:3002" }
        }
    },
    new ClusterConfig
    {
        ClusterId    = "payment-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["payment-1"] = new DestinationConfig { Address = "http://localhost:3003" }
        }
    }
};
