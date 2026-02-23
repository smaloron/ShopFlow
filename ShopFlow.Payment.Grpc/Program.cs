using ShopFlow.Payment.Grpc.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc(options => { options.EnableDetailedErrors = builder.Environment.IsDevelopment(); });
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapGrpcService<PaymentServiceImpl>();
app.MapGet("/", () => "ShopFlow Payment gRPC Service");
app.MapHealthChecks("/health");
app.Run("http://localhost:5001");
