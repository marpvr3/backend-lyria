using FluentValidation;
using Lyria.Domain.Services;

namespace Lyria.Application.Features.Services.Update;

public sealed class UpdateServiceCommandValidator
    : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MinimumLength(Service.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {Service.NameMinLength} caracteres.")
            .MaximumLength(Service.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {Service.NameMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(Service.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar los {Service.DescriptionMaxLength} caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.IconUrl)
            .MaximumLength(Service.IconUrlMaxLength)
            .WithMessage($"La URL del icono no puede superar los {Service.IconUrlMaxLength} caracteres.")
            .When(x => x.IconUrl is not null);
    }
}
