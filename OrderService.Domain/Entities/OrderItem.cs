namespace OrderService.Domain.Entities;

using OrderService.Domain.ValueObjects;

/// <summary>
/// Représente une ligne de commande (un produit avec quantité et prix).
/// Entité enfant de Order (ne peut exister indépendamment).
/// </summary>
public class OrderItem
{
    // ── PROPRIÉTÉS ────────────────────────────────────────────────────────

    /// <summary>Identifiant unique de la ligne de commande</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifiant du produit commandé</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Quantité commandée (doit être > 0)</summary>
    public int Quantity { get; private set; }

    /// <summary>Prix unitaire au moment de la commande (snapshot)</summary>
    public Money UnitPrice { get; private set; }

    /// <summary>Calcule le total de cette ligne (quantité × prix unitaire)</summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    // ── CONSTRUCTEUR PRIVÉ ────────────────────────────────────────────────

    private OrderItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "L'identifiant produit ne peut pas être vide.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException(
                $"La quantité doit être strictement positive. Valeur reçue : {quantity}",
                nameof(quantity));

        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // ── FACTORY METHOD ────────────────────────────────────────────────────

    /// <summary>Crée une nouvelle ligne de commande de manière contrôlée</summary>
    public static OrderItem Create(Guid productId, int quantity, Money unitPrice)
        => new OrderItem(productId, quantity, unitPrice);

    // ── MÉTHODES MÉTIER ───────────────────────────────────────────────────

    /// <summary>Modifie la quantité commandée</summary>
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException(
                "La nouvelle quantité doit être strictement positive",
                nameof(newQuantity));

        Quantity = newQuantity;
    }

    // ── CONSTRUCTEUR EF CORE ──────────────────────────────────────────────

    /// <summary>Constructeur requis par Entity Framework Core</summary>
    private OrderItem()
    {
        UnitPrice = null!;
    }
}
