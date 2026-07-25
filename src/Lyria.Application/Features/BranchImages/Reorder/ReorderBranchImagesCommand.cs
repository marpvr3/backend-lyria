using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchImages.Reorder;

public sealed record ReorderBranchImagesCommand(
    Guid BranchId,
    IReadOnlyList<ImageOrderItem> Images) : ICommand;

public sealed record ImageOrderItem(
    Guid ImageId,
    int SortOrder);
