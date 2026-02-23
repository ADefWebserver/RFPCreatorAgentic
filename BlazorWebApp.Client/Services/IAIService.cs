using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Interface for AI service operations including embedding and completion
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generate embedding vector for the given text
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text);
    
    /// <summary>
    /// Get AI completion for the given prompt
    /// </summary>
    Task<string> GetCompletionAsync(string prompt);
    
    /// <summary>
    /// Test the connection to the AI provider
    /// </summary>
    Task<bool> TestConnectionAsync();
    
    /// <summary>
    /// Load AI provider settings from storage
    /// </summary>
    Task<AIProviderSettings?> LoadSettingsAsync();
    
    /// <summary>
    /// Save AI provider settings to storage
    /// </summary>
    Task SaveSettingsAsync(AIProviderSettings settings);
    
    /// <summary>
    /// Check if AI service is configured and ready to use
    /// </summary>
    bool IsConfigured { get; }
    
    /// <summary>
    /// Get available models from OpenAI
    /// </summary>
    Task<(List<string> EmbeddingModels, List<string> CompletionModels)> GetAvailableModelsAsync(string apiKey);
}
