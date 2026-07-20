using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchServices.Assign;

public sealed class AssignServiceToBranchCommandValidator
    : AbstractValidator<AssignServiceToBranchCommand>
{
    public AssignServiceToBranchCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("El identificador del servicio es obligatorio.");

        RuleFor(x => x.Observation)
            .MaximumLength(EstablishmentBranchService.ObservationMaxLength)
            .WithMessage($"La observación no puede superar los {EstablishmentBranchService.ObservationMaxLength} caracteres.")
            .When(x => x.Observation is not null);
    }
}
