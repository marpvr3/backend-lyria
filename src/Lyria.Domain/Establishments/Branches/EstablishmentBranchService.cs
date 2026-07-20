using Lyria.Domain.Abstractions;
using Lyria.Domain.Services;

namespace Lyria.Domain.Establishments.Branches;

public sealed class EstablishmentBranchService : IAuditableEntity
{
    public const int ObservationMaxLength = 500;

    public EstablishmentBranchId BranchId { get; private set; }
    public ServiceId ServiceId { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? Observation { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private EstablishmentBranchService() { }

    private EstablishmentBranchService(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        bool isAvailable,
        string? observation)
    {
        BranchId = branchId;
        ServiceId = serviceId;
        IsAvailable = isAvailable;
        Observation = observation;
        IsActive = true;
    }

    public static EstablishmentBranchService Create(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        bool isAvailable,
        string? observation)
    {
        string? normalizedObservation = NormalizeObservation(observation);
        ValidateObservation(normalizedObservation);

        return new EstablishmentBranchService(branchId, serviceId, isAvailable, normalizedObservation);
    }

    public void Update(bool isAvailable, string? observation)
    {
        string? normalizedObservation = NormalizeObservation(observation);
        ValidateObservation(normalizedObservation);

        IsAvailable = isAvailable;
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

    private static void ValidateObservation(string? observation)
    {
        if (observation is not null && observation.Length > ObservationMaxLength)
        {
            throw new EstablishmentBranchException(
                $"La observación no puede superar los {ObservationMaxLength} caracteres.");
        }
    }
}
