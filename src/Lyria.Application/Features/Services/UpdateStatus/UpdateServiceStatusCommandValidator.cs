using FluentValidation;

namespace Lyria.Application.Features.Services.UpdateStatus;

public sealed class UpdateServiceStatusCommandValidator
    : AbstractValidator<UpdateServiceStatusCommand>
{
    public UpdateServiceStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
