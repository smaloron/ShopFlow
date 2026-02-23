namespace OrderService.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;

/// <summary>
/// Extension methods pour configurer l'Infrastructure dans le conteneur DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext avec SQLite
        services.AddDbContext<OrderDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("OrderDb")
                ?? throw new InvalidOperationException(
                    "La chaîne de connexion 'OrderDb' est manquante dans appsettings.json");

            options.UseSqlite(connectionString);

#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        // Repository (Scoped = une instance par requête HTTP)
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
