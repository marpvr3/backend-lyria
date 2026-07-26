using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.Replace;

public sealed class ReplaceBranchSpecialSchedulesCommandValidator
    : AbstractValidator<ReplaceBranchSpecialSchedulesCommand>
{
    public ReplaceBranchSpecialSchedulesCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.Reason)
            .MaximumLength(BranchSpecialSchedule.ReasonMaxLength)
            .WithMessage($"El motivo no puede superar los {BranchSpecialSchedule.ReasonMaxLength} caracteres.")
            .When(x => x.Reason is not null);

        RuleFor(x => x.TimeSlots)
            .NotNull().WithMessage("La lista de franjas horarias es obligatoria.");

        When(x => x.IsClosed, () =>
        {
            RuleFor(x => x.TimeSlots)
                .Must(slots => slots is null || slots.Count == 0)
                .WithMessage("Un día cerrado no puede tener franjas horarias.");
        });

        When(x => !x.IsClosed, () =>
        {
            RuleForEach(x => x.TimeSlots).ChildRules(item =>
            {
                item.RuleFor(s => s.OpeningTime)
                    .NotEmpty()
                    .WithMessage("La hora de apertura es obligatoria para una franja abierta.");

                item.RuleFor(s => s.ClosingTime)
                    .NotEmpty()
                    .WithMessage("La hora de cierre es obligatoria para una franja abierta.");
            });
        });
    }
}
