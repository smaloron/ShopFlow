namespace ShopFlow.ProductService.Models;

/// <summary>Entité produit persistée en base SQLite</summary>
public class Product
{
    public Guid    Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public decimal Price       { get; set; }
    public string  Currency    { get; set; } = "EUR";
    public int     Stock       { get; set; }
    public int     Reserved    { get; set; }
    public string  Category    { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;

    public int Available => Math.Max(0, Stock - Reserved);
}
