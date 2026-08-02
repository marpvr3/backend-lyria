using System.Text.RegularExpressions;
using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Roles;

public sealed partial class Role : AggregateRoot<RoleId>, IAuditableEntity
{
    public const int CodeMinLength = 2;
    public const int CodeMaxLength = 50;
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private Role()
    {
    }

    private Role(
        RoleId id,
        string code,
        string name,
        string? description)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Role Create(
        RoleId id,
        string code,
        string name,
        string? description)
    {
        string normalizedCode = NormalizeCode(code);
        ValidateCode(normalizedCode);

        string normalizedName = NormalizeName(name);
        ValidateName(normalizedName);

        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription);

        return new Role(id, normalizedCode, normalizedName, normalizedDescription);
    }

    public void Update(string name, string? description)
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
        if (IsActive)
        {
            throw new RoleException("El rol ya se encuentra activo.");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new RoleException("El rol ya se encuentra inactivo.");
        }

        IsActive = false;
    }

    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return code.Trim();
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

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new RoleException("El código del rol es obligatorio.");
        }

        if (code.Length < CodeMinLength)
        {
            throw new RoleException(
                $"El código del rol debe tener al menos {CodeMinLength} caracteres.");
        }

        if (code.Length > CodeMaxLength)
        {
            throw new RoleException(
                $"El código del rol no puede superar los {CodeMaxLength} caracteres.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new RoleException("El nombre del rol es obligatorio.");
        }

        if (name.Length < NameMinLength)
        {
            throw new RoleException(
                $"El nombre del rol debe tener al menos {NameMinLength} caracteres.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new RoleException(
                $"El nombre del rol no puede superar los {NameMaxLength} caracteres.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new RoleException(
                $"La descripción del rol no puede superar los {DescriptionMaxLength} caracteres.");
        }
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();
}
