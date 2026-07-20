using FluentValidation;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.Restrictions.Create;

public sealed class CreateRestrictionCommandValidator
    : AbstractValidator<CreateRestrictionCommand>
{
    public CreateRestrictionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MinimumLength(Restriction.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {Restriction.NameMinLength} caracteres.")
            .MaximumLength(Restriction.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {Restriction.NameMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(Restriction.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar los {Restriction.DescriptionMaxLength} caracteres.")
            .When(x => x.Description is not null);
    }
}
