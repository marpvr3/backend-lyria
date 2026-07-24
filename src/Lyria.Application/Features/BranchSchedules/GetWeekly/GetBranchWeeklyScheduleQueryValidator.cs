using FluentValidation;

namespace Lyria.Application.Features.BranchSchedules.GetWeekly;

public sealed class GetBranchWeeklyScheduleQueryValidator
    : AbstractValidator<GetBranchWeeklyScheduleQuery>
{
    public GetBranchWeeklyScheduleQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");
    }
}
