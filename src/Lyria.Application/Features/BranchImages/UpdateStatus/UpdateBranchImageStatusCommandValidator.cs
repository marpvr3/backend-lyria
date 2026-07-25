using FluentValidation;

namespace Lyria.Application.Features.BranchImages.UpdateStatus;

public sealed class UpdateBranchImageStatusCommandValidator
    : AbstractValidator<UpdateBranchImageStatusCommand>
{
    public UpdateBranchImageStatusCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("El identificador de la imagen es obligatorio.");
    }
}
