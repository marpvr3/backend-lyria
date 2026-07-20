using FluentValidation;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.Update;

public sealed class UpdateEstablishmentBranchCommandValidator
    : AbstractValidator<UpdateEstablishmentBranchCommand>
{
    public UpdateEstablishmentBranchCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MinimumLength(EstablishmentBranch.NameMinLength)
            .WithMessage($"El nombre debe tener al menos {EstablishmentBranch.NameMinLength} caracteres.")
            .MaximumLength(EstablishmentBranch.NameMaxLength)
            .WithMessage($"El nombre no puede superar los {EstablishmentBranch.NameMaxLength} caracteres.");

        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("La calle es obligatoria.")
            .MinimumLength(EstablishmentBranch.StreetMinLength)
            .WithMessage($"La calle debe tener al menos {EstablishmentBranch.StreetMinLength} caracteres.")
            .MaximumLength(EstablishmentBranch.StreetMaxLength)
            .WithMessage($"La calle no puede superar los {EstablishmentBranch.StreetMaxLength} caracteres.");

        RuleFor(x => x.Number)
            .MaximumLength(EstablishmentBranch.NumberMaxLength)
            .WithMessage($"El número no puede superar los {EstablishmentBranch.NumberMaxLength} caracteres.")
            .When(x => x.Number is not null);

        RuleFor(x => x.AddressComplement)
            .MaximumLength(EstablishmentBranch.AddressComplementMaxLength)
            .WithMessage($"El complemento de dirección no puede superar los {EstablishmentBranch.AddressComplementMaxLength} caracteres.")
            .When(x => x.AddressComplement is not null);

        RuleFor(x => x.Neighborhood)
            .MaximumLength(EstablishmentBranch.NeighborhoodMaxLength)
            .WithMessage($"El barrio no puede superar los {EstablishmentBranch.NeighborhoodMaxLength} caracteres.")
            .When(x => x.Neighborhood is not null);

        RuleFor(x => x.City)
            .MaximumLength(EstablishmentBranch.CityMaxLength)
            .WithMessage($"La ciudad no puede superar los {EstablishmentBranch.CityMaxLength} caracteres.")
            .When(x => x.City is not null);

        RuleFor(x => x.Province)
            .MaximumLength(EstablishmentBranch.ProvinceMaxLength)
            .WithMessage($"La provincia no puede superar los {EstablishmentBranch.ProvinceMaxLength} caracteres.")
            .When(x => x.Province is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(EstablishmentBranch.PostalCodeMaxLength)
            .WithMessage($"El código postal no puede superar los {EstablishmentBranch.PostalCodeMaxLength} caracteres.")
            .When(x => x.PostalCode is not null);

        RuleFor(x => x.Country)
            .MaximumLength(EstablishmentBranch.CountryMaxLength)
            .WithMessage($"El país no puede superar los {EstablishmentBranch.CountryMaxLength} caracteres.")
            .When(x => x.Country is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("La latitud debe estar entre -90 y 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("La longitud debe estar entre -180 y 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Phone)
            .MaximumLength(EstablishmentBranch.PhoneMaxLength)
            .WithMessage($"El teléfono no puede superar los {EstablishmentBranch.PhoneMaxLength} caracteres.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.WhatsApp)
            .MaximumLength(EstablishmentBranch.WhatsAppMaxLength)
            .WithMessage($"El WhatsApp no puede superar los {EstablishmentBranch.WhatsAppMaxLength} caracteres.")
            .When(x => x.WhatsApp is not null);

        RuleFor(x => x.Email)
            .MaximumLength(EstablishmentBranch.EmailMaxLength)
            .WithMessage($"El correo electrónico no puede superar los {EstablishmentBranch.EmailMaxLength} caracteres.")
            .EmailAddress()
            .WithMessage("El correo electrónico no tiene un formato válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
