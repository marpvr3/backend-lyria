using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;

public sealed record AssignRestrictionToBranchCommand(
    Guid BranchId,
    Guid RestrictionId,
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation) : ICommand;
