namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents a chunk of text from a knowledgebase document with its embedding
/// </summary>
public class KnowledgebaseChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}
