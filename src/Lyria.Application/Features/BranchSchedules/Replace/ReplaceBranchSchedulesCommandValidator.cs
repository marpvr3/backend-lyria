using FluentValidation;

namespace Lyria.Application.Features.BranchSchedules.Replace;

public sealed class ReplaceBranchSchedulesCommandValidator
    : AbstractValidator<ReplaceBranchSchedulesCommand>
{
    public ReplaceBranchSchedulesCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.Schedules)
            .NotNull().WithMessage("La lista de horarios es obligatoria.");

        RuleForEach(x => x.Schedules).ChildRules(item =>
        {
            item.RuleFor(s => s.DayOfWeek)
                .InclusiveBetween(1, 7)
                .WithMessage("El día de la semana debe estar entre 1 (lunes) y 7 (domingo).");

            item.When(s => !s.IsClosed, () =>
            {
                item.RuleFor(s => s.OpeningTime)
                    .NotEmpty()
                    .WithMessage("La hora de apertura es obligatoria para una franja abierta.");

                item.RuleFor(s => s.ClosingTime)
                    .NotEmpty()
                    .WithMessage("La hora de cierre es obligatoria para una franja abierta.");
            });

            item.When(s => s.IsClosed, () =>
            {
                item.RuleFor(s => s.OpeningTime)
                    .Null()
                    .WithMessage("Un día cerrado no puede tener hora de apertura.");

                item.RuleFor(s => s.ClosingTime)
                    .Null()
                    .WithMessage("Un día cerrado no puede tener hora de cierre.");

                item.RuleFor(s => s.CrossesMidnight)
                    .Equal(false)
                    .WithMessage("Un día cerrado no puede marcar cruce de medianoche.");
            });
        });
    }
}
