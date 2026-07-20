using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;

public sealed class AssignRestrictionToBranchCommandValidator
    : AbstractValidator<AssignRestrictionToBranchCommand>
{
    public AssignRestrictionToBranchCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");

        RuleFor(x => x.ComplianceLevel)
            .Must(level => Enum.IsDefined(level) && level != 0)
            .WithMessage("El nivel de cumplimiento indicado no es válido. Valores permitidos: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).");

        RuleFor(x => x.Observation)
            .MaximumLength(EstablishmentBranchRestriction.ObservationMaxLength)
            .WithMessage($"La observación no puede superar los {EstablishmentBranchRestriction.ObservationMaxLength} caracteres.")
            .When(x => x.Observation is not null);
    }
}
