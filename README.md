# ShopFlow — Jour 3 : MassTransit, RabbitMQ et Saga Pattern

## Architecture complète

```
┌─ YARP Gateway :8080 ──────────────────────────────────┐
│  /api/orders   → OrderService.DDD  :5000               │
│  /async/orders → ShopFlow.OrderSvc :5010               │
│  /api/sagas    → Orchestration     :5020               │
└────────────────────────────────────────────────────────┘
                         │ RabbitMQ :5672
         ┌───────────────┼───────────────────┐
         ▼               ▼                   ▼
  ShopFlow.OrderService  ShopFlow.Orchestration  ShopFlow.PaymentService
  publie OrderPlaced     Saga State Machine       consumer PaymentRequested
         │               │                   │
         └───────────────┴───────────────────┴──► ShopFlow.NotificationService
                                                   consumer OrderConfirmed (ex. 5)
```

## Démarrage

```bash
# 1. RabbitMQ (Docker requis)
docker-compose up -d rabbitmq
# Management UI → http://localhost:15672  (guest / guest)

# 2. Orchestration (Saga — démarrer EN PREMIER)
cd ShopFlow.Orchestration && dotnet run       # :5020

# 3. Payment Service
cd ShopFlow.PaymentService && dotnet run      # :5030

# 4. Notification Service (exercice 5)
cd ShopFlow.NotificationService && dotnet run # :5040

# 5. Order Service async
cd ShopFlow.OrderService && dotnet run        # Swagger :5010

# 6. Gateway
cd ShopFlow.Gateway && dotnet run             # :8080
```

## Tests rapides

```bash
# Créer une commande (succès — montant < 1000 €)
curl -X POST http://localhost:8080/async/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","items":[{"productId":"3fa85f64-5717-4562-b3fc-2c963f66afa7","quantity":2,"unitPrice":49.99}]}'

# Créer une commande (échec — montant > 1000 €)
curl -X POST http://localhost:8080/async/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","items":[{"productId":"3fa85f64-5717-4562-b3fc-2c963f66afa7","quantity":10,"unitPrice":150.00}]}'

# Interroger l'état de la Saga
curl http://localhost:8080/api/sagas/{orderId}
curl http://localhost:8080/api/sagas
```

## Nouveautés Jour 3

- **ShopFlow.Contracts** — 6 événements partagés (records immuables)
- **ShopFlow.OrderService** — publie OrderPlaced, retourne 202 Accepted
- **ShopFlow.Orchestration** — Saga State Machine + SQLite + timeout 5 min
- **ShopFlow.PaymentService** — consumer avec retry policy (ex. 4)
- **ShopFlow.NotificationService** — consumer OrderConfirmed (ex. 5 — Open/Closed)
- **OrderService.Presentation** — intégré MassTransit, publie OrderPlaced
- **docker-compose.yml** — RabbitMQ avec Management UI
