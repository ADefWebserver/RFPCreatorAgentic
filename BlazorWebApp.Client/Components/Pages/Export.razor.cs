using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Net.Http.Json;

namespace BlazorWebApp.Client.Components.Pages;

public partial class Export : ComponentBase
{
    [Inject] private IAIService AIService { get; set; } = default!;
    [Inject] private IDocumentService DocumentService { get; set; } = default!;
    [Inject] private IFileSystemService FileSystemService { get; set; } = default!;
    [Inject] private IRfpStateService RfpStateService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private string projectName = "RFP Response";
    private string fileName = "RFP_Response.docx";
    private string fileSize = "Calculating...";
    private string generatedTime = "just now";
    private bool isDownloading = false;
    private List<RfpQuestion> questions = new();
    
    private List<GenerationStep> generationSteps = new();
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadDataAsync();
            StateHasChanged();
        }
    }
    
    private async Task LoadDataAsync()
    {
        // Load state from persistent storage
        await RfpStateService.LoadStateAsync();
        
        // Load questions from state service
        if (RfpStateService.HasQuestions)
        {
            questions = RfpStateService.Questions;
            projectName = !string.IsNullOrEmpty(RfpStateService.ProjectName) 
                ? RfpStateService.ProjectName 
                : "RFP Response";
            fileName = $"{projectName.Replace(" ", "_")}_Response_{DateTime.Now:yyyyMMdd}.docx";
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Warning, "No Data", 
                "No RFP data available. Please process an RFP document first.");
            NavigationManager.NavigateTo("/");
            return;
        }
        
        // Initialize generation steps
        generationSteps = new List<GenerationStep>
        {
            new() { Name = "Compiling answers", Description = $"Gathered {questions.Count} responses" },
            new() { Name = "Formatting content", Description = "Standardizing answer format" },
            new() { Name = "Applying branding", Description = "Added header, footer, and fonts" },
            new() { Name = "Finalizing document", Description = "Ready for download" }
        };
        
        fileSize = $"~{(questions.Count * 2)} KB";
    }
    
    private async Task DownloadDocument()
    {
        isDownloading = true;
        StateHasChanged();
        
        try
        {
            // Generate summary for the document
            var summary = GenerateSummary();
            
            // Create the request with actual questions from state
            var request = new { Questions = questions, Summary = summary };
            var response = await HttpClient.PostAsJsonAsync("/api/generate-document", request);
            
            if (response.IsSuccessStatusCode)
            {
                var documentBytes = await response.Content.ReadAsByteArrayAsync();
                await FileSystemService.DownloadFileAsync(documentBytes, fileName);
                NotificationService.Notify(NotificationSeverity.Success, "Downloaded", $"Document '{fileName}' has been downloaded.");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to generate document: {error}");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to download document: {ex.Message}");
        }
        finally
        {
            isDownloading = false;
            StateHasChanged();
        }
    }
    
    private string GenerateSummary()
    {
        return $"""
            Thank you for the opportunity to respond to this Request for Proposal. 
            We have carefully reviewed all {questions.Count} questions and have provided 
            comprehensive responses that demonstrate our capabilities and commitment to 
            delivering exceptional results. We look forward to discussing our proposal 
            in further detail.
            """;
    }
    
    private void PreviewDocument()
    {
        NotificationService.Notify(NotificationSeverity.Info, "Preview", "Document preview is opening...");
    }
    
    private void ShareDocument()
    {
        NotificationService.Notify(NotificationSeverity.Info, "Share", "Share options will be available soon.");
    }
}

public class GenerationStep
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
