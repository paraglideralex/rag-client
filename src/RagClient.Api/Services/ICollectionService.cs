using RagClient.Api.Models;

namespace RagClient.Api.Services;

public interface ICollectionService
{
    Task<ApiResponse<List<CollectionModel>>> GetCollectionsAsync(
        GetCollectionsQuery query,
        CancellationToken ct = default);
}
