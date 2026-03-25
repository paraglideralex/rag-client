namespace RagClient.Api.Models;

/// <summary>
/// Scoped container for the Bearer API key extracted from the incoming request header.
/// Populated by <see cref="RagClient.Api.Middleware.ApiKeyMiddleware"/>.
/// </summary>
public class ApiKeyContext
{
    public string? ApiKey { get; set; }
}
