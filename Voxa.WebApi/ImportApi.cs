using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Voxa.WebApi;

/// <summary>
/// The member import API's own OpenAPI document. It holds only the <c>/api/import</c> endpoints, so integrators get a
/// small contract, and it is served in every environment at <see cref="DocumentPath"/>; the board's API document stays
/// Development-only.
/// </summary>
public static class ImportApi
{
    public const string DocumentName = "import";
    public const string DocumentPath = "/api/import/openapi.json";

    public const string SecuritySchemeId = "importKey";

    public static AuthenticationBuilder AddImportApiKey(this AuthenticationBuilder builder) =>
        builder.AddScheme<AuthenticationSchemeOptions, ImportApiKeyHandler>(ImportApiKeyHandler.SchemeName, null);

    /// <summary>Splits the endpoints over the board's document (v1) and the import document.</summary>
    public static void AddImportDocument(this SwaggerGenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SwaggerDoc(DocumentName, new OpenApiInfo
        {
            Title = "Voxa member import",
            Version = "v1",
            Description = "Send the club's member list to a meeting's voter list. Authenticate with "
                          + "`Authorization: Bearer <key>`, where the key is the server's `Import__ApiKey`. See docs/import-api.md.",
        });
        options.DocInclusionPredicate(static (document, api) => document == DocumentName ? api.GroupName == DocumentName : api.GroupName is null);
        options.DocumentFilter<ImportDocumentFilter>();
    }

    /// <summary>Serves the import document, and nothing else, in every environment.</summary>
    public static void UseImportDocument(this WebApplication app) =>
        app.UseWhen(
            static context => context.Request.Path.Equals(DocumentPath, StringComparison.OrdinalIgnoreCase),
            static branch => branch.UseSwagger(static o => o.RouteTemplate = "api/{documentName}/openapi.json"));
}

/// <summary>Adds the bearer key to the import document; Swashbuckle creates it.</summary>
public sealed class ImportDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (context.DocumentName != ImportApi.DocumentName)
        {
            return;
        }

        swaggerDoc.Components ??= new OpenApiComponents();
        swaggerDoc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        swaggerDoc.Components.SecuritySchemes[ImportApi.SecuritySchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "The club's import key (Import__ApiKey on the server).",
        };
        swaggerDoc.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(ImportApi.SecuritySchemeId, swaggerDoc)] = [] }];
    }
}
