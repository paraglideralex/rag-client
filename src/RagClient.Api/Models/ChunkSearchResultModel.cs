namespace RagClient.Api.Models;

/// <summary>
/// A single chunk returned by raw vector search.
/// Maps to model.ChunkSearchResult from the upstream API.
/// </summary>
public class ChunkSearchResultModel
{
    public int Id { get; set; }
    public int FileId { get; set; }
    public string? FileName { get; set; }
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public Dictionary<string, object>? Meta { get; set; }
}
