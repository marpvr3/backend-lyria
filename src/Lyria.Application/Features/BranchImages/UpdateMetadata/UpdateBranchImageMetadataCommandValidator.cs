using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.UpdateMetadata;

public sealed class UpdateBranchImageMetadataCommandValidator
    : AbstractValidator<UpdateBranchImageMetadataCommand>
{
    public UpdateBranchImageMetadataCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("El identificador de la sede es obligatorio.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("El identificador de la imagen es obligatorio.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("La URL de la imagen es obligatoria.")
            .MaximumLength(BranchImage.UrlMaxLength)
            .WithMessage($"La URL de la imagen no puede superar los {BranchImage.UrlMaxLength} caracteres.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .MaximumLength(BranchImage.FileNameMaxLength)
            .WithMessage($"El nombre del archivo no puede superar los {BranchImage.FileNameMaxLength} caracteres.");

        RuleFor(x => x.AlternativeText)
            .MaximumLength(BranchImage.AlternativeTextMaxLength)
            .WithMessage($"El texto alternativo no puede superar los {BranchImage.AlternativeTextMaxLength} caracteres.")
            .When(x => x.AlternativeText is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El orden no puede ser negativo.");
    }
}
