using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranchServices.List;

public sealed class ListBranchServicesQueryValidator
    : AbstractValidator<ListBranchServicesQuery>
{
    public ListBranchServicesQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("El tamaño de página debe ser al menos 1.")
            .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede superar 100.");
    }
}
