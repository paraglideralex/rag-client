using Microsoft.AspNetCore.Mvc;
using RagClient.Api.Models;
using RagClient.Api.Services;

namespace RagClient.Api.Controllers;

[ApiController]
[Route("api/rag")]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;

    public RagController(IRagService ragService)
    {
        _ragService = ragService;
    }

    /// <summary>
    /// Performs a full RAG search: vector retrieval + LLM answer generation.
    /// Proxies POST /v1/rag/search on the upstream RAG API (always non-streaming).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody] RagSearchRequest request,
        CancellationToken ct)
    {
        var result = await _ragService.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Performs a raw vector search, returning matching chunks without LLM involvement.
    /// Proxies POST /v1/rag/raw-search on the upstream RAG API.
    /// </summary>
    [HttpPost("raw-search")]
    public async Task<IActionResult> RawSearch(
        [FromBody] RawSearchRequest request,
        CancellationToken ct)
    {
        var result = await _ragService.RawSearchAsync(request, ct);
        return Ok(result);
    }
}
