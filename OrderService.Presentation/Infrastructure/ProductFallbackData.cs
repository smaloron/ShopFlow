namespace OrderService.Presentation.Infrastructure;

/// <summary>
/// Cache en mémoire de données produits utilisé comme fallback
/// quand le Product Service est indisponible.
///
/// LAB 4 — Étape 4 : Fallback
/// Dans un système réel, ce cache serait stocké dans Redis pour le partager
/// entre plusieurs instances et le mettre à jour dynamiquement.
/// Ici : Dictionary statique en mémoire, simple et suffisant pour le lab.
/// </summary>
public static class ProductFallbackData
{
    // Données pré-chargées avec les IDs fixes du ProductService seed data
    // (synchronisés avec ShopFlow.ProductService/Data/ProductDbContext.cs)
    private static readonly Dictionary<Guid, CachedProductDto> Cache = new()
    {
        {
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa1"),
            new CachedProductDto(
                Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa1"),
                "Laptop Pro 15 (Cache)",
                price: 1299.99m,
                currency: "EUR",
                stockAvailable: 5,
                category: "Electronics",
                lastUpdated: DateTime.UtcNow.AddHours(-2))
        },
        {
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa2"),
            new CachedProductDto(
                Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa2"),
                "Clavier Mécanique (Cache)",
                price: 89.99m,
                currency: "EUR",
                stockAvailable: 12,
                category: "Accessories",
                lastUpdated: DateTime.UtcNow.AddHours(-1))
        },
        {
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa3"),
            new CachedProductDto(
                Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa3"),
                "Souris Ergonomique (Cache)",
                price: 45.00m,
                currency: "EUR",
                stockAvailable: 20,
                category: "Accessories",
                lastUpdated: DateTime.UtcNow.AddMinutes(-30))
        },
        {
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa4"),
            new CachedProductDto(
                Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa4"),
                "Écran 27 pouces (Cache)",
                price: 349.99m,
                currency: "EUR",
                stockAvailable: 3,
                category: "Electronics",
                lastUpdated: DateTime.UtcNow.AddHours(-4))
        }
    };

    // ── API Publique ────────────────────────────────────────────────────

    /// <summary>
    /// Récupère un produit depuis le cache.
    /// Retourne null si le produit n'est pas dans le cache.
    /// TryGetValue = 1 recherche O(1) au lieu de 2 avec ContainsKey + accès.
    /// </summary>
    public static CachedProductDto? GetCachedProduct(Guid productId)
        => Cache.TryGetValue(productId, out var product) ? product : null;

    /// <summary>Récupère le stock disponible en cache pour un produit</summary>
    public static CachedStockDto? GetCachedStock(Guid productId)
    {
        if (!Cache.TryGetValue(productId, out var product)) return null;
        return new CachedStockDto(product.Id, product.StockAvailable, reserved: 0);
    }

    /// <summary>Met à jour le cache avec des données fraîches du service (à appeler après un succès)</summary>
    public static void UpdateCache(CachedProductDto product)
    {
        Cache[product.Id] = product with { LastUpdated = DateTime.UtcNow };
        Console.WriteLine($"[CACHE] Produit {product.Id} ({product.Name}) mis à jour dans le cache");
    }

    /// <summary>Vérifie si un produit est en cache</summary>
    public static bool HasCachedProduct(Guid productId)
        => Cache.ContainsKey(productId);

    /// <summary>Retourne tous les produits en cache (utile pour les health checks)</summary>
    public static IReadOnlyList<CachedProductDto> GetAllCachedProducts()
        => Cache.Values.ToList().AsReadOnly();

    /// <summary>Vide le cache (utile pour les tests)</summary>
    public static void ClearCache()
    {
        Console.WriteLine("[CACHE] Cache vidé");
        Cache.Clear();
    }

    /// <summary>Retourne l'âge des données pour un produit, ou null s'il n'est pas en cache</summary>
    public static TimeSpan? GetCacheAge(Guid productId)
        => Cache.TryGetValue(productId, out var product)
            ? DateTime.UtcNow - product.LastUpdated
            : null;
}

// ── DTOs du cache ────────────────────────────────────────────────────────────

/// <summary>Représentation d'un produit en cache (identique à ProductDto + métadonnées cache)</summary>
public record CachedProductDto(
    Guid     Id,
    string   Name,
    decimal  Price,
    string   Currency,
    int      StockAvailable,
    string   Category,
    DateTime LastUpdated);

/// <summary>Représentation du stock en cache (identique à ProductStockDto)</summary>
public record CachedStockDto(Guid ProductId, int Available, int Reserved);
