using System.Diagnostics.CodeAnalysis;

namespace Lyria.Application.Common.Errors;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Error is the standard name for this domain concept")]
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);
}
