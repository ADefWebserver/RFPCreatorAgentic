using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Interface for processing RFP documents
/// </summary>
public interface IRfpProcessingService
{
    /// <summary>
    /// Process an RFP document: detect questions, retrieve context, and generate answers
    /// </summary>
    Task<List<RfpQuestion>> ProcessRfpDocumentAsync(
        Stream fileStream, 
        string fileName, 
        IProgress<ProcessingProgress>? progress = null);
    
    /// <summary>
    /// Detect questions in the given text
    /// </summary>
    Task<List<string>> DetectQuestionsAsync(string text);
    
    /// <summary>
    /// Generate an answer for a specific question with the given context
    /// </summary>
    Task<string> GenerateAnswerAsync(RfpQuestion question, List<RetrievedContext> context);
}
