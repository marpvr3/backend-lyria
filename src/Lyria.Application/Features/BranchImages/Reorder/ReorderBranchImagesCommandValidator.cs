using FluentValidation;

namespace Lyria.Application.Features.BranchImages.Reorder;

public sealed class ReorderBranchImagesCommandValidator
    : AbstractValidator<ReorderBranchImagesCommand>
{
    public ReorderBranchImagesCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.Images)
            .NotNull().WithMessage("La lista de imágenes es obligatoria.");

        RuleForEach(x => x.Images).ChildRules(item =>
        {
            item.RuleFor(i => i.ImageId)
                .NotEmpty().WithMessage("El identificador de la imagen es obligatorio.");

            item.RuleFor(i => i.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El orden no puede ser negativo.");
        });
    }
}
