using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.ApiKey;

public static class ApiKeyMiddlewareExtensions
{
    private const string HeaderName = "X-API-Key";
    private const string ConfigKey = "ApiKey";

    // Enforces an X-API-Key header when a non-empty "ApiKey" is configured.
    // If the config key is missing or empty, requests pass through unchanged -
    // this is deliberate back-compat so existing trays keep working while the
    // WebApi is being upgraded.
    public static IApplicationBuilder UseApiKey(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var expected = config[ConfigKey];

            if (string.IsNullOrEmpty(expected))
            {
                await next();
                return;
            }

            if (!ctx.Request.Headers.TryGetValue(HeaderName, out var provided)
                || !string.Equals(provided, expected, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next();
        });
    }
}
