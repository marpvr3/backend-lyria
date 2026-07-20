using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.Update;

public sealed record UpdateBranchRestrictionCommand(
    Guid BranchId,
    Guid RestrictionId,
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation) : ICommand;
