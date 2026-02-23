using ShopFlow.Payment.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Enregistre gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Health checks (utilisé par YARP)
builder.Services.AddHealthChecks();

var app = builder.Build();

// Expose le service gRPC
app.MapGrpcService<PaymentServiceImpl>();

// Endpoint de découverte (facultatif, utile en dev)
app.MapGet("/", () =>
    "ShopFlow Payment gRPC Service — utilisez un client gRPC pour communiquer.");

app.MapHealthChecks("/health");

app.Run("http://localhost:5001");
