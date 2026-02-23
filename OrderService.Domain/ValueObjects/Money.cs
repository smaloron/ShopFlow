namespace OrderService.Domain.ValueObjects;

public record Money(decimal Amount, string Currency)
{
    public Money
    {
        if (Amount < 0)
            throw new ArgumentException($"Le montant doit être positif ou nul. Valeur reçue : {Amount}", nameof(Amount));
        if (string.IsNullOrWhiteSpace(Currency) || Currency.Length != 3)
            throw new ArgumentException("Le code devise doit être une chaîne ISO 4217 valide (ex: EUR, USD, GBP)", nameof(Currency));
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Impossible d'additionner {Currency} et {other.Currency}.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor)
    {
        if (factor < 0)
            throw new ArgumentException("Le facteur doit être positif", nameof(factor));
        return new Money(Amount * factor, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Impossible de soustraire {other.Currency} de {Currency}");
        if (Amount < other.Amount)
            throw new InvalidOperationException($"Impossible de soustraire {other.Amount} {other.Currency} de {Amount} {Currency} (résultat négatif)");
        return new Money(Amount - other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
