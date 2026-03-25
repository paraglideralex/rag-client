using RagClient.Api.Models;

namespace RagClient.Api.Middleware;

/// <summary>
/// Extracts the Bearer API key from the X-Api-Key request header and stores it
/// in the scoped <see cref="ApiKeyContext"/> so downstream services can use it
/// without depending on HttpContext directly.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApiKeyContext apiKeyContext)
    {
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var keyValues))
            apiKeyContext.ApiKey = keyValues.ToString();

        await _next(context);
    }
}
