using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchImages.UpdateStatus;

public sealed record UpdateBranchImageStatusCommand(
    Guid BranchId,
    Guid ImageId,
    bool IsActive) : ICommand;
