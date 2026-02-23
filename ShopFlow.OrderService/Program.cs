using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShopFlow.OrderService.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db"));
builder.Services.AddMassTransit(x => { x.UsingRabbitMq((context, cfg) => { var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost"; cfg.Host(host, "/", h => { h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest"); h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest"); }); cfg.ConfigureEndpoints(context); }); });
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new() { Title = "ShopFlow — Order Service", Version = "v1" }); });
builder.Services.AddHealthChecks().AddDbContextCheck<OrderDbContext>();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = string.Empty; });
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>(); await db.Database.EnsureCreatedAsync(); Console.WriteLine("✅ [OrderService] Base de données prête"); }
app.Run();
