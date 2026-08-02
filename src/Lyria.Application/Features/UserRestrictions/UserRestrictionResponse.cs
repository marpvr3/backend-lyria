namespace Lyria.Application.Features.UserRestrictions;

/// <summary>
/// Detalle de una restricción asociada a un usuario.
/// </summary>
/// <param name="UserId">Identificador del usuario.</param>
/// <param name="RestrictionId">Identificador de la restricción.</param>
/// <param name="RestrictionName">Nombre de la restricción.</param>
/// <param name="ImportanceLevel">Nivel de importancia: Low, Medium o High.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC en que se registró la asociación.</param>
public sealed record UserRestrictionResponse(
    Guid UserId,
    Guid RestrictionId,
    string RestrictionName,
    string ImportanceLevel,
    DateTime CreatedAtUtc);
