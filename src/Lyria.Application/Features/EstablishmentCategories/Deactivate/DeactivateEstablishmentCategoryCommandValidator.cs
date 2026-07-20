using FluentValidation;

namespace Lyria.Application.Features.EstablishmentCategories.Deactivate;

public sealed class DeactivateEstablishmentCategoryCommandValidator
    : AbstractValidator<DeactivateEstablishmentCategoryCommand>
{
    public DeactivateEstablishmentCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
