using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Api.Extensions;

internal sealed class UniqueConstraintExceptionHandler : IExceptionHandler
{
    private const int SqlErrorDuplicateKey = 2627;
    private const int SqlErrorDuplicateIndex = 2601;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbUpdateException)
        {
            return false;
        }

        if (dbUpdateException.InnerException is not SqlException sqlException)
        {
            return false;
        }

        if (sqlException.Number is not (SqlErrorDuplicateKey or SqlErrorDuplicateIndex))
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto",
            Detail = "La operación no se completó porque ya existe un registro con los mismos datos únicos.",
            Extensions = { ["code"] = "Database.UniqueConstraintViolation" }
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
