using Lyria.Domain.Abstractions;
using Lyria.Domain.Restrictions;

namespace Lyria.Domain.Establishments.Branches;

public sealed class EstablishmentBranchRestriction : IAuditableEntity
{
    public const int ObservationMaxLength = 500;

    public EstablishmentBranchId BranchId { get; private set; }
    public RestrictionId RestrictionId { get; private set; }
    public RestrictionComplianceLevel ComplianceLevel { get; private set; }
    public bool IsCertified { get; private set; }
    public string? Observation { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private EstablishmentBranchRestriction() { }

    private EstablishmentBranchRestriction(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        RestrictionComplianceLevel complianceLevel,
        bool isCertified,
        string? observation)
    {
        BranchId = branchId;
        RestrictionId = restrictionId;
        ComplianceLevel = complianceLevel;
        IsCertified = isCertified;
        Observation = observation;
        IsActive = true;
    }

    public static EstablishmentBranchRestriction Create(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        RestrictionComplianceLevel complianceLevel,
        bool isCertified,
        string? observation)
    {
        ValidateComplianceLevel(complianceLevel);

        string? normalizedObservation = NormalizeObservation(observation);
        ValidateObservation(normalizedObservation);

        return new EstablishmentBranchRestriction(
            branchId, restrictionId, complianceLevel, isCertified, normalizedObservation);
    }

    public void Update(
        RestrictionComplianceLevel complianceLevel,
        bool isCertified,
        string? observation)
    {
        ValidateComplianceLevel(complianceLevel);

        string? normalizedObservation = NormalizeObservation(observation);
        ValidateObservation(normalizedObservation);

        ComplianceLevel = complianceLevel;
        IsCertified = isCertified;
        Observation = normalizedObservation;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static string? NormalizeObservation(string? observation)
    {
        if (string.IsNullOrWhiteSpace(observation))
        {
            return null;
        }

        return observation.Trim();
    }

    private static void ValidateComplianceLevel(RestrictionComplianceLevel complianceLevel)
    {
        if (!Enum.IsDefined(complianceLevel) || complianceLevel == 0)
        {
            throw new EstablishmentBranchException(
                "El nivel de cumplimiento indicado no es válido.");
        }
    }

    private static void ValidateObservation(string? observation)
    {
        if (observation is not null && observation.Length > ObservationMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La observación no puede superar los {ObservationMaxLength} caracteres.");
        }
    }
}
