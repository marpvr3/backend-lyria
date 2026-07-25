using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.Create;

public sealed record CreateBranchImageCommand(
    Guid BranchId,
    string Url,
    string FileName,
    string? AlternativeText,
    bool IsPrimary,
    int SortOrder) : ICommand<BranchImageId>;
