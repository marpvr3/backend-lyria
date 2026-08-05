using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Extensions;

internal static class ResultExtensions
{
    public static IActionResult ToProblemResult(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException(
                "No se puede convertir un resultado exitoso en un problema HTTP.");
        }

        return CreateProblemResult(result.Error);
    }

    private static ObjectResult CreateProblemResult(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(error.Type),
            Detail = error.Description,
            Extensions = { ["code"] = error.Code }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Error de validación",
        ErrorType.Unauthorized => "No autenticado",
        ErrorType.NotFound => "Recurso no encontrado",
        ErrorType.Conflict => "Conflicto",
        ErrorType.Forbidden => "Acceso denegado",
        ErrorType.Failure => "Error interno del servidor",
        _ => "Error interno del servidor"
    };
}
