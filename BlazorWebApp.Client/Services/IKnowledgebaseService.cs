using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Interface for knowledgebase management operations
/// </summary>
public interface IKnowledgebaseService
{
    /// <summary>
    /// Process a document and add it to the knowledgebase
    /// </summary>
    Task<KnowledgebaseEntry> ProcessDocumentAsync(Stream fileStream, string fileName, IProgress<ProcessingProgress>? progress = null);
    
    /// <summary>
    /// Get all knowledgebase entries
    /// </summary>
    Task<List<KnowledgebaseEntry>> GetAllEntriesAsync();
    
    /// <summary>
    /// Delete a knowledgebase entry
    /// </summary>
    Task DeleteEntryAsync(Guid entryId);
    
    /// <summary>
    /// Search for similar chunks in the knowledgebase
    /// </summary>
    Task<List<KnowledgebaseChunk>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK = 5);
    
    /// <summary>
    /// Save the knowledgebase to storage
    /// </summary>
    Task SaveKnowledgebaseAsync();
    
    /// <summary>
    /// Load the knowledgebase from storage
    /// </summary>
    Task LoadKnowledgebaseAsync();
}
