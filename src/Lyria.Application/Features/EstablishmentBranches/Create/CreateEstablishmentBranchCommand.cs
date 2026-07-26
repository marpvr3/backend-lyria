using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranches.Create;

public sealed record CreateEstablishmentBranchCommand(
    Guid EstablishmentId,
    string Name,
    string Street,
    string? Number,
    string? AddressComplement,
    string? Neighborhood,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? TimeZoneId) : ICommand<EstablishmentBranchId>;
