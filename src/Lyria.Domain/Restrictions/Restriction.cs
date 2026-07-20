using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Restrictions;

public sealed partial class Restriction : AggregateRoot<RestrictionId>, IAuditableEntity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private Restriction()
    {
    }

    private Restriction(
        RestrictionId id,
        string name,
        string? description)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Restriction Create(
        RestrictionId id,
        string name,
        string? description)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        return new Restriction(id, normalizedName, normalizedDescription);
    }

    public void Update(
        string name,
        string? description)
    {
        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        Name = normalizedName;
        Description = normalizedDescription;
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

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new RestrictionException("El nombre de la restricción es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new RestrictionException(
                $"El nombre de la restricción debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new RestrictionException(
                $"El nombre de la restricción no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new RestrictionException(
                $"La descripción de la restricción no puede superar los {DescriptionMaxLength} caracteres.");
        }
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
