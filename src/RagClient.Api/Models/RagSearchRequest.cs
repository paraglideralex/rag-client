namespace RagClient.Api.Models;

/// <summary>
/// Request body for POST /api/rag/search.
/// Maps to handler.RagSearchV1Request from the upstream API.
/// The stream field is always forced to false — streaming is not supported by this client.
/// </summary>
public class RagSearchRequest
{
    public string CollectionKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AgentRole { get; set; }
    public int? MaxDocuments { get; set; }
    public int? MaxTokens { get; set; }
    public double? MinSimilarity { get; set; }
    public double? Temperature { get; set; }
    public List<ContextMessage>? ContextMessages { get; set; }
}

public class ContextMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
