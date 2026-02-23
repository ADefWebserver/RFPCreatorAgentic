namespace BlazorWebApp.Client.Models;

/// <summary>
/// Settings for AI provider configuration
/// </summary>
public class AIProviderSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProviderType { get; set; } = "OpenAI";  // "OpenAI", "AzureOpenAI", etc.
    public string DisplayName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    public string CompletionModel { get; set; } = "gpt-4";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
