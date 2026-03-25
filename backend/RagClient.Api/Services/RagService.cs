using RagClient.Api.Infrastructure;
using RagClient.Api.Models;

namespace RagClient.Api.Services;

public class RagService : IRagService
{
    private readonly IRagApiClient _ragApiClient;

    public RagService(IRagApiClient ragApiClient)
    {
        _ragApiClient = ragApiClient;
    }

    public Task<ApiResponse<string>> SearchAsync(
        RagSearchRequest request,
        CancellationToken ct = default)
        => _ragApiClient.RagSearchAsync(request, ct);

    public Task<ApiResponse<List<ChunkSearchResultModel>>> RawSearchAsync(
        RawSearchRequest request,
        CancellationToken ct = default)
        => _ragApiClient.RawSearchAsync(request, ct);
}
