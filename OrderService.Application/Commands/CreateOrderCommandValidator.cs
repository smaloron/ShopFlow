namespace OrderService.Application.Commands;
using FluentValidation;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("L'identifiant du client est obligatoire.");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("L'identifiant du produit est obligatoire.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La quantité doit être strictement positive.");
        RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("Le prix unitaire doit être positif.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("La devise doit être un code ISO 4217 à 3 caractères (ex: EUR, USD).");
    }
}
