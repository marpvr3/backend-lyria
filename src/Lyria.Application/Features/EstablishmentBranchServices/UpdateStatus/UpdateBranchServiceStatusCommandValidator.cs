using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;

public sealed class UpdateBranchServiceStatusCommandValidator
    : AbstractValidator<UpdateBranchServiceStatusCommand>
{
    public UpdateBranchServiceStatusCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("El identificador del servicio es obligatorio.");
    }
}
