using Blazored.LocalStorage;
using BlazorWebApp.Client.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Knowledgebase service for managing document storage and retrieval
/// </summary>
public class KnowledgebaseService : IKnowledgebaseService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IAIService _aiService;
    private readonly IDocumentService _documentService;
    
    private const string KnowledgebaseKey = "knowledgebase";
    private List<KnowledgebaseEntry> _entries = new();
    private bool _isLoaded = false;
    
    public KnowledgebaseService(
        ILocalStorageService localStorage,
        IAIService aiService,
        IDocumentService documentService)
    {
        _localStorage = localStorage;
        _aiService = aiService;
        _documentService = documentService;
    }
    
    public async Task<KnowledgebaseEntry> ProcessDocumentAsync(Stream fileStream, string fileName, IProgress<ProcessingProgress>? progress = null)
    {
        var totalSteps = 5; // Upload, Extract, Embed, Index, Finalize
        
        // Step 1: Upload (0 steps completed, starting step 1)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Uploading", 
            CurrentItem = 0,
            TotalItems = totalSteps,
            Message = $"File {fileName} uploaded successfully.",
            Status = ProcessingStatus.InProgress
        });
        
        // Step 2: Extract text (1 step completed, starting step 2)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Extracting Text", 
            CurrentItem = 1,
            TotalItems = totalSteps,
            Message = $"Processing {fileName}...",
            Status = ProcessingStatus.InProgress
        });
        
        // Reset stream position
        fileStream.Position = 0;
        
        // Extract text from PDF
        var text = await _documentService.ExtractTextFromPdfAsync(fileStream);
        
        // Create entry
        var entry = new KnowledgebaseEntry
        {
            FileName = fileName,
            OriginalText = text,
            FileSizeBytes = fileStream.Length
        };
        
        // Step 3: Generate embedding (2 steps completed, starting step 3)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Generating Embeddings", 
            CurrentItem = 2,
            TotalItems = totalSteps,
            Message = "Creating document embedding...",
            Status = ProcessingStatus.InProgress
        });
        
        // Generate full text embedding (truncate if too long)
        var truncatedText = text.Length > 8000 ? text.Substring(0, 8000) : text;
        entry.OriginalTextEmbedding = await _aiService.GetEmbeddingAsync(truncatedText);
        
        // Chunk the text
        var chunkTexts = ChunkText(text, 250);
        
        // Step 4: Indexing - process chunks (3 steps completed, starting step 4)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Indexing", 
            CurrentItem = 3,
            TotalItems = totalSteps,
            Message = $"Generating embeddings for {chunkTexts.Count} chunks...",
            Status = ProcessingStatus.InProgress
        });
        
        // Generate embeddings for each chunk
        for (int i = 0; i < chunkTexts.Count; i++)
        {
            progress?.Report(new ProcessingProgress 
            { 
                CurrentStep = "Indexing", 
                CurrentItem = 3,
                TotalItems = totalSteps,
                Message = $"Processing chunk {i + 1} of {chunkTexts.Count}...",
                Status = ProcessingStatus.InProgress
            });
            
            var chunk = new KnowledgebaseChunk
            {
                EntryId = entry.Id,
                Index = i,
                Text = chunkTexts[i],
                Embedding = await _aiService.GetEmbeddingAsync(chunkTexts[i])
            };
            entry.Chunks.Add(chunk);
        }
        
        // Step 5: Finalizing - add to collection and save (4 steps completed, starting step 5)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Finalizing", 
            CurrentItem = 4,
            TotalItems = totalSteps,
            Message = "Saving to knowledgebase...",
            Status = ProcessingStatus.InProgress
        });
        
        await LoadKnowledgebaseAsync();
        _entries.Add(entry);
        await SaveKnowledgebaseAsync();
        
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Complete", 
            CurrentItem = totalSteps,
            TotalItems = totalSteps,
            Message = $"Added {fileName} with {entry.Chunks.Count} chunks to knowledgebase.",
            Status = ProcessingStatus.Completed
        });
        
        return entry;
    }
    
    public async Task<List<KnowledgebaseEntry>> GetAllEntriesAsync()
    {
        await LoadKnowledgebaseAsync();
        return _entries;
    }
    
    public async Task DeleteEntryAsync(Guid entryId)
    {
        await LoadKnowledgebaseAsync();
        _entries.RemoveAll(e => e.Id == entryId);
        await SaveKnowledgebaseAsync();
    }
    
    public async Task<List<KnowledgebaseChunk>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK = 5)
    {
        await LoadKnowledgebaseAsync();
        
        var allChunks = _entries.SelectMany(e => e.Chunks);
        
        var rankedChunks = allChunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = CosineSimilarity(queryEmbedding, chunk.Embedding)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
        
        return rankedChunks;
    }
    
    public async Task SaveKnowledgebaseAsync()
    {
        await _localStorage.SetItemAsync(KnowledgebaseKey, _entries);
    }
    
    public async Task LoadKnowledgebaseAsync()
    {
        if (_isLoaded) return;
        
        try
        {
            var stored = await _localStorage.GetItemAsync<List<KnowledgebaseEntry>>(KnowledgebaseKey);
            _entries = stored ?? new List<KnowledgebaseEntry>();
            _isLoaded = true;
        }
        catch
        {
            _entries = new List<KnowledgebaseEntry>();
            _isLoaded = true;
        }
    }
    
    /// <summary>
    /// Chunk text into smaller pieces, respecting sentence boundaries
    /// </summary>
    public List<string> ChunkText(string text, int chunkSize = 250)
    {
        var chunks = new List<string>();
        var sentences = SplitIntoSentences(text);
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            currentChunk.Append(sentence + " ");
        }
        
        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString().Trim());
        
        return chunks;
    }
    
    /// <summary>
    /// Split text into sentences
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        var pattern = @"(?<=[.!?])\s+";
        return Regex.Split(text, pattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
    
    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    public static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length || vectorA.Length == 0)
            return 0;
        
        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;
        
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }
        
        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;
        
        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
