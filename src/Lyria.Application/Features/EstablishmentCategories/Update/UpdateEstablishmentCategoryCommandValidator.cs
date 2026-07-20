using FluentValidation;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.Update;

public sealed class UpdateEstablishmentCategoryCommandValidator
    : AbstractValidator<UpdateEstablishmentCategoryCommand>
{
    public UpdateEstablishmentCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MinimumLength(EstablishmentCategory.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {EstablishmentCategory.NameMinLength} caracteres.")
            .MaximumLength(EstablishmentCategory.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {EstablishmentCategory.NameMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(EstablishmentCategory.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar los {EstablishmentCategory.DescriptionMaxLength} caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(EstablishmentCategory.IconUrlMaxLength)
            .WithMessage($"La URL del ícono no puede superar los {EstablishmentCategory.IconUrlMaxLength} caracteres.")
            .When(x => x.IconUrl is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El orden no puede ser negativo.");
    }
}
