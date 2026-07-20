using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Services;

public sealed partial class Service : AggregateRoot<ServiceId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int IconUrlMaxLength = 500;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private Service()
    {
    }

    private Service(
        ServiceId id,
        string name,
        string? description,
        string? iconUrl)
        : base(id)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
        IsActive = true;
    }

    public static Service Create(
        ServiceId id,
        string name,
        string? description,
        string? iconUrl)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedIconUrl = NormalizeIconUrl(iconUrl);
        ValidateIconUrl(normalizedIconUrl);

        return new Service(id, normalizedName, normalizedDescription, normalizedIconUrl);
    }

    public void Update(
        string name,
        string? description,
        string? iconUrl)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedIconUrl = NormalizeIconUrl(iconUrl);
        ValidateIconUrl(normalizedIconUrl);

        Name = normalizedName;
        Description = normalizedDescription;
        IconUrl = normalizedIconUrl;
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

    public static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    public static string? NormalizeIconUrl(string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(iconUrl))
        {
            return null;
        }

        return iconUrl.Trim();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ServiceException("El nombre del servicio es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new ServiceException(
                $"El nombre del servicio debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new ServiceException(
                $"El nombre del servicio no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new ServiceException(
                $"La descripción del servicio no puede superar los {DescriptionMaxLength} caracteres.");
        }
    }

    private static void ValidateIconUrl(string? iconUrl)
    {
        if (iconUrl is null)
        {
            return;
        }

        if (iconUrl.Length > IconUrlMaxLength)
        {
            throw new ServiceException(
                $"La URL del icono no puede superar los {IconUrlMaxLength} caracteres.");
        }

        if (!Uri.TryCreate(iconUrl, UriKind.Absolute, out Uri? uri))
        {
            throw new ServiceException("La URL del icono debe ser una URL absoluta válida.");
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            throw new ServiceException("La URL del icono solo permite esquemas http y https.");
        }
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
