using RagClient.Api.Models;

namespace RagClient.Api.Infrastructure;

/// <summary>
/// Contract for communicating with the upstream RAG Core API.
/// Implementations are responsible for auth header injection and HTTP error handling.
/// </summary>
public interface IRagApiClient
{
    Task<ApiResponse<List<CollectionModel>>> GetCollectionsAsync(
        GetCollectionsQuery query,
        CancellationToken ct = default);

    Task<ApiResponse<string>> RagSearchAsync(
        RagSearchRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<List<ChunkSearchResultModel>>> RawSearchAsync(
        RawSearchRequest request,
        CancellationToken ct = default);
}
