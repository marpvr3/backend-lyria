using FluentValidation;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;

public sealed class GetBranchOpenStatusQueryValidator
    : AbstractValidator<GetBranchOpenStatusQuery>
{
    public GetBranchOpenStatusQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");
    }
}
