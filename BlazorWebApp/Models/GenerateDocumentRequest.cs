using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Models;

/// <summary>
/// Request DTO for document generation API
/// </summary>
public class GenerateDocumentRequest
{
    public List<RfpQuestion> Questions { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
