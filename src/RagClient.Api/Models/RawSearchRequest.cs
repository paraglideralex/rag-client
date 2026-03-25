namespace RagClient.Api.Models;

/// <summary>
/// Request body for POST /api/rag/raw-search.
/// Maps to handler.RagRawSearchV1Request from the upstream API.
/// </summary>
public class RawSearchRequest
{
    public string CollectionKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? MaxDocuments { get; set; }
    public double? MinSimilarity { get; set; }
}
