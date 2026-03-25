using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using RagClient.Api.Models;

namespace RagClient.Api.Infrastructure;

/// <summary>
/// HttpClient-based implementation of <see cref="IRagApiClient"/>.
/// Injects the Bearer token per-request from <see cref="ApiKeyContext"/> (scoped, set by middleware).
/// Uses snake_case JSON serialization to match the upstream API contract.
/// </summary>
public class RagApiClient : IRagApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiKeyContext _apiKeyContext;
    private readonly ILogger<RagApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public RagApiClient(
        HttpClient httpClient,
        ApiKeyContext apiKeyContext,
        ILogger<RagApiClient> logger)
    {
        _httpClient = httpClient;
        _apiKeyContext = apiKeyContext;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CollectionModel>>> GetCollectionsAsync(
        GetCollectionsQuery query,
        CancellationToken ct = default)
    {
        var queryParams = BuildCollectionsParams(query);
        var url = QueryHelpers.AddQueryString("api/v1/collection", queryParams);

        using var request = CreateRequest(HttpMethod.Get, url);
        return await SendAsync<List<CollectionModel>>(request, ct);
    }

    public async Task<ApiResponse<string>> RagSearchAsync(
        RagSearchRequest body,
        CancellationToken ct = default)
    {
        // stream is always false — this client does not support SSE streaming
        var payload = new
        {
            body.CollectionKey,
            body.Message,
            body.AgentRole,
            body.MaxDocuments,
            body.MaxTokens,
            body.MinSimilarity,
            body.Temperature,
            body.ContextMessages,
            Stream = false
        };

        using var request = CreateRequest(HttpMethod.Post, "api/v1/rag/search");
        request.Content = Serialize(payload);
        return await SendAsync<string>(request, ct);
    }

    public async Task<ApiResponse<List<ChunkSearchResultModel>>> RawSearchAsync(
        RawSearchRequest body,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/v1/rag/raw-search");
        request.Content = Serialize(body);
        return await SendAsync<List<ChunkSearchResultModel>>(request, ct);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var message = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_apiKeyContext.ApiKey))
            message.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKeyContext.ApiKey);
        return message;
    }

    private static StringContent Serialize<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
            return result ?? ApiResponse<T>.Error("Empty or invalid response from upstream API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to RAG API failed: {Message}", ex.Message);
            return ApiResponse<T>.Error($"Upstream connection error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return ApiResponse<T>.Error("Request timed out or was cancelled");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize RAG API response");
            return ApiResponse<T>.Error($"Response parse error: {ex.Message}");
        }
    }

    private static Dictionary<string, string?> BuildCollectionsParams(GetCollectionsQuery q)
    {
        var p = new Dictionary<string, string?>();

        if (q.Id.HasValue) p["id"] = q.Id.ToString();
        if (!string.IsNullOrEmpty(q.Name)) p["name"] = q.Name;
        if (!string.IsNullOrEmpty(q.NameEq)) p["name_eq"] = q.NameEq;
        if (!string.IsNullOrEmpty(q.Key)) p["key"] = q.Key;
        if (!string.IsNullOrEmpty(q.KeyEq)) p["key_eq"] = q.KeyEq;
        if (!string.IsNullOrEmpty(q.Description)) p["description"] = q.Description;
        if (!string.IsNullOrEmpty(q.CreatedFrom)) p["created_from"] = q.CreatedFrom;
        if (!string.IsNullOrEmpty(q.CreatedTo)) p["created_to"] = q.CreatedTo;
        if (!string.IsNullOrEmpty(q.UpdatedFrom)) p["updated_from"] = q.UpdatedFrom;
        if (!string.IsNullOrEmpty(q.UpdatedTo)) p["updated_to"] = q.UpdatedTo;
        if (!string.IsNullOrEmpty(q.OrderBy)) p["order_by"] = q.OrderBy;
        if (!string.IsNullOrEmpty(q.OrderDir)) p["order_dir"] = q.OrderDir;
        if (q.Page.HasValue) p["page"] = q.Page.ToString();
        if (q.Limit.HasValue) p["limit"] = q.Limit.ToString();

        return p;
    }
}
