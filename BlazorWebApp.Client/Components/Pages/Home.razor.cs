using BlazorWebApp.Client.Components.Pages.Dialogs;
using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Net.Http.Json;

namespace BlazorWebApp.Client.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IAIService AIService { get; set; } = default!;
    [Inject] private IKnowledgebaseService KnowledgebaseService { get; set; } = default!;
    [Inject] private IRfpProcessingService RfpProcessingService { get; set; } = default!;
    [Inject] private IDocumentService DocumentService { get; set; } = default!;
    [Inject] private IFileSystemService FileSystemService { get; set; } = default!;
    [Inject] private IRfpStateService RfpStateService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    
    private List<KnowledgebaseEntry> knowledgebaseEntries = new();
    private List<RfpQuestion> questions = new();
    private List<ProcessingStep> processingSteps = new();
    
    private bool aiConfigured = false;
    private bool isProcessingKnowledgebase = false;
    private bool isProcessingRfp = false;
    
    private ProcessingProgress kbProgress = new();
    private ProcessingProgress rfpProgress = new();
    private ProcessingProgress currentProgress = new();
    private string currentFileName = "";
    private string currentProcessingId = "";
    
    protected override async Task OnInitializedAsync()
    {
        // Load AI settings
        var settings = await AIService.LoadSettingsAsync();
        aiConfigured = settings != null && !string.IsNullOrEmpty(settings.ApiKey);
        
        // Load knowledgebase entries
        knowledgebaseEntries = await KnowledgebaseService.GetAllEntriesAsync();
        
        // Initialize processing steps
        InitializeProcessingSteps();
    }
    
    private void InitializeProcessingSteps()
    {
        processingSteps = new List<ProcessingStep>
        {
            new() { Name = "Upload Complete", Description = "File successfully uploaded to secure storage." },
            new() { Name = "Text Extraction", Description = "Text and tables parsed successfully." },
            new() { Name = "Question Detection", Description = "Identifying RFP requirements and compliance criteria..." },
            new() { Name = "Embedding", Description = "Vectorizing content for semantic search" },
            new() { Name = "Context Retrieval", Description = "Matching content from knowledge base" },
            new() { Name = "Answer Generation", Description = "Drafting initial responses" }
        };
    }
    
    private void UpdateProcessingSteps(int currentStep)
    {
        for (int i = 0; i < processingSteps.Count; i++)
        {
            processingSteps[i].IsCompleted = i < currentStep;
            processingSteps[i].IsActive = i == currentStep;
        }
    }
    
    private async Task OpenConfigureAIDialog()
    {
        var existingSettings = await AIService.LoadSettingsAsync();
        
        var result = await DialogService.OpenAsync<ConfigureAIDialog>(
            "Configure AI Provider",
            new Dictionary<string, object?>
            {
                { "ExistingSettings", existingSettings! }
            },
            new DialogOptions
            {
                Width = "500px",
                Height = "auto",
                CloseDialogOnOverlayClick = false
            });
        
        if (result == true)
        {
            aiConfigured = AIService.IsConfigured;
            StateHasChanged();
        }
    }
    
    private async Task TriggerFileInput(string inputId)
    {
        await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('{inputId}').click()");
    }
    
    private async Task OnKnowledgebaseFileSelected(InputFileChangeEventArgs e)
    {
        if (!aiConfigured)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Warning", "Please configure AI provider first.");
            return;
        }
        
        var file = e.File;
        if (file == null) return;
        
        var validExtensions = new[] { ".pdf", ".docx", ".txt" };
        if (!validExtensions.Any(ext => file.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Please upload a PDF, DOCX, or TXT file.");
            return;
        }
        
        isProcessingKnowledgebase = true;
        currentFileName = file.Name;
        currentProcessingId = $"KB-{DateTime.Now:yyyyMMdd-HHmmss}";
        InitializeProcessingSteps();
        StateHasChanged();
        
        try
        {
            var progress = new Progress<ProcessingProgress>(p =>
            {
                currentProgress = p;
                
                // Update processing steps based on progress
                var stepIndex = p.CurrentStep switch
                {
                    "Uploading" => 0,
                    "Extracting Text" => 1,
                    "Detecting Questions" => 2,
                    "Generating Embeddings" => 3,
                    "Indexing" => 4,
                    "Finalizing" => 5,
                    "Complete" => processingSteps.Count, // Mark all steps completed
                    _ => -1
                };
                
                if (stepIndex >= 0)
                {
                    UpdateProcessingSteps(stepIndex);
                }
                
                InvokeAsync(StateHasChanged);
            });
            
            // Read file into memory stream
            using var stream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024).CopyToAsync(stream);
            stream.Position = 0;
            
            var entry = await KnowledgebaseService.ProcessDocumentAsync(stream, file.Name, progress);
            
            // Refresh the list
            knowledgebaseEntries = await KnowledgebaseService.GetAllEntriesAsync();
            
            NotificationService.Notify(NotificationSeverity.Success, "Success", 
                $"Added {file.Name} with {entry.Chunks.Count} chunks to knowledgebase.");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to process document: {ex.Message}");
        }
        finally
        {
            isProcessingKnowledgebase = false;
            StateHasChanged();
        }
    }
    
    private async Task DeleteKnowledgebaseEntry(Guid entryId)
    {
        var confirm = await DialogService.Confirm(
            "Are you sure you want to delete this document from the knowledgebase?",
            "Confirm Delete",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });
        
        if (confirm == true)
        {
            await KnowledgebaseService.DeleteEntryAsync(entryId);
            knowledgebaseEntries = await KnowledgebaseService.GetAllEntriesAsync();
            NotificationService.Notify(NotificationSeverity.Info, "Deleted", "Document removed from knowledgebase.");
        }
    }
    
    private async Task OnRfpFileSelected(InputFileChangeEventArgs e)
    {
        if (!aiConfigured)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Warning", "Please configure AI provider first.");
            return;
        }
        
        if (!knowledgebaseEntries.Any())
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Warning", "Please upload documents to the knowledgebase first.");
            return;
        }
        
        var file = e.File;
        if (file == null) return;
        
        if (!file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && 
            !file.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", "Please upload a PDF or DOCX file.");
            return;
        }
        
        isProcessingRfp = true;
        currentFileName = file.Name;
        currentProcessingId = $"RFP-{DateTime.Now:yyyyMMdd-HHmmss}";
        InitializeProcessingSteps();
        questions.Clear();
        StateHasChanged();
        
        try
        {
            var progress = new Progress<ProcessingProgress>(p =>
            {
                currentProgress = p;
                
                var stepIndex = p.CurrentStep switch
                {
                    "Uploading" => 0,
                    "Extracting Text" => 1,
                    "Detecting Questions" => 2,
                    "Generating Embeddings" => 3,
                    "Retrieving Context" => 4,
                    "Generating Answers" => 5,
                    "Complete" => processingSteps.Count, // Mark all steps completed
                    _ => -1
                };
                
                if (stepIndex >= 0)
                {
                    UpdateProcessingSteps(stepIndex);
                }
                
                InvokeAsync(StateHasChanged);
            });
            
            // Read file into memory stream
            using var stream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024).CopyToAsync(stream);
            stream.Position = 0;
            
            questions = await RfpProcessingService.ProcessRfpDocumentAsync(stream, file.Name, progress);
            
            if (questions.Any())
            {
                // Store questions in state service for Review page
                RfpStateService.Questions = questions;
                RfpStateService.FileName = file.Name;
                RfpStateService.ProjectName = Path.GetFileNameWithoutExtension(file.Name);
                
                // Save to persistent storage before navigation
                await RfpStateService.SaveStateAsync();
                
                // Navigate to review page
                NavigationManager.NavigateTo("/review");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Warning", 
                    "No questions were detected in the document.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to process RFP: {ex.Message}");
        }
        finally
        {
            isProcessingRfp = false;
            StateHasChanged();
        }
    }
    
    private void MinimizeProcessing()
    {
        // For now, just hide the modal but keep processing in background
        // In a real app, you'd want to track this separately
    }
    
    private void CancelProcessing()
    {
        isProcessingKnowledgebase = false;
        isProcessingRfp = false;
        NotificationService.Notify(NotificationSeverity.Info, "Cancelled", "Processing has been cancelled.");
        StateHasChanged();
    }
    
    private async Task OnCreateRfpResponse()
    {
        if (!questions.Any())
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Warning", "No questions to export.");
            return;
        }
        
        StateHasChanged();
        
        try
        {
            string summary;
            try
            {
                if (DocumentService is DocumentService docService)
                {
                    var summaryPrompt = docService.BuildSummaryPrompt(questions);
                    summary = await AIService.GetCompletionAsync(summaryPrompt);
                }
                else
                {
                    summary = GetFallbackSummary();
                }
            }
            catch
            {
                summary = GetFallbackSummary();
            }
            
            var request = new { Questions = questions, Summary = summary };
            var response = await HttpClient.PostAsJsonAsync("/api/generate-document", request);
            
            if (response.IsSuccessStatusCode)
            {
                var documentBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"RFP_Response_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                await FileSystemService.DownloadFileAsync(documentBytes, fileName);
                
                NotificationService.Notify(NotificationSeverity.Success, "Success", 
                    $"RFP response document '{fileName}' has been downloaded.");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to generate document: {error}");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to generate document: {ex.Message}");
        }
        finally
        {
            StateHasChanged();
        }
    }
    
    private string GetFallbackSummary()
    {
        return $"""
            Thank you for the opportunity to respond to this Request for Proposal. 
            We have carefully reviewed all {questions.Count} questions and have provided 
            comprehensive responses that demonstrate our capabilities and commitment to 
            delivering exceptional results. We look forward to discussing our proposal 
            in further detail.
            """;
    }
    
    private static BadgeStyle GetConfidenceBadgeStyle(double score)
    {
        return score switch
        {
            >= 0.8 => BadgeStyle.Success,
            >= 0.5 => BadgeStyle.Warning,
            _ => BadgeStyle.Danger
        };
    }
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

// Supporting models
public class ProcessingStep
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; }
}
