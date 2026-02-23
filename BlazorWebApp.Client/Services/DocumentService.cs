using BlazorWebApp.Client.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.JSInterop;
using System.Text;
using Xceed.Document.NET;
using Xceed.Words.NET;
using XColor = Xceed.Drawing.Color;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Document service for PDF extraction and Word document generation
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IAIService _aiService;
    private readonly IJSRuntime _jsRuntime;
    
    // Define colors using Xceed.Drawing.Color static properties
    private static readonly XColor TitleColor = XColor.DarkBlue;      // Professional dark blue
    private static readonly XColor QuestionColor = XColor.Blue;       // Standard blue  
    private static readonly XColor GrayColor = XColor.Gray;
    
    public DocumentService(IAIService aiService, IJSRuntime jsRuntime)
    {
        _aiService = aiService;
        _jsRuntime = jsRuntime;
    }
    
    public Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        using var reader = new PdfReader(pdfStream);
        using var document = new PdfDocument(reader);
        var text = new StringBuilder();
        
        for (int i = 1; i <= document.GetNumberOfPages(); i++)
        {
            var page = document.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            text.Append(PdfTextExtractor.GetTextFromPage(page, strategy));
            text.AppendLine();
        }
        
        return Task.FromResult(text.ToString());
    }
    
    public Task<string> ExtractTextFromDocxAsync(Stream docxStream)
    {
        using var document = DocX.Load(docxStream);
        var text = new StringBuilder();
        
        foreach (var paragraph in document.Paragraphs)
        {
            text.AppendLine(paragraph.Text);
        }
        
        return Task.FromResult(text.ToString());
    }
    
    public async Task<byte[]> GenerateRfpResponseDocumentAsync(
        List<RfpQuestion> questions, 
        string summary,
        IProgress<ProcessingProgress>? progress = null)
    {
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Creating document", 
            Message = "Initializing Word document...",
            Status = ProcessingStatus.InProgress
        });
        
        using var memoryStream = new MemoryStream();
        using var document = DocX.Create(memoryStream);
        
        // Document Settings
        document.MarginLeft = 72f;    // 1 inch
        document.MarginRight = 72f;
        document.MarginTop = 72f;
        document.MarginBottom = 72f;
        
        // Title
        var title = document.InsertParagraph("RFP Response");
        title.FontSize(24)
             .Bold()
             .Font("Calibri")
             .Color(TitleColor);
        title.Alignment = Alignment.center;
        
        // Date
        var date = document.InsertParagraph($"Generated: {DateTime.Now:MMMM dd, yyyy}");
        date.FontSize(12)
            .Italic()
            .Font("Calibri")
            .Color(GrayColor);
        date.Alignment = Alignment.center;
        
        document.InsertParagraph(); // Spacer
        
        // Horizontal Rule
        var rule = document.InsertParagraph("────────────────────────────────────────────────────────────────────");
        rule.FontSize(8).Color(GrayColor);
        rule.Alignment = Alignment.center;
        
        document.InsertParagraph(); // Spacer
        
        // Executive Summary Section
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Adding summary", 
            Message = "Writing executive summary...",
            Status = ProcessingStatus.InProgress
        });
        
        var summaryHeader = document.InsertParagraph("Executive Summary");
        summaryHeader.FontSize(16)
                     .Bold()
                     .Font("Calibri")
                     .Color(TitleColor);
        summaryHeader.SpacingAfter(10);
        
        var summaryText = document.InsertParagraph(summary);
        summaryText.FontSize(11)
                   .Font("Calibri");
        summaryText.SpacingAfter(15);
        
        document.InsertParagraph(); // Spacer
        
        // Questions & Answers Section
        var qaHeader = document.InsertParagraph("Questions and Responses");
        qaHeader.FontSize(16)
                .Bold()
                .Font("Calibri")
                .Color(TitleColor);
        qaHeader.SpacingAfter(15);
        
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Adding Q&A", 
            CurrentItem = 0,
            TotalItems = questions.Count,
            Message = "Writing questions and answers...",
            Status = ProcessingStatus.InProgress
        });
        
        foreach (var question in questions)
        {
            progress?.Report(new ProcessingProgress 
            { 
                CurrentStep = "Adding Q&A", 
                CurrentItem = question.Index,
                TotalItems = questions.Count,
                Message = $"Writing question {question.Index} of {questions.Count}...",
                Status = ProcessingStatus.InProgress
            });
            
            // Question
            var qPara = document.InsertParagraph($"Q{question.Index}: {question.QuestionText}");
            qPara.FontSize(12)
                 .Bold()
                 .Font("Calibri")
                 .Color(QuestionColor);
            qPara.SpacingAfter(5);
            
            // Answer
            var answerText = !string.IsNullOrEmpty(question.EditedAnswer) 
                ? question.EditedAnswer 
                : question.GeneratedAnswer;
            var aPara = document.InsertParagraph(answerText);
            aPara.FontSize(11)
                 .Font("Calibri");
            aPara.SpacingAfter(20);
        }
        
        // Footer
        document.InsertParagraph(); // Spacer
        var footer = document.InsertParagraph($"Document generated on {DateTime.Now:f}");
        footer.FontSize(9)
              .Italic()
              .Font("Calibri")
              .Color(GrayColor);
        footer.Alignment = Alignment.right;
        
        document.Save();
        
        progress?.Report(new ProcessingProgress 
        { 
            CurrentStep = "Complete", 
            CurrentItem = questions.Count,
            TotalItems = questions.Count,
            Message = "Document generated successfully!",
            Status = ProcessingStatus.Completed
        });
        
        return memoryStream.ToArray();
    }
    
    /// <summary>
    /// Build summary prompt for AI
    /// </summary>
    public string BuildSummaryPrompt(List<RfpQuestion> questions)
    {
        var qaText = string.Join("\n\n", questions.Select(q => 
            $"Q: {q.QuestionText}\nA: {(!string.IsNullOrEmpty(q.EditedAnswer) ? q.EditedAnswer : q.GeneratedAnswer)}"));
        
        return $"""
            You are an expert RFP response writer. Based on the following questions and answers 
            from an RFP response, write a professional executive summary paragraph that:
            
            1. Introduces the responding organization's capabilities
            2. Highlights key strengths demonstrated in the responses
            3. Expresses enthusiasm for the opportunity
            4. Is concise (2-3 paragraphs maximum)
            
            QUESTIONS AND ANSWERS:
            {qaText}
            
            Write the executive summary now:
            """;
    }
    
    /// <summary>
    /// Get fallback summary when AI is not available
    /// </summary>
    public string GetFallbackSummary(int questionCount)
    {
        return $"""
            Thank you for the opportunity to respond to this Request for Proposal. 
            We have carefully reviewed all {questionCount} questions and have provided 
            comprehensive responses that demonstrate our capabilities and commitment to 
            delivering exceptional results. We look forward to discussing our proposal 
            in further detail.
            """;
    }
}
