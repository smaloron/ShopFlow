namespace OrderService.Application.Clients;
using Refit;

public interface IProductServiceClient
{
    [Get("/api/products/{productId}/stock")]
    Task<ProductStockDto> GetStockAsync(Guid productId, CancellationToken ct = default);

    [Get("/api/products/{productId}")]
    Task<ProductDto> GetProductAsync(Guid productId, CancellationToken ct = default);

    [Put("/api/products/{productId}/stock/reserve")]
    Task ReserveStockAsync(Guid productId, [Body] ReserveStockRequest request, CancellationToken ct = default);
}

public record ProductStockDto(Guid ProductId, int Available, int Reserved);
public record ProductDto(Guid Id, string Name, decimal Price, string Currency);
public record ReserveStockRequest(int Quantity);
