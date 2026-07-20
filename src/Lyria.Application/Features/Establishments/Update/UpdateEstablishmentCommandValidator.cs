using FluentValidation;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.Establishments.Update;

public sealed class UpdateEstablishmentCommandValidator
    : AbstractValidator<UpdateEstablishmentCommand>
{
    public UpdateEstablishmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("La categoría es obligatoria.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MinimumLength(Establishment.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {Establishment.NameMinLength} caracteres.")
            .MaximumLength(Establishment.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {Establishment.NameMaxLength} caracteres.");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("El slug es obligatorio.")
            .MinimumLength(Establishment.SlugMinLength)
            .WithMessage($"El slug debe tener al menos {Establishment.SlugMinLength} caracteres.")
            .MaximumLength(Establishment.SlugMaxLength)
            .WithMessage($"El slug no puede superar los {Establishment.SlugMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(Establishment.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar los {Establishment.DescriptionMaxLength} caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Website)
            .MaximumLength(Establishment.WebsiteMaxLength)
            .WithMessage($"El sitio web no puede superar los {Establishment.WebsiteMaxLength} caracteres.")
            .When(x => x.Website is not null);

        RuleFor(x => x.Instagram)
            .MaximumLength(Establishment.InstagramMaxLength)
            .WithMessage($"El Instagram no puede superar los {Establishment.InstagramMaxLength} caracteres.")
            .When(x => x.Instagram is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(Establishment.LogoUrlMaxLength)
            .WithMessage($"La URL del logotipo no puede superar los {Establishment.LogoUrlMaxLength} caracteres.")
            .When(x => x.LogoUrl is not null);

        RuleFor(x => x.ContactEmail)
            .MaximumLength(Establishment.ContactEmailMaxLength)
            .WithMessage($"El correo electrónico de contacto no puede superar los {Establishment.ContactEmailMaxLength} caracteres.")
            .When(x => x.ContactEmail is not null);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(Establishment.ContactPhoneMaxLength)
            .WithMessage($"El teléfono de contacto no puede superar los {Establishment.ContactPhoneMaxLength} caracteres.")
            .When(x => x.ContactPhone is not null);
    }
}
