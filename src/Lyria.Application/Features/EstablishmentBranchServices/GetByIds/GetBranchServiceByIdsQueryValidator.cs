using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranchServices.GetByIds;

public sealed class GetBranchServiceByIdsQueryValidator
    : AbstractValidator<GetBranchServiceByIdsQuery>
{
    public GetBranchServiceByIdsQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("El identificador del servicio es obligatorio.");
    }
}
