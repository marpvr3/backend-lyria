using FluentValidation;

namespace Lyria.Application.Features.BranchImages.SetPrimary;

public sealed class SetBranchImagePrimaryCommandValidator
    : AbstractValidator<SetBranchImagePrimaryCommand>
{
    public SetBranchImagePrimaryCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("El identificador de la imagen es obligatorio.");
    }
}
