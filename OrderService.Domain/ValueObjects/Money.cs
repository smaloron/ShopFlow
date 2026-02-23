namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Value Object représentant une somme d'argent avec sa devise.
/// Immutable et avec validation des règles métier.
/// </summary>
/// <param name="Amount">Le montant (doit être >= 0)</param>
/// <param name="Currency">Code devise ISO 4217 (ex: EUR, USD, GBP)</param>
public record Money(decimal Amount, string Currency)
{
    // Compact constructor — s'exécute après l'assignation des propriétés primaires
    public Money
    {
        // Règle 1 : Le montant ne peut pas être négatif
        if (Amount < 0)
            throw new ArgumentException(
                $"Le montant doit être positif ou nul. Valeur reçue : {Amount}",
                nameof(Amount));

        // Règle 2 : La devise doit respecter la norme ISO 4217 (3 lettres)
        if (string.IsNullOrWhiteSpace(Currency) || Currency.Length != 3)
            throw new ArgumentException(
                "Le code devise doit être une chaîne ISO 4217 valide (ex: EUR, USD, GBP)",
                nameof(Currency));
    }

    /// <summary>
    /// Additionne deux montants de même devise
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Impossible d'additionner {Currency} et {other.Currency}. " +
                $"Convertissez d'abord vers une devise commune.");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Multiplie le montant par un facteur entier (pour les quantités)
    /// </summary>
    public Money Multiply(int factor)
    {
        if (factor < 0)
            throw new ArgumentException("Le facteur doit être positif", nameof(factor));

        return new Money(Amount * factor, Currency);
    }

    /// <summary>
    /// Soustrait un montant (pratique pour les remboursements)
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Impossible de soustraire {other.Currency} de {Currency}");

        if (Amount < other.Amount)
            throw new InvalidOperationException(
                $"Impossible de soustraire {other.Amount} {other.Currency} " +
                $"de {Amount} {Currency} (résultat négatif)");

        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Représentation textuelle lisible
    /// </summary>
    public override string ToString() => $"{Amount:F2} {Currency}";
}
