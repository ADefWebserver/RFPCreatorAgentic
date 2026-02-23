namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents a document in the knowledgebase with its chunks and embeddings
/// </summary>
public class KnowledgebaseEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public float[] OriginalTextEmbedding { get; set; } = Array.Empty<float>();
    public List<KnowledgebaseChunk> Chunks { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long FileSizeBytes { get; set; }
}
