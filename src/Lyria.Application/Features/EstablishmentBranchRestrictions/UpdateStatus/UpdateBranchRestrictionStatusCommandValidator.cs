using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;

public sealed class UpdateBranchRestrictionStatusCommandValidator
    : AbstractValidator<UpdateBranchRestrictionStatusCommand>
{
    public UpdateBranchRestrictionStatusCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.RestrictionId)
            .NotEmpty().WithMessage("El identificador de la restricción es obligatorio.");
    }
}
