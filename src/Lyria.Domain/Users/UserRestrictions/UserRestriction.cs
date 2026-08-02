using Lyria.Domain.Restrictions;

namespace Lyria.Domain.Users.UserRestrictions;

/// <summary>
/// Asociación entre un usuario y una restricción alimentaria.
/// La identidad está formada por el usuario y la restricción.
/// </summary>
public sealed class UserRestriction
{
    public UserId UserId { get; private set; }
    public RestrictionId RestrictionId { get; private set; }
    public string ImportanceLevel { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private UserRestriction()
    {
    }

    private UserRestriction(
        UserId userId,
        RestrictionId restrictionId,
        string importanceLevel,
        DateTime createdAtUtc)
    {
        UserId = userId;
        RestrictionId = restrictionId;
        ImportanceLevel = importanceLevel;
        CreatedAtUtc = createdAtUtc;
    }

    public static UserRestriction Create(
        UserId userId,
        RestrictionId restrictionId,
        string importanceLevel,
        DateTime createdAtUtc)
    {
        ValidateUserId(userId);
        ValidateRestrictionId(restrictionId);

        string normalizedImportanceLevel = NormalizeImportanceLevel(importanceLevel);
        ValidateImportanceLevel(normalizedImportanceLevel);

        return new UserRestriction(
            userId, restrictionId, normalizedImportanceLevel, createdAtUtc);
    }

    public void UpdateImportanceLevel(string importanceLevel)
    {
        string normalizedImportanceLevel = NormalizeImportanceLevel(importanceLevel);
        ValidateImportanceLevel(normalizedImportanceLevel);

        ImportanceLevel = normalizedImportanceLevel;
    }

    public static string NormalizeImportanceLevel(string? importanceLevel)
    {
        if (string.IsNullOrWhiteSpace(importanceLevel))
        {
            return string.Empty;
        }

        return importanceLevel.Trim();
    }

    private static void ValidateUserId(UserId userId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new UserRestrictionException("El usuario es obligatorio.");
        }
    }

    private static void ValidateRestrictionId(RestrictionId restrictionId)
    {
        if (restrictionId.Value == Guid.Empty)
        {
            throw new UserRestrictionException("La restricción es obligatoria.");
        }
    }

    private static void ValidateImportanceLevel(string importanceLevel)
    {
        if (string.IsNullOrEmpty(importanceLevel))
        {
            throw new UserRestrictionException("El nivel de importancia es obligatorio.");
        }

        if (!UserRestrictionImportanceLevels.IsValid(importanceLevel))
        {
            throw new UserRestrictionException(
                "El nivel de importancia indicado no es válido. " +
                $"Valores permitidos: {string.Join(", ", UserRestrictionImportanceLevels.All)}.");
        }
    }
}
