using FluentValidation;

namespace Lyria.Application.Features.Restrictions.List;

public sealed class ListRestrictionsQueryValidator
    : AbstractValidator<ListRestrictionsQuery>
{
    public ListRestrictionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser al menos 1.");
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("El tamaño de página debe ser al menos 1.")
            .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede superar 100.");
    }
}
