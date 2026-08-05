using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lyria.Api.Extensions;

/// <summary>
/// Marca en Swagger como protegidas únicamente las operaciones que exigen autenticación.
/// </summary>
/// <remarks>
/// Se aplica por operación en lugar de registrar un requisito global de seguridad:
/// un requisito global marcaría también el inicio de sesión, la renovación, el registro
/// móvil y el catálogo público, que son anónimos.
/// </remarks>
internal sealed class BearerSecurityOperationFilter : IOperationFilter
{
    /// <summary>
    /// Nombre del esquema de seguridad declarado en el documento OpenAPI.
    /// </summary>
    public const string SchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        bool allowsAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (allowsAnonymous)
        {
            return;
        }

        bool requiresAuthorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName)] = []
            }
        ];
    }
}
