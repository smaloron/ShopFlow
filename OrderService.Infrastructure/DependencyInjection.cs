namespace OrderService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("OrderDb")
                ?? throw new InvalidOperationException("La chaîne de connexion 'OrderDb' est manquante dans appsettings.json");
            options.UseSqlite(connectionString);
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
