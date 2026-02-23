using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Orchestration.Data;
using ShopFlow.Orchestration.Sagas;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SagaDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("SagaDb") ?? "Data Source=saga.db"));
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderSaga, OrderSagaState>().EntityFrameworkRepository(r => { r.ExistingDbContext<SagaDbContext>(); r.UseSqlite(); r.ConcurrencyMode = ConcurrencyMode.Optimistic; });
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(host, "/", h => { h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest"); h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest"); });
        cfg.UseMessageRetry(r => { r.Intervals(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)); });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new() { Title = "ShopFlow — Orchestration", Version = "v1" }); });
builder.Services.AddHealthChecks().AddDbContextCheck<SagaDbContext>();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>(); await db.Database.EnsureCreatedAsync(); Console.WriteLine("✅ [Orchestration] Base de données Saga prête"); }
app.Run();
