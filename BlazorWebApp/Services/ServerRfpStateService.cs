using BlazorWebApp.Client.Models;
using BlazorWebApp.Client.Services;

namespace BlazorWebApp.Services;

/// <summary>
/// Server-side stub implementation of IRfpStateService.
/// This is used during pre-rendering and returns empty state.
/// The actual state is managed on the client side with localStorage.
/// </summary>
public class ServerRfpStateService : IRfpStateService
{
    public List<RfpQuestion> Questions { get; set; } = new();
    public string ProjectName { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool HasQuestions => false; // Always false on server - state is on client
    
    public void Clear()
    {
        Questions = new List<RfpQuestion>();
        ProjectName = "";
        FileName = "";
    }
    
#pragma warning disable CS0067 // Event is required by interface but unused in this server-side stub
    public event Action? OnQuestionsChanged;
#pragma warning restore CS0067
    
    // Server-side stubs - do nothing since state is managed on client
    public Task SaveStateAsync() => Task.CompletedTask;
    public Task LoadStateAsync() => Task.CompletedTask;
}
