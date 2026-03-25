namespace RagClient.Api.Models;

/// <summary>
/// Represents a knowledge collection in the RAG system.
/// Maps to model.Collection from the upstream API.
/// </summary>
public class CollectionModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool HybridSearchEnabled { get; set; }
    public bool RerankingEnabled { get; set; }
    public bool QueryExpansionEnabled { get; set; }
    public int DocumentsCount { get; set; }
    public int ChunksCount { get; set; }
    public long Size { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}
