namespace OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("L'identifiant produit ne peut pas être vide.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException($"La quantité doit être strictement positive. Valeur reçue : {quantity}", nameof(quantity));
        ArgumentNullException.ThrowIfNull(unitPrice, nameof(unitPrice));
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem Create(Guid productId, int quantity, Money unitPrice)
        => new OrderItem(productId, quantity, unitPrice);

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("La nouvelle quantité doit être strictement positive", nameof(newQuantity));
        Quantity = newQuantity;
    }

    private OrderItem() { UnitPrice = null!; }
}
