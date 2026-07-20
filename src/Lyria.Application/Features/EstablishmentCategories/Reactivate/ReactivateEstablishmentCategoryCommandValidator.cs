using FluentValidation;

namespace Lyria.Application.Features.EstablishmentCategories.Reactivate;

public sealed class ReactivateEstablishmentCategoryCommandValidator
    : AbstractValidator<ReactivateEstablishmentCategoryCommand>
{
    public ReactivateEstablishmentCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
