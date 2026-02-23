using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Interface for document extraction and generation services
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Extract text from a PDF document
    /// </summary>
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    
    /// <summary>
    /// Extract text from a DOCX document
    /// </summary>
    Task<string> ExtractTextFromDocxAsync(Stream docxStream);
    
    /// <summary>
    /// Generate an RFP response Word document
    /// </summary>
    Task<byte[]> GenerateRfpResponseDocumentAsync(
        List<RfpQuestion> questions, 
        string summary,
        IProgress<ProcessingProgress>? progress = null);
}
