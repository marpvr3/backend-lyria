using FluentValidation;

namespace Lyria.Application.Features.Establishments.UpdateStatus;

public sealed class UpdateEstablishmentStatusCommandValidator
    : AbstractValidator<UpdateEstablishmentStatusCommand>
{
    public UpdateEstablishmentStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");
    }
}
