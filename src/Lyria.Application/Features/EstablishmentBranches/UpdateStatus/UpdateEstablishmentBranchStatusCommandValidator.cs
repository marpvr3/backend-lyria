using FluentValidation;

namespace Lyria.Application.Features.EstablishmentBranches.UpdateStatus;

public sealed class UpdateEstablishmentBranchStatusCommandValidator
    : AbstractValidator<UpdateEstablishmentBranchStatusCommand>
{
    public UpdateEstablishmentBranchStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
