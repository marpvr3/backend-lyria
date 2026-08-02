namespace Lyria.Domain.Users.UserRestrictions;

/// <summary>
/// Niveles de importancia autorizados para la restricción de un usuario.
/// </summary>
public static class UserRestrictionImportanceLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";

    public const int MaxLength = 10;

    public static IReadOnlyList<string> All { get; } = [Low, Medium, High];

    public static bool IsValid(string? importanceLevel) =>
        importanceLevel is not null &&
        All.Contains(importanceLevel, StringComparer.Ordinal);
}
