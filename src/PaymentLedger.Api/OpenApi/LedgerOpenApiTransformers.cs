using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using PaymentLedger.Api.Http;

namespace PaymentLedger.Api.OpenApi;

internal static class LedgerOpenApiTransformers
{
    private const string BearerSchemeName = "Bearer";

    public static OpenApiOptions AddLedgerTransformers(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Get a token from POST /api/auth/dev-token.",
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = [],
            });
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            var requiresKey = context.Description.ActionDescriptor.EndpointMetadata.OfType<IdempotencyKeyRequirement>().Any();
            if (requiresKey)
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = LedgerHeaders.IdempotencyKey,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Unique key for this request. Retrying with the same key returns the original response.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, MaxLength = 128 },
                });
            }

            return Task.CompletedTask;
        });

        return options;
    }
}
