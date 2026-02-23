using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Net.Http.Json;

namespace BlazorWebApp.Client.Components.Pages;

public partial class Review : ComponentBase
{
    [Inject] private IAIService AIService { get; set; } = default!;
    [Inject] private IKnowledgebaseService KnowledgebaseService { get; set; } = default!;
    [Inject] private IRfpProcessingService RfpProcessingService { get; set; } = default!;
    [Inject] private IDocumentService DocumentService { get; set; } = default!;
    [Inject] private IFileSystemService FileSystemService { get; set; } = default!;
    [Inject] private IRfpStateService RfpStateService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private List<RfpQuestion> questions = new();
    private string projectName = "RFP Project";
    private int displayedCount = 10;
    private bool isSaving = false;
    private string lastSaveTime = "2 mins ago";
    private int lowConfidenceCount = 0;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadQuestions();
            CalculateLowConfidenceCount();
            StateHasChanged();
        }
    }
    
    private async Task LoadQuestions()
    {
        Console.WriteLine("[Review] LoadQuestions called");
        
        // Load state from persistent storage
        await RfpStateService.LoadStateAsync();
        
        Console.WriteLine($"[Review] HasQuestions: {RfpStateService.HasQuestions}, Count: {RfpStateService.Questions?.Count ?? 0}");
        
        // Load questions from RfpStateService
        if (RfpStateService.HasQuestions && RfpStateService.Questions is not null)
        {
            questions = RfpStateService.Questions;
            projectName = !string.IsNullOrEmpty(RfpStateService.ProjectName) 
                ? RfpStateService.ProjectName 
                : "RFP Project";
            Console.WriteLine($"[Review] Loaded {questions.Count} questions for project: {projectName}");
        }
        else
        {
            // No questions available, redirect to home
            Console.WriteLine("[Review] No questions found, redirecting to home");
            NotificationService.Notify(NotificationSeverity.Warning, "No RFP Data", 
                "Please upload an RFP document first.");
            NavigationManager.NavigateTo("/");
        }
    }
    
    private void CalculateLowConfidenceCount()
    {
        lowConfidenceCount = questions.Count(q => q.ConfidenceScore < 0.5);
    }
    
    private string GetConfidenceClass(double score)
    {
        return score switch
        {
            >= 0.8 => "high",
            >= 0.5 => "medium",
            _ => "low"
        };
    }
    
    private void ShowMoreQuestions()
    {
        displayedCount = Math.Min(displayedCount + 10, questions.Count);
        StateHasChanged();
    }
    
    private async Task SaveDraft()
    {
        isSaving = true;
        StateHasChanged();
        
        try
        {
            // Update the state service with edited questions
            RfpStateService.Questions = questions;
            
            await Task.Delay(500); // Brief delay for UX
            lastSaveTime = "just now";
            NotificationService.Notify(NotificationSeverity.Success, "Saved", "Draft saved successfully.");
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }
    
    private void CreateRfpResponse()
    {
        // Ensure state is saved before navigating
        RfpStateService.Questions = questions;
        
        // Navigate to export page
        NavigationManager.NavigateTo("/export");
    }
}
