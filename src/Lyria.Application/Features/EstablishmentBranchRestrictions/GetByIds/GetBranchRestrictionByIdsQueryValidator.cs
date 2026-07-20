using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;

public sealed class GetBranchRestrictionByIdsQueryValidator
    : AbstractValidator<GetBranchRestrictionByIdsQuery>
{
    public GetBranchRestrictionByIdsQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");
    }
}
