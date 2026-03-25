using RagClient.Api.Infrastructure;
using RagClient.Api.Models;

namespace RagClient.Api.Services;

public class CollectionService : ICollectionService
{
    private readonly IRagApiClient _ragApiClient;

    public CollectionService(IRagApiClient ragApiClient)
    {
        _ragApiClient = ragApiClient;
    }

    public Task<ApiResponse<List<CollectionModel>>> GetCollectionsAsync(
        GetCollectionsQuery query,
        CancellationToken ct = default)
        => _ragApiClient.GetCollectionsAsync(query, ct);
}
