using RagClient.Api.Models;

namespace RagClient.Api.Services;

public interface IRagService
{
    Task<ApiResponse<string>> SearchAsync(
        RagSearchRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<List<ChunkSearchResultModel>>> RawSearchAsync(
        RawSearchRequest request,
        CancellationToken ct = default);
}
