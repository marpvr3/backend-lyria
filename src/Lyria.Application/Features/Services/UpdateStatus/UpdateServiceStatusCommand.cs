using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Services.UpdateStatus;

public sealed record UpdateServiceStatusCommand(
    Guid Id,
    bool IsActive) : ICommand;
