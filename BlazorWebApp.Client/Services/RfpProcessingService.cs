using BlazorWebApp.Client.Models;
using System.Text.RegularExpressions;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Service for processing RFP documents, detecting questions, and generating answers
/// </summary>
public class RfpProcessingService : IRfpProcessingService
{
    private readonly IKnowledgebaseService _knowledgebaseService;
    private readonly IAIService _aiService;
    private readonly IDocumentService _documentService;
    
    public RfpProcessingService(
        IKnowledgebaseService knowledgebaseService,
        IAIService aiService,
        IDocumentService documentService)
    {
        _knowledgebaseService = knowledgebaseService;
        _aiService = aiService;
        _documentService = documentService;
    }
    
    public async Task<List<RfpQuestion>> ProcessRfpDocumentAsync(
        Stream fileStream, 
        string fileName,
        IProgress<ProcessingProgress>? progress = null)
    {
        var questions = new List<RfpQuestion>();
        var totalSteps = 6; // Upload, Extract, Detect, Embed, Retrieve, Generate
        
        // Step 1: Upload Complete (0 steps completed, starting step 1)
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
        
        fileStream.Position = 0;
        
        string text;
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            text = await _documentService.ExtractTextFromPdfAsync(fileStream);
        else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            text = await _documentService.ExtractTextFromDocxAsync(fileStream);
        else
            throw new NotSupportedException("File type not supported. Please upload a PDF or DOCX file.");
        
        // Step 3: Detect questions (2 steps completed, starting step 3)
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Detecting Questions", 
            CurrentItem = 2,
            TotalItems = totalSteps,
            Message = "Analyzing document for questions...",
            Status = ProcessingStatus.InProgress
        });
        
        var detectedQuestions = DetectQuestions(text);
        
        if (detectedQuestions.Count == 0)
        {
            progress?.Report(new ProcessingProgress 
            { 
                CurrentStep = "Complete", 
                CurrentItem = totalSteps,
                TotalItems = totalSteps,
                Message = "No questions detected in the document.",
                Status = ProcessingStatus.Completed
            });
            return questions;
        }
        
        // Step 4-6: Process each question (Embedding, Context Retrieval, Answer Generation)
        for (int i = 0; i < detectedQuestions.Count; i++)
        {
            var question = new RfpQuestion
            {
                Index = i + 1,
                QuestionText = detectedQuestions[i],
                Status = ProcessingStatus.InProgress
            };
            
            try
            {
                // Step 4: Generate embedding for question (3 steps completed, starting step 4)
                progress?.Report(new ProcessingProgress 
                { 
                    CurrentStep = "Generating Embeddings", 
                    CurrentItem = 3,
                    TotalItems = totalSteps,
                    Message = $"Embedding question {i + 1} of {detectedQuestions.Count}...",
                    Status = ProcessingStatus.InProgress
                });
                
                question.Embedding = await _aiService.GetEmbeddingAsync(detectedQuestions[i]);
                
                // Step 5: Retrieve relevant context (4 steps completed, starting step 5)
                progress?.Report(new ProcessingProgress 
                { 
                    CurrentStep = "Retrieving Context", 
                    CurrentItem = 4,
                    TotalItems = totalSteps,
                    Message = $"Finding relevant context for question {i + 1} of {detectedQuestions.Count}...",
                    Status = ProcessingStatus.InProgress
                });
                
                question.RelevantContext = await RetrieveContextAsync(question.Embedding, topK: 5);
                
                // Calculate confidence based on similarity scores
                question.ConfidenceScore = question.RelevantContext.Any() 
                    ? question.RelevantContext.Average(c => c.SimilarityScore) 
                    : 0;
                
                // Step 6: Generate answer (5 steps completed, starting step 6)
                progress?.Report(new ProcessingProgress 
                { 
                    CurrentStep = "Generating Answers", 
                    CurrentItem = 5,
                    TotalItems = totalSteps,
                    Message = $"Generating answer for question {i + 1} of {detectedQuestions.Count}...",
                    Status = ProcessingStatus.InProgress
                });
                
                var prompt = BuildAnswerPrompt(question.QuestionText, question.RelevantContext);
                question.GeneratedAnswer = await _aiService.GetCompletionAsync(prompt);
                question.EditedAnswer = question.GeneratedAnswer; // Default to generated
                question.Status = ProcessingStatus.Completed;
            }
            catch (Exception ex)
            {
                question.GeneratedAnswer = $"Unable to generate answer: {ex.Message}";
                question.EditedAnswer = question.GeneratedAnswer;
                question.Status = ProcessingStatus.Failed;
            }
            
            questions.Add(question);
        }
        
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Complete", 
            CurrentItem = totalSteps,
            TotalItems = totalSteps,
            Message = $"Processed {detectedQuestions.Count} questions successfully.",
            Status = ProcessingStatus.Completed
        });
        
        return questions;
    }
    
    public Task<List<string>> DetectQuestionsAsync(string text)
    {
        return Task.FromResult(DetectQuestions(text));
    }
    
    public async Task<string> GenerateAnswerAsync(RfpQuestion question, List<RetrievedContext> context)
    {
        var prompt = BuildAnswerPrompt(question.QuestionText, context);
        return await _aiService.GetCompletionAsync(prompt);
    }
    
    /// <summary>
    /// Detect questions in text using pattern matching
    /// </summary>
    private List<string> DetectQuestions(string text)
    {
        var questions = new List<string>();
        var patterns = new[]
        {
            @"^\d+[.)]\s+.+\?$",                    // Numbered questions: "1. What is your experience?"
            @"^[A-Z][^.!]*\?$",                     // Standard questions ending with ?
            @"(?:please|kindly)\s+(?:describe|explain|provide|list|detail|outline)",  // Imperative requests
        };
        
        // Preprocess: normalize the text to handle PDF extraction artifacts
        var normalizedText = NormalizeExtractedText(text);
        
        var sentences = SplitIntoSentences(normalizedText);
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 10)
                continue;
                
            if (IsQuestion(trimmed, patterns))
                questions.Add(trimmed);
        }
        
        return questions.Distinct().ToList();
    }
    
    /// <summary>
    /// Normalize text extracted from PDFs to handle common artifacts
    /// </summary>
    private string NormalizeExtractedText(string text)
    {
        // Remove common bullet characters that may appear as single letters (e.g., "G" from Wingdings bullets)
        // These typically appear on their own line before a question
        var lines = text.Split('\n');
        var normalizedLines = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip single-character bullet markers (common PDF extraction artifact)
            if (trimmed.Length == 1 && !char.IsLetterOrDigit(trimmed[0]) == false)
            {
                // Check if it's a common bullet substitute (G, l, n, o, etc. from symbol fonts)
                if ("GlnoO•●○◦▪▸►".Contains(trimmed[0]))
                    continue;
            }
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
                
            normalizedLines.Add(trimmed);
        }
        
        // Rejoin lines, merging those that appear to be continuations
        // (lines not ending with sentence terminators should be joined)
        var result = new List<string>();
        var currentSentence = "";
        
        foreach (var line in normalizedLines)
        {
            if (string.IsNullOrEmpty(currentSentence))
            {
                currentSentence = line;
            }
            else
            {
                // Check if previous line ends with sentence terminator
                var lastChar = currentSentence[^1];
                if (lastChar == '.' || lastChar == '!' || lastChar == '?' || lastChar == ':')
                {
                    result.Add(currentSentence);
                    currentSentence = line;
                }
                else
                {
                    // Join with previous line (it's a continuation)
                    currentSentence += " " + line;
                }
            }
        }
        
        // Don't forget the last sentence
        if (!string.IsNullOrEmpty(currentSentence))
            result.Add(currentSentence);
        
        return string.Join("\n", result);
    }
    
    /// <summary>
    /// Check if a sentence is a question
    /// </summary>
    private bool IsQuestion(string sentence, string[] patterns)
    {
        // Check if ends with question mark
        if (sentence.TrimEnd().EndsWith("?"))
            return true;
        
        // Check against regex patterns
        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                return true;
        }
        
        // Check for question starters (first word)
        var questionStarters = new[] { "what", "how", "why", "when", "where", "who", "which", "can", "could", "would", "will", "do", "does", "is", "are", "describe", "explain", "provide" };
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstWord = words.FirstOrDefault()?.ToLowerInvariant();
        if (firstWord != null && questionStarters.Contains(firstWord))
            return true;
        
        // Also check if a question starter appears near the beginning (within first 3 words)
        // This handles cases like bullet prefixes that weren't cleaned up
        for (int i = 1; i < Math.Min(3, words.Length); i++)
        {
            var word = words[i]?.ToLowerInvariant();
            if (word != null && questionStarters.Contains(word))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Split text into sentences
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        // Split on sentence boundaries and line breaks
        var pattern = @"(?<=[.!?\n])\s+";
        return Regex.Split(text, pattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();
    }
    
    /// <summary>
    /// Retrieve relevant context from the knowledgebase
    /// </summary>
    private async Task<List<RetrievedContext>> RetrieveContextAsync(float[] queryEmbedding, int topK = 5)
    {
        var entries = await _knowledgebaseService.GetAllEntriesAsync();
        var allChunks = entries.SelectMany(e => e.Chunks.Select(c => new 
        { 
            Chunk = c, 
            SourceFileName = e.FileName 
        }));
        
        var rankedChunks = allChunks
            .Select(item => new
            {
                item.Chunk,
                item.SourceFileName,
                Score = KnowledgebaseService.CosineSimilarity(queryEmbedding, item.Chunk.Embedding)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedContext
            {
                ChunkId = x.Chunk.Id,
                ChunkText = x.Chunk.Text,
                SimilarityScore = x.Score,
                SourceFileName = x.SourceFileName
            })
            .ToList();
        
        return rankedChunks;
    }
    
    /// <summary>
    /// Build the prompt for answer generation
    /// </summary>
    private string BuildAnswerPrompt(string question, List<RetrievedContext> context)
    {
        var contextText = context.Any() 
            ? string.Join("\n\n", context.Select(c => c.ChunkText))
            : "No relevant context available in the knowledgebase.";
        
        return $"""
            You are an expert RFP response writer. Based on the following context from our knowledge base, 
            provide a professional, accurate, and comprehensive answer to the question.
            
            CONTEXT:
            {contextText}
            
            QUESTION:
            {question}
            
            Provide a clear, professional response suitable for an RFP submission. 
            If the context doesn't contain enough information, indicate what additional details might be needed.
            """;
    }
}
