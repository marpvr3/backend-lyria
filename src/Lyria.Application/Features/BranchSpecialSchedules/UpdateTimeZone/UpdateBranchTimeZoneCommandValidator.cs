using FluentValidation;

namespace Lyria.Application.Features.BranchSpecialSchedules.UpdateTimeZone;

public sealed class UpdateBranchTimeZoneCommandValidator
    : AbstractValidator<UpdateBranchTimeZoneCommand>
{
    public UpdateBranchTimeZoneCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("La zona horaria es obligatoria.");
    }
}
