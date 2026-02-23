namespace OrderService.Application.Clients;

using Refit;

/// <summary>
/// Client HTTP vers le Product Service, généré automatiquement par Refit.
/// Chaque service copie les DTOs localement (anti-pattern : projet partagé).
/// </summary>
public interface IProductServiceClient
{
    /// <summary>Récupère le stock disponible pour un produit</summary>
    [Get("/api/products/{productId}/stock")]
    Task<ProductStockDto> GetStockAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Récupère les détails d'un produit</summary>
    [Get("/api/products/{productId}")]
    Task<ProductDto> GetProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Met à jour la réservation de stock</summary>
    [Put("/api/products/{productId}/stock/reserve")]
    Task ReserveStockAsync(Guid productId, [Body] ReserveStockRequest request, CancellationToken ct = default);
}

// ── DTOs locaux (copiés dans ce service, pas partagés) ────────────────

/// <summary>Stock disponible pour un produit</summary>
public record ProductStockDto(Guid ProductId, int Available, int Reserved);

/// <summary>Détails d'un produit</summary>
public record ProductDto(Guid Id, string Name, decimal Price, string Currency);

/// <summary>Requête de réservation de stock</summary>
public record ReserveStockRequest(int Quantity);
