using System.Net.Mail;
using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Domain.Establishments;

public sealed partial class Establishment : AggregateRoot<EstablishmentId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 150;
    public const int SlugMinLength = 2;
    public const int SlugMaxLength = 160;
    public const int DescriptionMaxLength = 1000;
    public const int WebsiteMaxLength = 250;
    public const int InstagramMaxLength = 200;
    public const int LogoUrlMaxLength = 500;
    public const int ContactEmailMaxLength = 254;
    public const int ContactPhoneMaxLength = 30;

    public EstablishmentCategoryId CategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Website { get; private set; }
    public string? Instagram { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private Establishment()
    {
    }

    private Establishment(
        EstablishmentId id,
        EstablishmentCategoryId categoryId,
        string name,
        string slug,
        string? description,
        string? website,
        string? instagram,
        string? logoUrl,
        string? contactEmail,
        string? contactPhone)
        : base(id)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        Description = description;
        Website = website;
        Instagram = instagram;
        LogoUrl = logoUrl;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        IsVerified = false;
        VerifiedAtUtc = null;
        IsActive = true;
    }

    public static Establishment Create(
        EstablishmentId id,
        EstablishmentCategoryId categoryId,
        string name,
        string slug,
        string? description,
        string? website,
        string? instagram,
        string? logoUrl,
        string? contactEmail,
        string? contactPhone)
    {
        ValidateCategoryId(categoryId);

        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedSlug = NormalizeSlug(slug);
        ValidateSlug(normalizedSlug);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedWebsite = NormalizeWebsite(website);
        ValidateWebsite(normalizedWebsite);

        string? normalizedInstagram = NormalizeInstagram(instagram);
        ValidateInstagram(normalizedInstagram);

        string? normalizedLogoUrl = NormalizeOptionalUrl(logoUrl);
        ValidateLogoUrl(normalizedLogoUrl);

        string? normalizedContactEmail = NormalizeContactEmail(contactEmail);
        ValidateContactEmail(normalizedContactEmail);

        string? normalizedContactPhone = NormalizeContactPhone(contactPhone);
        ValidateContactPhone(normalizedContactPhone);

        return new Establishment(
            id, categoryId, normalizedName, normalizedSlug,
            normalizedDescription, normalizedWebsite, normalizedInstagram,
            normalizedLogoUrl, normalizedContactEmail, normalizedContactPhone);
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description,
        string? website,
        string? instagram,
        string? logoUrl,
        string? contactEmail,
        string? contactPhone)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string normalizedSlug = NormalizeSlug(slug);
        ValidateSlug(normalizedSlug);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedWebsite = NormalizeWebsite(website);
        ValidateWebsite(normalizedWebsite);

        string? normalizedInstagram = NormalizeInstagram(instagram);
        ValidateInstagram(normalizedInstagram);

        string? normalizedLogoUrl = NormalizeOptionalUrl(logoUrl);
        ValidateLogoUrl(normalizedLogoUrl);

        string? normalizedContactEmail = NormalizeContactEmail(contactEmail);
        ValidateContactEmail(normalizedContactEmail);

        string? normalizedContactPhone = NormalizeContactPhone(contactPhone);
        ValidateContactPhone(normalizedContactPhone);

        Name = normalizedName;
        Slug = normalizedSlug;
        Description = normalizedDescription;
        Website = normalizedWebsite;
        Instagram = normalizedInstagram;
        LogoUrl = normalizedLogoUrl;
        ContactEmail = normalizedContactEmail;
        ContactPhone = normalizedContactPhone;
    }

    public void ChangeCategory(EstablishmentCategoryId categoryId)
    {
        ValidateCategoryId(categoryId);
        CategoryId = categoryId;
    }

    public void Verify(DateTime verifiedAtUtc)
    {
        if (!IsVerified)
        {
            IsVerified = true;
            VerifiedAtUtc = verifiedAtUtc;
        }
    }

    public void RevokeVerification()
    {
        IsVerified = false;
        VerifiedAtUtc = null;
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

    public static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        return slug.Trim().ToLowerInvariant();
    }

    public static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    public static string? NormalizeWebsite(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            return null;
        }

        return website.Trim();
    }

    public static string? NormalizeInstagram(string? instagram)
    {
        if (string.IsNullOrWhiteSpace(instagram))
        {
            return null;
        }

        return instagram.Trim();
    }

    private static void ValidateCategoryId(EstablishmentCategoryId categoryId)
    {
        if (categoryId.Value == Guid.Empty)
        {
            throw new EstablishmentException("La categoría del establecimiento es obligatoria.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new EstablishmentException("El nombre del establecimiento es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new EstablishmentException(
                $"El nombre del establecimiento debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new EstablishmentException(
                $"El nombre del establecimiento no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            throw new EstablishmentException("El slug del establecimiento es obligatorio.");
        }

        if (slug.Length < SlugMinLength)
        {
            throw new EstablishmentException(
                $"El slug del establecimiento debe tener al menos {SlugMinLength} caracteres.");
        }

        if (slug.Length > SlugMaxLength)
        {
            throw new EstablishmentException(
                $"El slug del establecimiento no puede superar los {SlugMaxLength} caracteres.");
        }

        if (!ValidSlugRegex().IsMatch(slug))
        {
            throw new EstablishmentException(
                "El slug solo permite letras ASCII minúsculas, números y guiones medios. " +
                "No puede comenzar ni terminar con guion ni contener guiones consecutivos.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new EstablishmentException(
                $"La descripción del establecimiento no puede superar los {DescriptionMaxLength} caracteres.");
        }
    }

    public static string? NormalizeOptionalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Trim();
    }

    public static string? NormalizeContactEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim();
    }

    public static string? NormalizeContactPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        return phone.Trim();
    }

    private static void ValidateWebsite(string? website)
    {
        if (website is null)
        {
            return;
        }

        if (website.Length > WebsiteMaxLength)
        {
            throw new EstablishmentException(
                $"El sitio web del establecimiento no puede superar los {WebsiteMaxLength} caracteres.");
        }

        if (!Uri.TryCreate(website, UriKind.Absolute, out Uri? uri))
        {
            throw new EstablishmentException(
                "El sitio web debe ser una URL absoluta válida.");
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            throw new EstablishmentException(
                "El sitio web solo permite los esquemas HTTP y HTTPS.");
        }
    }

    private static void ValidateInstagram(string? instagram)
    {
        if (instagram is not null && instagram.Length > InstagramMaxLength)
        {
            throw new EstablishmentException(
                $"El Instagram del establecimiento no puede superar los {InstagramMaxLength} caracteres.");
        }
    }

    private static void ValidateLogoUrl(string? logoUrl)
    {
        if (logoUrl is null)
        {
            return;
        }

        if (logoUrl.Length > LogoUrlMaxLength)
        {
            throw new EstablishmentException(
                $"La URL del logotipo no puede superar los {LogoUrlMaxLength} caracteres.");
        }

        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out Uri? uri))
        {
            throw new EstablishmentException(
                "La URL del logotipo debe ser una URL absoluta válida.");
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            throw new EstablishmentException(
                "La URL del logotipo solo permite los esquemas HTTP y HTTPS.");
        }
    }

    private static void ValidateContactEmail(string? email)
    {
        if (email is null)
        {
            return;
        }

        if (email.Length > ContactEmailMaxLength)
        {
            throw new EstablishmentException(
                $"El correo electrónico de contacto no puede superar los {ContactEmailMaxLength} caracteres.");
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            throw new EstablishmentException(
                "El correo electrónico de contacto no tiene un formato válido.");
        }
    }

    private static void ValidateContactPhone(string? phone)
    {
        if (phone is null)
        {
            return;
        }

        if (phone.Length > ContactPhoneMaxLength)
        {
            throw new EstablishmentException(
                $"El teléfono de contacto no puede superar los {ContactPhoneMaxLength} caracteres.");
        }

        if (!ValidPhoneRegex().IsMatch(phone))
        {
            throw new EstablishmentException(
                "El teléfono de contacto solo permite números, espacios, el signo +, guiones y paréntesis.");
        }
    }

    [GeneratedRegex(@"^[a-z0-9](-?[a-z0-9])*$")]
    private static partial Regex ValidSlugRegex();

    [GeneratedRegex(@"^[\d\s\+\-\(\)]+$")]
    private static partial Regex ValidPhoneRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
