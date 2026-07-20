using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentBranches.Update;

public sealed record UpdateEstablishmentBranchCommand(
    Guid Id,
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
    string? Email) : ICommand;
