using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.List;

public sealed class ListBranchRestrictionsQueryValidator
    : AbstractValidator<ListBranchRestrictionsQuery>
{
    public ListBranchRestrictionsQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("El tamaño de página debe ser al menos 1.")
            .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede superar 100.");

        RuleFor(x => x.ComplianceLevel)
            .Must(level => Enum.IsDefined(level!.Value) && level.Value != 0)
            .WithMessage("El nivel de cumplimiento indicado no es válido. Valores permitidos: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).")
            .When(x => x.ComplianceLevel.HasValue);
    }
}
