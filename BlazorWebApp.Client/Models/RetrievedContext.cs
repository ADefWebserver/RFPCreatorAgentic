namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents a context chunk retrieved from the knowledgebase during RAG
/// </summary>
public class RetrievedContext
{
    public Guid ChunkId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
}
