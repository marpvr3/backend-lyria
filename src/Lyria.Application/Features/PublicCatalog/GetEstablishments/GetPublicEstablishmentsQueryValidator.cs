using FluentValidation;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishments;

public sealed class GetPublicEstablishmentsQueryValidator
    : AbstractValidator<GetPublicEstablishmentsQuery>
{
    private static readonly string[] AllowedSortBy = ["name", "newest", "branchcount"];
    private static readonly string[] AllowedSortDirection = ["asc", "desc"];
    private static readonly int[] AllowedComplianceLevels = [1, 2, 3];

    public GetPublicEstablishmentsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El tamaño de página debe ser al menos 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("El tamaño de página no puede superar 100.");

        RuleFor(x => x.SortBy)
            .Must(value => AllowedSortBy.Contains(value.ToLowerInvariant()))
            .WithMessage(x => $"El criterio de ordenamiento '{x.SortBy}' no es válido. Valores permitidos: name, newest, branchCount.");

        RuleFor(x => x.SortDirection)
            .Must(value => AllowedSortDirection.Contains(value.ToLowerInvariant()))
            .WithMessage(x => $"La dirección de ordenamiento '{x.SortDirection}' no es válida. Valores permitidos: asc, desc.");

        RuleFor(x => x.ComplianceLevel)
            .Must(value => value is null || AllowedComplianceLevels.Contains(value.Value))
            .WithMessage(x => $"El nivel de cumplimiento '{x.ComplianceLevel}' no es válido. Valores permitidos: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).");
    }
}
