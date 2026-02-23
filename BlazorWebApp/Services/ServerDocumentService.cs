using BlazorWebApp.Client.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlazorWebApp.Services;

/// <summary>
/// Server-side document generation service using Open XML SDK
/// </summary>
public class ServerDocumentService
{
    // Define colors as hex strings (BGR format for Open XML)
    private const string TitleColorHex = "8B0000";      // Dark Blue
    private const string QuestionColorHex = "FF0000";   // Blue
    private const string GrayColorHex = "808080";       // Gray

    public byte[] GenerateRfpResponseDocument(List<RfpQuestion> questions, string summary)
    {
        using var memoryStream = new MemoryStream();
        
        using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            // Add main document part
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Document Settings - Set margins (in twentieths of a point, 1 inch = 1440)
            var sectionProps = new SectionProperties(
                new PageMargin() { Top = 1440, Right = 1440U, Bottom = 1440, Left = 1440U }
            );

            // Title
            body.AppendChild(CreateParagraph("RFP Response", 48, true, false, TitleColorHex, JustificationValues.Center, "Calibri"));

            // Date
            body.AppendChild(CreateParagraph($"Generated: {DateTime.Now:MMMM dd, yyyy}", 24, false, true, GrayColorHex, JustificationValues.Center, "Calibri"));

            // Spacer
            body.AppendChild(new Paragraph());

            // Horizontal Rule
            body.AppendChild(CreateParagraph("────────────────────────────────────────────────────────────────────", 16, false, false, GrayColorHex, JustificationValues.Center, "Calibri"));

            // Spacer
            body.AppendChild(new Paragraph());

            // Executive Summary Section
            body.AppendChild(CreateParagraph("Executive Summary", 32, true, false, TitleColorHex, JustificationValues.Left, "Calibri", 200));

            body.AppendChild(CreateParagraph(summary, 22, false, false, null, JustificationValues.Left, "Calibri", 300));

            // Spacer
            body.AppendChild(new Paragraph());

            // Questions & Answers Section
            body.AppendChild(CreateParagraph("Questions and Responses", 32, true, false, TitleColorHex, JustificationValues.Left, "Calibri", 300));

            foreach (var question in questions)
            {
                // Question
                body.AppendChild(CreateParagraph($"Q{question.Index}: {question.QuestionText}", 24, true, false, QuestionColorHex, JustificationValues.Left, "Calibri", 100));

                // Answer
                var answerText = !string.IsNullOrEmpty(question.EditedAnswer)
                    ? question.EditedAnswer
                    : question.GeneratedAnswer;
                body.AppendChild(CreateParagraph(answerText ?? string.Empty, 22, false, false, null, JustificationValues.Left, "Calibri", 400));
            }

            // Spacer
            body.AppendChild(new Paragraph());

            // Footer
            body.AppendChild(CreateParagraph($"Document generated on {DateTime.Now:f}", 18, false, true, GrayColorHex, JustificationValues.Right, "Calibri"));

            // Add section properties at the end
            body.AppendChild(sectionProps);

            mainPart.Document.Save();
        }

        return memoryStream.ToArray();
    }

    private static Paragraph CreateParagraph(
        string text,
        int fontSizeHalfPoints,
        bool isBold,
        bool isItalic,
        string? colorHex,
        JustificationValues justification,
        string fontName,
        int spacingAfterTwips = 0)
    {
        var paragraph = new Paragraph();

        // Paragraph properties
        var paragraphProperties = new ParagraphProperties();
        paragraphProperties.AppendChild(new Justification() { Val = justification });
        
        if (spacingAfterTwips > 0)
        {
            paragraphProperties.AppendChild(new SpacingBetweenLines() { After = spacingAfterTwips.ToString() });
        }

        paragraph.AppendChild(paragraphProperties);

        // Run with text
        var run = new Run();
        var runProperties = new RunProperties();

        // Font
        runProperties.AppendChild(new RunFonts() { Ascii = fontName, HighAnsi = fontName });

        // Font size (in half-points)
        runProperties.AppendChild(new FontSize() { Val = fontSizeHalfPoints.ToString() });

        // Bold
        if (isBold)
        {
            runProperties.AppendChild(new Bold());
        }

        // Italic
        if (isItalic)
        {
            runProperties.AppendChild(new Italic());
        }

        // Color
        if (!string.IsNullOrEmpty(colorHex))
        {
            runProperties.AppendChild(new Color() { Val = colorHex });
        }

        run.AppendChild(runProperties);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        paragraph.AppendChild(run);

        return paragraph;
    }
}
