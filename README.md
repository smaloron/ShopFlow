# ShopFlow — Lab 4 : Résilience avec Polly

## Ce qui a été ajouté par rapport au Lab 3

### Nouveaux fichiers (Lab 4)

| Fichier | Description |
|---------|-------------|
| `ShopFlow.ProductService/` | **Nouveau service** .NET remplaçant le service Node.js port 3002. Inclut la simulation de latence configurable. |
| `OrderService.Presentation/Infrastructure/ProductFallbackData.cs` | Cache en mémoire de données produits pour le Fallback Polly. IDs synchronisés avec le seed data du ProductService. |
| `OrderService.Presentation/Resilience/ResilienceMetrics.cs` | Suivi thread-safe des métriques Polly (retries, circuit breaks, fallbacks). |
| `OrderService.Presentation/Controllers/MetricsController.cs` | Endpoint `GET /api/metrics/resilience` (Exercice 4). |
| `OrderService.Presentation/HealthChecks/DependenciesHealthCheck.cs` | Health check agrégé avec dégradation partielle (Exercice 5). |

### Fichiers modifiés (Lab 4)

| Fichier | Modification |
|---------|-------------|
| `OrderService.Presentation/Program.cs` | Pipelines Polly configurés pour Refit (Product) et gRPC (Payment). |
| `OrderService.Presentation/OrderService.Presentation.csproj` | Ajout de `Microsoft.Extensions.Http.Resilience` et `Microsoft.Extensions.Resilience`. |

---

## Architecture des Pipelines Polly

### Product Service (Refit) — Pipeline complet avec Fallback

```
Requête → [Fallback] → [Circuit Breaker] → [Retry] → [Timeout 3s] → ProductService :3002
                ↑             ↑               ↑
            Cache mémoire  Ouvre après     2 retries
            ProductFallback  3 échecs/10s    + jitter
            Data.cs          BreakDuration   Backoff exp.
                             = 20s
```

### Payment Service (gRPC) — Pipeline sans Fallback

```
Requête → [Circuit Breaker] → [Retry] → [Timeout 5s] → PaymentGrpc :5001
                ↑               ↑
           Ouvre après      3 retries
           5 échecs/10s       + jitter
           BreakDuration      Backoff exp.
           = 30s
```

> **Pourquoi pas de Fallback pour Payment ?**
> Un fallback silencieux sur les paiements serait dangereux : le client croirait que sa commande est payée alors qu'elle ne l'est pas. Les erreurs de paiement doivent être explicites.

---

## Démarrage

### 1. RabbitMQ (Docker requis)

```bash
docker-compose up -d rabbitmq
# Management UI → http://localhost:15672  (guest / guest)
```

### 2. Lancer les services (ordre recommandé)

```bash
# Terminal 1 — Payment gRPC (port 5001)
cd ShopFlow.Payment.Grpc && dotnet run

# Terminal 2 — Product Service (port 3002) — NOUVEAU Lab 4
cd ShopFlow.ProductService && dotnet run

# Terminal 3 — Orchestration Saga (port 5020)
cd ShopFlow.Orchestration && dotnet run

# Terminal 4 — Payment Service async (port 5030)
cd ShopFlow.PaymentService && dotnet run

# Terminal 5 — Notification Service (port 5040)
cd ShopFlow.NotificationService && dotnet run

# Terminal 6 — Order Service DDD (port 5000) — PIPELINES POLLY ICI
cd OrderService.Presentation && dotnet run

# Terminal 7 — Order Service async (port 5010)
cd ShopFlow.OrderService && dotnet run

# Terminal 8 — Gateway (port 8080)
cd ShopFlow.Gateway && dotnet run
```

---

## Tests Lab 4

### Étape 5 — Activer la simulation de latence

```bash
# Activer un délai de 4-6s (> timeout Polly de 3s → déclenche Timeout + Retry + Fallback)
curl -X POST http://localhost:3002/api/products/simulation \
  -H "Content-Type: application/json" \
  -d '{"enabled":true,"minDelaySeconds":4,"maxDelaySeconds":6,"failureRate":0.0}'

# Puis créer une commande → observer dans les logs d'OrderService :
# [PRODUCT RETRY] Tentative #1 après 1.0s
# [PRODUCT RETRY] Tentative #2 après 2.0s
# [PRODUCT FALLBACK] *** FALLBACK ACTIVÉ *** Produit 3fa85f64-... — Erreur : TimeoutRejectedException
```

### Étape 3 — Ouvrir le Circuit Breaker Payment

```bash
# Arrêter le service gRPC Payment (Ctrl+C dans Terminal 1)
# Puis envoyer plusieurs commandes :
for i in 1 2 3 4 5 6; do
  curl -X POST http://localhost:5000/api/orders \
    -H "Content-Type: application/json" \
    -d '{"customerId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","productId":"3fa85f64-5717-4562-b3fc-2c963f66afa1","quantity":1,"unitPrice":99.99,"currency":"EUR"}'
  echo ""
done
# Requêtes 1-5 : chacune prend ~27s (4 tentatives × 5s timeout + délais retry)
# Requête 6+ : rejetée immédiatement (circuit ouvert)
```

### Exercice 4 — Consulter les métriques

```bash
curl http://localhost:5000/api/metrics/resilience
# Retourne :
# {
#   "timestamp": "...",
#   "retries": { "product": 4, "payment": 9 },
#   "circuitBreakersOpened": { "payment": 2, "product": 1 },
#   "fallbackActivations": 7
# }

# Remettre les compteurs à zéro
curl -X DELETE http://localhost:5000/api/metrics/resilience
```

### Exercice 5 — Tester le Health Check avec dégradation partielle

```bash
# État normal
curl http://localhost:5000/health/dependencies

# Arrêter ProductService → cache disponible → Degraded (pas Unhealthy)
# Arrêter PaymentGrpc   → Unhealthy (pod exclu du load balancer)
curl http://localhost:5000/health
```

---

## IDs Produits pour les Tests

Les IDs suivants sont pré-chargés dans le ProductService **et** dans le cache Fallback :

| ID | Nom | Prix |
|----|-----|------|
| `3fa85f64-5717-4562-b3fc-2c963f66afa1` | Laptop Pro 15 | 1 299,99 € |
| `3fa85f64-5717-4562-b3fc-2c963f66afa2` | Clavier Mécanique | 89,99 € |
| `3fa85f64-5717-4562-b3fc-2c963f66afa3` | Souris Ergonomique | 45,00 € |
| `3fa85f64-5717-4562-b3fc-2c963f66afa4` | Écran 27 pouces | 349,99 € |

---

## Adaptation Lab 3 → Lab 4

### Différences par rapport aux instructions du lab

Les instructions du lab utilisent des `int` pour les IDs produits, mais cette solution utilise des `Guid` conformément à l'architecture DDD du Lab 3. Les adaptations :

- `ProductFallbackData` utilise `Dictionary<Guid, CachedProductDto>` au lieu de `Dictionary<int, Product>`
- Le parsing de l'URL dans le `FallbackAction` recherche un `Guid` au lieu d'un `int`
- Les IDs seed dans `ProductDbContext` correspondent exactement aux IDs dans `ProductFallbackData`

