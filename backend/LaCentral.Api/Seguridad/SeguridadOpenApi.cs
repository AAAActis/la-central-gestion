using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LaCentral.Api.Seguridad;

/// <summary>
/// Declara el esquema de seguridad Bearer en el documento OpenAPI.
///
/// Sin esto Swagger no muestra el botón "Authorize" y no hay forma de
/// probar desde el navegador un endpoint protegido: el CA3 de HU-ACC-03
/// —que un Operador reciba el rechazo— quedaría sin poder verificarse.
/// </summary>
public sealed class SeguridadOpenApi : IOpenApiDocumentTransformer
{
    private const string EsquemaBearer = "Bearer";

    public Task TransformAsync(
        OpenApiDocument documento,
        OpenApiDocumentTransformerContext contexto,
        CancellationToken cancellationToken)
    {
        var esquema = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pegar solo el token que devuelve /api/acceso/login, " +
                          "sin escribir la palabra Bearer adelante."
        };

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[EsquemaBearer] = esquema;

        // El requisito global hace que Swagger adjunte el token en todas las
        // peticiones una vez que apretaste Authorize. El login igual sigue
        // siendo accesible sin token: lo decide [AllowAnonymous] en el
        // controlador, no este documento, que es solo descripción.
        documento.Security ??= new List<OpenApiSecurityRequirement>();
        documento.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(EsquemaBearer, documento)] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
