using Microsoft.AspNetCore.Builder;

namespace WebAPI.ApiKey;

public static class ApiKeyMiddlewareExtensions
{
    // Stub — real implementation lands in a later commit. Retained as a no-op so
    // Program.cs can reference app.UseApiKey() without a build break.
    public static IApplicationBuilder UseApiKey(this IApplicationBuilder app) => app;
}
