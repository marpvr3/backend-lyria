using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Services.Update;

public sealed record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl) : ICommand;
