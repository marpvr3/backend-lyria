using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchImages.UpdateMetadata;

public sealed record UpdateBranchImageMetadataCommand(
    Guid BranchId,
    Guid ImageId,
    string Url,
    string FileName,
    string? AlternativeText,
    int SortOrder) : ICommand;
