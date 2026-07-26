using System.Net.Mail;
using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public sealed partial class EstablishmentBranch : Entity<EstablishmentBranchId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 150;
    public const int StreetMinLength = 2;
    public const int StreetMaxLength = 150;
    public const int NumberMaxLength = 20;
    public const int AddressComplementMaxLength = 150;
    public const int NeighborhoodMaxLength = 100;
    public const int CityMaxLength = 100;
    public const int ProvinceMaxLength = 100;
    public const int PostalCodeMaxLength = 20;
    public const int CountryMaxLength = 100;
    public const int PhoneMaxLength = 30;
    public const int WhatsAppMaxLength = 30;
    public const int EmailMaxLength = 254;
    public const int TimeZoneIdMaxLength = 100;

    public EstablishmentId EstablishmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string? Number { get; private set; }
    public string? AddressComplement { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? Phone { get; private set; }
    public string? WhatsApp { get; private set; }
    public string? Email { get; private set; }
    public string TimeZoneId { get; private set; } = null!;
    public decimal RatingAverage { get; private set; }
    public int TotalReviews { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private EstablishmentBranch()
    {
    }

    private EstablishmentBranch(
        EstablishmentBranchId id,
        EstablishmentId establishmentId,
        string name,
        string street,
        string? number,
        string? addressComplement,
        string? neighborhood,
        string? city,
        string? province,
        string? postalCode,
        string? country,
        decimal? latitude,
        decimal? longitude,
        string? phone,
        string? whatsApp,
        string? email,
        string timeZoneId)
        : base(id)
    {
        EstablishmentId = establishmentId;
        Name = name;
        Street = street;
        Number = number;
        AddressComplement = addressComplement;
        Neighborhood = neighborhood;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        Phone = phone;
        WhatsApp = whatsApp;
        Email = email;
        TimeZoneId = timeZoneId;
        RatingAverage = 0;
        TotalReviews = 0;
        IsActive = true;
    }

    public static EstablishmentBranch Create(
        EstablishmentBranchId id,
        EstablishmentId establishmentId,
        string name,
        string street,
        string? number,
        string? addressComplement,
        string? neighborhood,
        string? city,
        string? province,
        string? postalCode,
        string? country,
        decimal? latitude,
        decimal? longitude,
        string? phone,
        string? whatsApp,
        string? email,
        string timeZoneId)
    {
        ValidateEstablishmentId(establishmentId);

        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedStreet = NormalizeStreet(street);
        ValidateStreet(normalizedStreet);

        string? normalizedNumber = NormalizeOptionalString(number);
        ValidateNumber(normalizedNumber);

        string? normalizedAddressComplement = NormalizeOptionalString(addressComplement);
        ValidateAddressComplement(normalizedAddressComplement);

        string? normalizedNeighborhood = NormalizeOptionalString(neighborhood);
        ValidateNeighborhood(normalizedNeighborhood);

        string? normalizedCity = NormalizeOptionalString(city);
        ValidateCity(normalizedCity);

        string? normalizedProvince = NormalizeOptionalString(province);
        ValidateProvince(normalizedProvince);

        string? normalizedPostalCode = NormalizeOptionalString(postalCode);
        ValidatePostalCode(normalizedPostalCode);

        string? normalizedCountry = NormalizeOptionalString(country);
        ValidateCountry(normalizedCountry);

        ValidateCoordinates(latitude, longitude);

        string? normalizedPhone = NormalizeOptionalString(phone);
        ValidatePhone(normalizedPhone);

        string? normalizedWhatsApp = NormalizeOptionalString(whatsApp);
        ValidateWhatsApp(normalizedWhatsApp);

        string? normalizedEmail = NormalizeOptionalString(email);
        ValidateEmail(normalizedEmail);

        string normalizedTimeZoneId = NormalizeTimeZoneId(timeZoneId);
        ValidateTimeZoneId(normalizedTimeZoneId);

        return new EstablishmentBranch(
            id, establishmentId,
            normalizedName, normalizedStreet,
            normalizedNumber, normalizedAddressComplement,
            normalizedNeighborhood, normalizedCity,
            normalizedProvince, normalizedPostalCode,
            normalizedCountry,
            latitude, longitude,
            normalizedPhone, normalizedWhatsApp, normalizedEmail,
            normalizedTimeZoneId);
    }

    public void UpdateDetails(
        string name,
        string street,
        string? number,
        string? addressComplement,
        string? neighborhood,
        string? city,
        string? province,
        string? postalCode,
        string? country,
        decimal? latitude,
        decimal? longitude,
        string? phone,
        string? whatsApp,
        string? email)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedStreet = NormalizeStreet(street);
        ValidateStreet(normalizedStreet);

        string? normalizedNumber = NormalizeOptionalString(number);
        ValidateNumber(normalizedNumber);

        string? normalizedAddressComplement = NormalizeOptionalString(addressComplement);
        ValidateAddressComplement(normalizedAddressComplement);

        string? normalizedNeighborhood = NormalizeOptionalString(neighborhood);
        ValidateNeighborhood(normalizedNeighborhood);

        string? normalizedCity = NormalizeOptionalString(city);
        ValidateCity(normalizedCity);

        string? normalizedProvince = NormalizeOptionalString(province);
        ValidateProvince(normalizedProvince);

        string? normalizedPostalCode = NormalizeOptionalString(postalCode);
        ValidatePostalCode(normalizedPostalCode);

        string? normalizedCountry = NormalizeOptionalString(country);
        ValidateCountry(normalizedCountry);

        ValidateCoordinates(latitude, longitude);

        string? normalizedPhone = NormalizeOptionalString(phone);
        ValidatePhone(normalizedPhone);

        string? normalizedWhatsApp = NormalizeOptionalString(whatsApp);
        ValidateWhatsApp(normalizedWhatsApp);

        string? normalizedEmail = NormalizeOptionalString(email);
        ValidateEmail(normalizedEmail);

        Name = normalizedName;
        Street = normalizedStreet;
        Number = normalizedNumber;
        AddressComplement = normalizedAddressComplement;
        Neighborhood = normalizedNeighborhood;
        City = normalizedCity;
        Province = normalizedProvince;
        PostalCode = normalizedPostalCode;
        Country = normalizedCountry;
        Latitude = latitude;
        Longitude = longitude;
        Phone = normalizedPhone;
        WhatsApp = normalizedWhatsApp;
        Email = normalizedEmail;
    }

    public void UpdateTimeZoneId(string timeZoneId)
    {
        string normalizedTimeZoneId = NormalizeTimeZoneId(timeZoneId);
        ValidateTimeZoneId(normalizedTimeZoneId);
        TimeZoneId = normalizedTimeZoneId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        string trimmed = name.Trim();
        return MultipleSpacesRegex().Replace(trimmed, " ");
    }

    public static string NormalizeStreet(string street)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            return string.Empty;
        }

        return street.Trim();
    }

    public static string? NormalizeOptionalString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static void ValidateEstablishmentId(EstablishmentId establishmentId)
    {
        if (establishmentId.Value == Guid.Empty)
        {
            throw new EstablishmentBranchException("El establecimiento de la sede es obligatorio.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new EstablishmentBranchException("El nombre de la sede es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new EstablishmentBranchException(
                $"El nombre de la sede debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El nombre de la sede no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateStreet(string street)
    {
        if (string.IsNullOrEmpty(street))
        {
            throw new EstablishmentBranchException("La calle de la sede es obligatoria.");
        }

        if (street.Length < StreetMinLength)
        {
            throw new EstablishmentBranchException(
                $"La calle debe tener al menos {StreetMinLength} caracteres.");
        }

        if (street.Length > StreetMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La calle no puede superar los {StreetMaxLength} caracteres.");
        }
    }

    private static void ValidateNumber(string? number)
    {
        if (number is not null && number.Length > NumberMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El número no puede superar los {NumberMaxLength} caracteres.");
        }
    }

    private static void ValidateAddressComplement(string? addressComplement)
    {
        if (addressComplement is not null && addressComplement.Length > AddressComplementMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El complemento de dirección no puede superar los {AddressComplementMaxLength} caracteres.");
        }
    }

    private static void ValidateNeighborhood(string? neighborhood)
    {
        if (neighborhood is not null && neighborhood.Length > NeighborhoodMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El barrio no puede superar los {NeighborhoodMaxLength} caracteres.");
        }
    }

    private static void ValidateCity(string? city)
    {
        if (city is not null && city.Length > CityMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La ciudad no puede superar los {CityMaxLength} caracteres.");
        }
    }

    private static void ValidateProvince(string? province)
    {
        if (province is not null && province.Length > ProvinceMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La provincia no puede superar los {ProvinceMaxLength} caracteres.");
        }
    }

    private static void ValidatePostalCode(string? postalCode)
    {
        if (postalCode is not null && postalCode.Length > PostalCodeMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El código postal no puede superar los {PostalCodeMaxLength} caracteres.");
        }
    }

    private static void ValidateCountry(string? country)
    {
        if (country is not null && country.Length > CountryMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El país no puede superar los {CountryMaxLength} caracteres.");
        }
    }

    public static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude.HasValue && !longitude.HasValue)
        {
            throw new EstablishmentBranchException(
                "Si se informa la latitud, también debe informarse la longitud.");
        }

        if (!latitude.HasValue && longitude.HasValue)
        {
            throw new EstablishmentBranchException(
                "Si se informa la longitud, también debe informarse la latitud.");
        }

        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
        {
            throw new EstablishmentBranchException(
                "La latitud debe estar entre -90 y 90.");
        }

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
        {
            throw new EstablishmentBranchException(
                "La longitud debe estar entre -180 y 180.");
        }
    }

    private static void ValidatePhone(string? phone)
    {
        if (phone is null)
        {
            return;
        }

        if (phone.Length > PhoneMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El teléfono no puede superar los {PhoneMaxLength} caracteres.");
        }

        if (!ValidPhoneRegex().IsMatch(phone))
        {
            throw new EstablishmentBranchException(
                "El teléfono solo permite números, espacios, el signo +, guiones y paréntesis.");
        }
    }

    private static void ValidateWhatsApp(string? whatsApp)
    {
        if (whatsApp is null)
        {
            return;
        }

        if (whatsApp.Length > WhatsAppMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El WhatsApp no puede superar los {WhatsAppMaxLength} caracteres.");
        }

        if (!ValidPhoneRegex().IsMatch(whatsApp))
        {
            throw new EstablishmentBranchException(
                "El WhatsApp solo permite números, espacios, el signo +, guiones y paréntesis.");
        }
    }

    private static void ValidateEmail(string? email)
    {
        if (email is null)
        {
            return;
        }

        if (email.Length > EmailMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El correo electrónico no puede superar los {EmailMaxLength} caracteres.");
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            throw new EstablishmentBranchException(
                "El correo electrónico no tiene un formato válido.");
        }
    }

    public static string NormalizeTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return string.Empty;
        }

        return timeZoneId.Trim();
    }

    private static void ValidateTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId))
        {
            throw new EstablishmentBranchException("La zona horaria de la sede es obligatoria.");
        }

        if (timeZoneId.Length > TimeZoneIdMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La zona horaria no puede superar los {TimeZoneIdMaxLength} caracteres.");
        }
    }

    [GeneratedRegex(@"^[\d\s\+\-\(\)]+$")]
    private static partial Regex ValidPhoneRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
