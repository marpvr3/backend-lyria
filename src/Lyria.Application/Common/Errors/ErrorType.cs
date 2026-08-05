namespace Lyria.Application.Common.Errors;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Failure,

    /// <summary>
    /// La solicitud no acredita una identidad válida. Se traduce a 401.
    /// Se agrega al final para no alterar los valores ordinales existentes.
    /// </summary>
    Unauthorized
}
