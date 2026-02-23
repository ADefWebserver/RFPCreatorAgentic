namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents a question detected in an RFP document with its generated answer
/// </summary>
public class RfpQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Index { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string GeneratedAnswer { get; set; } = string.Empty;
    public string EditedAnswer { get; set; } = string.Empty;
    public List<RetrievedContext> RelevantContext { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
}
