using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Categories;

public sealed partial class EstablishmentCategory : AggregateRoot<EstablishmentCategoryId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int IconUrlMaxLength = 500;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private EstablishmentCategory()
    {
    }

    private EstablishmentCategory(
        EstablishmentCategoryId id,
        string name,
        string? description,
        string? iconUrl,
        int sortOrder)
        : base(id)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public static EstablishmentCategory Create(
        EstablishmentCategoryId id,
        string name,
        string? description,
        string? iconUrl,
        int sortOrder)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedIconUrl = NormalizeOptionalUrl(iconUrl);
        ValidateIconUrl(normalizedIconUrl);

        ValidateSortOrder(sortOrder);

        return new EstablishmentCategory(id, normalizedName, normalizedDescription, normalizedIconUrl, sortOrder);
    }

    public void UpdateDetails(string name, string? description, string? iconUrl, int sortOrder)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        string? normalizedIconUrl = NormalizeOptionalUrl(iconUrl);
        ValidateIconUrl(normalizedIconUrl);

        ValidateSortOrder(sortOrder);

        Name = normalizedName;
        Description = normalizedDescription;
        IconUrl = normalizedIconUrl;
        SortOrder = sortOrder;
    }

    public void ChangeSortOrder(int sortOrder)
    {
        ValidateSortOrder(sortOrder);
        SortOrder = sortOrder;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
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

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new EstablishmentCategoryException("El nombre de la categoría es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new EstablishmentCategoryException(
                $"El nombre de la categoría debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new EstablishmentCategoryException(
                $"El nombre de la categoría no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new EstablishmentCategoryException(
                $"La descripción de la categoría no puede superar los {DescriptionMaxLength} caracteres.");
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

    private static void ValidateIconUrl(string? iconUrl)
    {
        if (iconUrl is null)
        {
            return;
        }

        if (iconUrl.Length > IconUrlMaxLength)
        {
            throw new EstablishmentCategoryException(
                $"La URL del ícono no puede superar los {IconUrlMaxLength} caracteres.");
        }

        if (!Uri.TryCreate(iconUrl, UriKind.Absolute, out Uri? uri))
        {
            throw new EstablishmentCategoryException(
                "La URL del ícono debe ser una URL absoluta válida.");
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            throw new EstablishmentCategoryException(
                "La URL del ícono solo permite los esquemas HTTP y HTTPS.");
        }
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new EstablishmentCategoryException(
                "El orden de la categoría no puede ser negativo.");
        }
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
