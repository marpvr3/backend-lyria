using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchImages.SetPrimary;

public sealed record SetBranchImagePrimaryCommand(
    Guid BranchId,
    Guid ImageId) : ICommand;
