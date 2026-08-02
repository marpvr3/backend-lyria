using FluentValidation;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.Features.UserRestrictions.Assign;

public sealed class AssignRestrictionToUserCommandValidator
    : AbstractValidator<AssignRestrictionToUserCommand>
{
    public AssignRestrictionToUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");

        RuleFor(x => x.ImportanceLevel)
            .NotEmpty().WithMessage("El nivel de importancia es obligatorio.")
            .Must(level => UserRestrictionImportanceLevels.IsValid(
                UserRestriction.NormalizeImportanceLevel(level)))
            .WithMessage(
                "El nivel de importancia indicado no es válido. " +
                $"Valores permitidos: {string.Join(", ", UserRestrictionImportanceLevels.All)}.")
            .When(
                x => !string.IsNullOrWhiteSpace(x.ImportanceLevel),
                ApplyConditionTo.CurrentValidator);
    }
}
