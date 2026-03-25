using Microsoft.AspNetCore.Mvc;
using RagClient.Api.Models;
using RagClient.Api.Services;

namespace RagClient.Api.Controllers;

[ApiController]
[Route("api/collections")]
public class CollectionController : ControllerBase
{
    private readonly ICollectionService _collectionService;

    public CollectionController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    /// <summary>
    /// Returns collections list with optional filtering and pagination.
    /// Proxies GET /v1/collection on the upstream RAG API.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCollections(
        [FromQuery] GetCollectionsQuery query,
        CancellationToken ct)
    {
        var result = await _collectionService.GetCollectionsAsync(query, ct);
        return Ok(result);
    }
}
