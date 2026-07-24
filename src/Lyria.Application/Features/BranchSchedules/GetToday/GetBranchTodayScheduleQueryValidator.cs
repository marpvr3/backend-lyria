using FluentValidation;

namespace Lyria.Application.Features.BranchSchedules.GetToday;

public sealed class GetBranchTodayScheduleQueryValidator
    : AbstractValidator<GetBranchTodayScheduleQuery>
{
    public GetBranchTodayScheduleQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");
    }
}
