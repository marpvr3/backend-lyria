using FluentValidation;

namespace Lyria.Application.Features.Restrictions.UpdateStatus;

public sealed class UpdateRestrictionStatusCommandValidator
    : AbstractValidator<UpdateRestrictionStatusCommand>
{
    public UpdateRestrictionStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
