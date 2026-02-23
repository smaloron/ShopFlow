using System.Net;
using System.Text;
using System.Text.Json;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using OrderService.Application.Behaviors;
using OrderService.Application.Clients;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Persistence;
using OrderService.Presentation.HealthChecks;
using OrderService.Presentation.Infrastructure;
using OrderService.Presentation.Resilience;
using Polly;
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
        Description = "DDD, CQRS, MediatR, Refit, gRPC, MassTransit + Polly Resilience (Lab 4)"
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

// ── Métriques de résilience (Singleton — partagé entre tous les pipelines) ──
// LAB 4 — Exercice 4
builder.Services.AddSingleton<ResilienceMetrics>();

// ── Refit — Product Service avec Pipeline Polly complet ───────────────────
// LAB 4 — Étapes 2, 3, 4
//
// Pipeline : Fallback → Circuit Breaker → Retry → Timeout
//            (du plus externe au plus interne)
//
// Pourquoi cet ordre ?
// Timeout  (poupée la plus petite) : limite chaque tentative individuelle à 3s
// Retry    : relance si timeout → jusqu'à 3 tentatives au total
// CB       : bloque si trop d'échecs (évite de surcharger un service déjà mort)
// Fallback (poupée la plus grande) : retourne le cache si tout échoue
builder.Services
    .AddRefitClient<IProductServiceClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(
            builder.Configuration["Services:ProductService"] ?? "http://localhost:3002");
        c.Timeout = TimeSpan.FromMinutes(2); // timeout global du HttpClient (secours)
    })
    .AddResilienceHandler("product-pipeline", (pipeline, context) =>
    {
        // Récupérer les métriques depuis le DI
        var metrics = context.ServiceProvider.GetRequiredService<ResilienceMetrics>();

        // ──────────────────────────────────────────────────────────────────────
        // PATTERN 1 : TIMEOUT (plus interne)
        // Limite chaque tentative HTTP individuelle à 3s.
        // Plus court que Payment car on a un fallback disponible.
        // ──────────────────────────────────────────────────────────────────────
        pipeline.AddTimeout(TimeSpan.FromSeconds(3));

        // ──────────────────────────────────────────────────────────────────────
        // PATTERN 2 : RETRY
        // Seulement 2 retries (vs 3 pour Payment) : on a un fallback, inutile
        // d'insister. Backoff exponentiel + jitter pour éviter le thundering herd.
        // ──────────────────────────────────────────────────────────────────────
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay            = TimeSpan.FromSeconds(1),
            BackoffType      = DelayBackoffType.Exponential,
            UseJitter        = true,
            OnRetry          = args =>
            {
                metrics.IncrementRetry("product");
                Console.WriteLine(
                    $"[PRODUCT RETRY] Tentative #{args.AttemptNumber + 1} " +
                    $"après {args.RetryDelay.TotalSeconds:F1}s. " +
                    $"Erreur : {args.Outcome.Exception?.GetType().Name ?? "Status " + (int?)args.Outcome.Result?.StatusCode}");
                return ValueTask.CompletedTask;
            }
        });

        // ──────────────────────────────────────────────────────────────────────
        // PATTERN 3 : CIRCUIT BREAKER
        // Plus sensible que Payment (MinimumThroughput = 3, BreakDuration = 20s)
        // car on a un fallback : autant refermer le circuit plus vite.
        // ──────────────────────────────────────────────────────────────────────
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.5,
            SamplingDuration  = TimeSpan.FromSeconds(10),
            MinimumThroughput = 3,
            BreakDuration     = TimeSpan.FromSeconds(20),
            OnOpened = args =>
            {
                metrics.IncrementCircuitOpen("product");
                Console.WriteLine(
                    "[PRODUCT CIRCUIT BREAKER] *** CIRCUIT OUVERT *** " +
                    "Product Service considéré indisponible — Fallback activé automatiquement.");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.WriteLine("[PRODUCT CIRCUIT BREAKER] --- Circuit fermé --- Product Service rétabli.");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                Console.WriteLine("[PRODUCT CIRCUIT BREAKER] ... Circuit semi-ouvert ... Test de rétablissement en cours.");
                return ValueTask.CompletedTask;
            }
        });

        // ──────────────────────────────────────────────────────────────────────
        // PATTERN 4 : FALLBACK (plus externe — dernière ligne de défense)
        // Intervient quand Timeout + Retry + Circuit Breaker ont tous échoué.
        // Retourne une HttpResponseMessage avec les données en cache.
        // Refit désérialise cette réponse comme si elle venait du service réel.
        // ──────────────────────────────────────────────────────────────────────
        pipeline.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
        {
            // Déclencher le fallback pour toute exception ou réponse non-2xx
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Exception is not null ||
                (args.Outcome.Result is not null && !args.Outcome.Result.IsSuccessStatusCode)),

            FallbackAction = args =>
            {
                metrics.IncrementFallback();

                // Extraire l'URL de la requête originale pour identifier le produit et l'endpoint
                var requestUri = args.Outcome.Result?.RequestMessage?.RequestUri
                              ?? args.Outcome.Exception?.Data["RequestUri"] as Uri;

                var urlPath   = requestUri?.AbsolutePath ?? "";
                var segments  = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var isStock   = urlPath.EndsWith("/stock", StringComparison.OrdinalIgnoreCase);

                // Trouver le Guid du produit dans les segments de l'URL
                // URL stock:   /api/products/{guid}/stock
                // URL produit: /api/products/{guid}
                Guid productId = Guid.Empty;
                foreach (var segment in segments)
                {
                    if (Guid.TryParse(segment, out var parsed))
                    {
                        productId = parsed;
                        break;
                    }
                }

                string jsonBody;

                if (isStock)
                {
                    // Fallback pour GetStockAsync — retourne le stock en cache
                    var cached = ProductFallbackData.GetCachedStock(productId);
                    jsonBody = cached is not null
                        ? JsonSerializer.Serialize(new
                        {
                            productId = cached.ProductId,
                            available = cached.Available,
                            reserved  = cached.Reserved,
                            isFromCache = true
                        })
                        : JsonSerializer.Serialize(new
                        {
                            productId   = productId,
                            available   = 0,
                            reserved    = 0,
                            isFromCache = true,
                            warning     = "Produit non trouvé dans le cache de fallback"
                        });

                    Console.WriteLine(
                        $"[PRODUCT FALLBACK] *** FALLBACK ACTIVÉ (stock) *** " +
                        $"Produit {productId} — Erreur : {args.Outcome.Exception?.Message ?? "N/A"}");
                }
                else
                {
                    // Fallback pour GetProductAsync — retourne les données produit en cache
                    var cached = ProductFallbackData.GetCachedProduct(productId);
                    var cacheAge = ProductFallbackData.GetCacheAge(productId);

                    jsonBody = cached is not null
                        ? JsonSerializer.Serialize(new
                        {
                            id          = cached.Id,
                            name        = cached.Name,
                            price       = cached.Price,
                            currency    = cached.Currency,
                            isFromCache = true,
                            cacheAgeMinutes = (int?)cacheAge?.TotalMinutes,
                            cacheWarning    = "Données issues du cache — peuvent être obsolètes"
                        })
                        : JsonSerializer.Serialize(new
                        {
                            id          = productId,
                            name        = "Produit indisponible (cache vide)",
                            price       = 0m,
                            currency    = "EUR",
                            isFromCache = true,
                            error       = "PRODUCT_NOT_IN_CACHE"
                        });

                    Console.WriteLine(
                        $"[PRODUCT FALLBACK] *** FALLBACK ACTIVÉ (produit) *** " +
                        $"Produit {productId} — Erreur : {args.Outcome.Exception?.Message ?? "N/A"}");
                }

                // Construire la HttpResponseMessage que Refit va désérialiser
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                // Ajouter des headers pour que l'appelant sache que c'est du cache
                response.Headers.Add("X-Fallback-Source", "Cache");
                response.Headers.Add("X-Fallback-Reason", args.Outcome.Exception?.GetType().Name ?? "ServiceError");

                return ValueTask.FromResult(response);
            }
        });
    });

// ── gRPC — Payment Service avec Pipeline Polly (sans Fallback) ────────────
// LAB 4 — Étape 3 : Pipeline personnalisé
//
// Pipeline : Circuit Breaker → Retry → Timeout
// PAS de Fallback : les erreurs de paiement doivent être explicites.
// Un fallback silencieux pourrait laisser croire qu'un paiement a réussi.
builder.Services
    .AddGrpcClient<ShopFlow.Payment.Grpc.PaymentService.PaymentServiceClient>(o =>
    {
        o.Address = new Uri(
            builder.Configuration["Services:PaymentGrpc"] ?? "http://localhost:5001");
    })
    .AddResilienceHandler("payment-pipeline", (pipeline, context) =>
    {
        var metrics = context.ServiceProvider.GetRequiredService<ResilienceMetrics>();

        // PATTERN 1 : TIMEOUT — 5s par tentative (fail-fast pour les paiements)
        pipeline.AddTimeout(TimeSpan.FromSeconds(5));

        // PATTERN 2 : RETRY — 3 tentatives, backoff exponentiel + jitter
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay            = TimeSpan.FromSeconds(1),
            BackoffType      = DelayBackoffType.Exponential,
            UseJitter        = true,
            OnRetry          = args =>
            {
                metrics.IncrementRetry("payment");
                Console.WriteLine(
                    $"[PAYMENT RETRY] Tentative #{args.AttemptNumber + 1} " +
                    $"après {args.RetryDelay.TotalSeconds:F1}s. " +
                    $"Erreur : {args.Outcome.Exception?.GetType().Name ?? "Status " + (int?)args.Outcome.Result?.StatusCode}");
                return ValueTask.CompletedTask;
            }
        });

        // PATTERN 3 : CIRCUIT BREAKER — s'ouvre après 5 échecs, 30s de break
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.5,
            SamplingDuration  = TimeSpan.FromSeconds(10),
            MinimumThroughput = 5,
            BreakDuration     = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                metrics.IncrementCircuitOpen("payment");
                Console.WriteLine(
                    "[PAYMENT CIRCUIT BREAKER] *** CIRCUIT OUVERT *** " +
                    "Payment Service indisponible — Toutes les requêtes rejetées pendant 30s.");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.WriteLine("[PAYMENT CIRCUIT BREAKER] --- Circuit fermé --- Payment Service rétabli.");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                Console.WriteLine("[PAYMENT CIRCUIT BREAKER] ... Circuit semi-ouvert ... Test de rétablissement.");
                return ValueTask.CompletedTask;
            }
        });
    });

// ── MassTransit + RabbitMQ ────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host     = builder.Configuration["RabbitMQ:Host"]     ?? "localhost";
        var user     = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
        cfg.Host(host, "/", h => { h.Username(user); h.Password(password); });
        cfg.ConfigureEndpoints(context);
    });
});

// ── Health Checks ─────────────────────────────────────────────────────
// LAB 4 — Étape 6 + Exercices 3 & 5
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db" })
    .AddCheck<DependenciesHealthCheck>(
        name: "external-dependencies",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "dependencies" });

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health checks avec détail JSON (pour voir les sous-checks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name     = e.Key,
                status   = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});
app.MapHealthChecks("/health/db",           new HealthCheckOptions { Predicate = check => check.Tags.Contains("db") });
app.MapHealthChecks("/health/dependencies", new HealthCheckOptions { Predicate = check => check.Tags.Contains("dependencies") });

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ [OrderService.Presentation] Base de données initialisée");
        Console.WriteLine("✅ [OrderService.Presentation] Pipelines Polly configurés (Product + Payment)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur DB : {ex.Message}");
    }
}

app.Run();
