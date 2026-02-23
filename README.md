# ShopFlow — Jour 2 : MediatR, YARP, Refit, gRPC

## Architecture

```
Clients
  │
  ▼
ShopFlow.Gateway :8080          ← YARP Reverse Proxy
  │  /api/orders  →  :5000
  │  /api/products → :3002
  │  /api/payments → :3003
  │
  ├─► OrderService.Presentation :5000
  │     │  IMediator (MediatR)
  │     │  LoggingBehavior → ValidationBehavior → Handler
  │     │
  │     ├─► OrderService.Application
  │     │     Commands, Queries, Validators, Behaviors
  │     │     IProductServiceClient (Refit → :3002)
  │     │
  │     ├─► OrderService.Infrastructure
  │     │     EF Core SQLite
  │     │
  │     └─► ShopFlow.Payment.Grpc :5001  (gRPC)
  │
  └─► ShopFlow.Payment.Grpc :5001       ← Serveur gRPC
```

## Lancer les services

### Terminal 1 — Payment gRPC Service
```bash
cd ShopFlow.Payment.Grpc
dotnet run
# → écoute sur http://localhost:5001
```

### Terminal 2 — Order Service
```bash
cd OrderService.Presentation
dotnet run
# → écoute sur http://localhost:5000
# → Swagger UI : http://localhost:5000
```

### Terminal 3 — API Gateway
```bash
cd ShopFlow.Gateway
dotnet run
# → écoute sur http://localhost:8080
# → route /api/orders vers localhost:5000
```

## Nouvelles fonctionnalités (Jour 2)

### MediatR
- `IMediator.Send()` dans `OrdersController` — une seule dépendance
- `LoggingBehavior<,>` — log durée de chaque requête
- `ValidationBehavior<,>` — FluentValidation avant chaque handler
- `CreateOrderCommandValidator` — règles de validation

### YARP Gateway
- Point d'entrée unique : `http://localhost:8080`
- Rate Limiting : 100 req/min
- Health checks actifs (ping /health de chaque service)
- RoundRobin load balancing configuré

### Refit (client HTTP)
- `IProductServiceClient` — vérifie le stock avant création commande
- Appel automatique vers `http://localhost:3002/api/products/{id}/stock`
- Dégradé gracieux si Product Service indisponible

### gRPC Payment Service
- `POST /api/orders/{id}/payment` → appel gRPC → `PaymentServiceImpl`
- Commande marquée comme `Paid` si paiement accepté
- `LoggingInterceptor` sur le client
- Gestion des `RpcException` (Unavailable, InvalidArgument)

## Tests rapides (curl)

```bash
# 1. Créer une commande (via Gateway)
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "productId":  "789e4567-e89b-12d3-a456-426614174000",
    "quantity": 2, "unitPrice": 29.99, "currency": "EUR"
  }'

# 2. Récupérer la commande
curl http://localhost:8080/api/orders/{orderId}

# 3. Payer la commande (gRPC en coulisse)
curl -X POST http://localhost:8080/api/orders/{orderId}/payment \
  -H "Content-Type: application/json" \
  -d '{"amount": 59.98, "currency": "EUR"}'

# 4. Tester la validation FluentValidation (quantity invalide)
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "123e4567-e89b-12d3-a456-426614174000",
       "productId": "789e4567-e89b-12d3-a456-426614174000",
       "quantity": -1, "unitPrice": 29.99, "currency": "EUR"}'
# → 400 avec le détail des erreurs de validation
```
