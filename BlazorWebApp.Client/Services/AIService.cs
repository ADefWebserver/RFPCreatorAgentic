using Blazored.LocalStorage;
using BlazorWebApp.Client.Models;
using Microsoft.Extensions.AI;
using OpenAI;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// AI service implementation using Microsoft.Extensions.AI with OpenAI/Azure OpenAI
/// </summary>
public class AIService : IAIService
{
    private readonly ILocalStorageService _localStorage;
    private IChatClient? _chatClient;
    private IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private AIProviderSettings? _settings;
    
    private const string SettingsKey = "ai_provider_settings";
    
    public bool IsConfigured => _settings != null && !string.IsNullOrEmpty(_settings.ApiKey);
    
    public AIService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
    
    public async Task<AIProviderSettings?> LoadSettingsAsync()
    {
        try
        {
            _settings = await _localStorage.GetItemAsync<AIProviderSettings>(SettingsKey);
            if (_settings != null)
            {
                InitializeClients();
            }
            return _settings;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task SaveSettingsAsync(AIProviderSettings settings)
    {
        settings.LastUpdated = DateTime.UtcNow;
        await _localStorage.SetItemAsync(SettingsKey, settings);
        _settings = settings;
        InitializeClients();
    }
    
    private void InitializeClients()
    {
        if (_settings == null || string.IsNullOrEmpty(_settings.ApiKey)) return;
        
        try
        {
            var client = new OpenAIClient(_settings.ApiKey);
            _chatClient = client.GetChatClient(_settings.CompletionModel).AsIChatClient();
            _embeddingGenerator = client.GetEmbeddingClient(_settings.EmbeddingModel).AsIEmbeddingGenerator();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing AI clients: {ex.Message}");
            _chatClient = null;
            _embeddingGenerator = null;
        }
    }
    
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_embeddingGenerator == null)
            throw new InvalidOperationException("AI service not configured. Please configure your AI provider in settings.");
        
        var result = await _embeddingGenerator.GenerateAsync(text);
        return result.Vector.ToArray();
    }
    
    public async Task<string> GetCompletionAsync(string prompt)
    {
        if (_chatClient == null)
            throw new InvalidOperationException("AI service not configured. Please configure your AI provider in settings.");
        
        var response = await _chatClient.GetResponseAsync(prompt);
        return response.Text ?? string.Empty;
    }
    
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (!IsConfigured) return false;
            
            var testEmbed = await GetEmbeddingAsync("test connection");
            return testEmbed != null && testEmbed.Length > 0;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<(List<string> EmbeddingModels, List<string> CompletionModels)> GetAvailableModelsAsync(string apiKey)
    {
        var embeddingModels = new List<string>();
        var completionModels = new List<string>();
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            
            var response = await httpClient.GetAsync("https://api.openai.com/v1/models");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var modelsResponse = System.Text.Json.JsonSerializer.Deserialize<OpenAIModelsResponse>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (modelsResponse?.Data != null)
                {
                    foreach (var model in modelsResponse.Data.OrderBy(m => m.Id))
                    {
                        var id = model.Id;
                        if (id.Contains("embedding"))
                        {
                            embeddingModels.Add(id);
                        }
                        else if (id.StartsWith("gpt-") || id.StartsWith("o1") || id.StartsWith("o3") || id.StartsWith("o4"))
                        {
                            // Skip realtime, audio, and transcribe models for completion
                            if (!id.Contains("realtime") && !id.Contains("audio") && !id.Contains("transcribe"))
                            {
                                completionModels.Add(id);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching models: {ex.Message}");
        }
        
        // Return defaults if nothing found
        if (!embeddingModels.Any())
        {
            embeddingModels.AddRange(new[] { "text-embedding-ada-002", "text-embedding-3-small", "text-embedding-3-large" });
        }
        if (!completionModels.Any())
        {
            completionModels.AddRange(new[] { "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" });
        }
        
        return (embeddingModels, completionModels);
    }
}

internal class OpenAIModelsResponse
{
    public List<OpenAIModel>? Data { get; set; }
}

internal class OpenAIModel
{
    public string Id { get; set; } = string.Empty;
    public string OwnedBy { get; set; } = string.Empty;
}
