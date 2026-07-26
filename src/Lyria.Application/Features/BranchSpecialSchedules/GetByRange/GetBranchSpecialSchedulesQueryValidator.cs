using FluentValidation;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetByRange;

public sealed class GetBranchSpecialSchedulesQueryValidator
    : AbstractValidator<GetBranchSpecialSchedulesQuery>
{
    private const int MaxRangeDays = 366;

    public GetBranchSpecialSchedulesQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithMessage("La fecha de inicio no puede ser posterior a la fecha de fin.");

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= MaxRangeDays)
            .WithMessage($"El rango de fechas no puede superar los {MaxRangeDays} días.")
            .When(x => x.From <= x.To);
    }
}
