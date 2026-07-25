using FluentValidation;

namespace Lyria.Application.Features.BranchImages.GetByBranch;

public sealed class GetBranchImagesQueryValidator
    : AbstractValidator<GetBranchImagesQuery>
{
    public GetBranchImagesQueryValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");
    }
}
