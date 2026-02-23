using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromMemory(GetRoutes(), GetClusters());
builder.Services.AddRateLimiter(options => { options.AddFixedWindowLimiter("api-limit", opt => { opt.Window = TimeSpan.FromMinutes(1); opt.PermitLimit = 100; }); options.OnRejected = async (context, ct) => { context.HttpContext.Response.StatusCode = 429; await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Trop de requêtes." }, ct); }; });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHealthChecks();
var app = builder.Build();
app.Use(async (context, next) => { var requestId = Guid.NewGuid().ToString("N")[..8]; app.Logger.LogInformation("[Gateway] [{RequestId}] {Method} {Path}", requestId, context.Request.Method, context.Request.Path); await next(); app.Logger.LogInformation("[Gateway] [{RequestId}] → {StatusCode}", requestId, context.Response.StatusCode); });
app.UseCors();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();

static RouteConfig[] GetRoutes() => new[]
{
    new RouteConfig { RouteId = "orders-ddd-route",   ClusterId = "order-ddd-cluster",   Match = new RouteMatch { Path = "/api/orders/{**catch-all}" } },
    new RouteConfig { RouteId = "orders-async-route", ClusterId = "order-async-cluster", Match = new RouteMatch { Path = "/async/orders/{**catch-all}" } },
    new RouteConfig { RouteId = "sagas-route",        ClusterId = "orchestration-cluster",Match = new RouteMatch { Path = "/api/sagas/{**catch-all}" } },
    new RouteConfig { RouteId = "products-route",     ClusterId = "product-cluster",     Match = new RouteMatch { Path = "/api/products/{**catch-all}" } }
};

static ClusterConfig[] GetClusters() => new[]
{
    new ClusterConfig { ClusterId = "order-ddd-cluster",    Destinations = new Dictionary<string, DestinationConfig> { ["order-ddd-1"] = new() { Address = "http://localhost:5000" } } },
    new ClusterConfig { ClusterId = "order-async-cluster",  Destinations = new Dictionary<string, DestinationConfig> { ["order-async-1"] = new() { Address = "http://localhost:5010" } } },
    new ClusterConfig { ClusterId = "orchestration-cluster",Destinations = new Dictionary<string, DestinationConfig> { ["orchestration-1"] = new() { Address = "http://localhost:5020" } } },
    new ClusterConfig { ClusterId = "product-cluster",      Destinations = new Dictionary<string, DestinationConfig> { ["product-1"] = new() { Address = "http://localhost:3002" } } }
};
