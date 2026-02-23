using Blazored.LocalStorage;
using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Implementation of IRfpStateService for managing RFP processing state
/// </summary>
public class RfpStateService : IRfpStateService
{
    private readonly ILocalStorageService _localStorage;
    private const string StateKey = "rfp_state";
    
    private List<RfpQuestion> _questions = new();
    private string _projectName = "";
    private string _fileName = "";
    private bool _isInitialized = false;
    
    public RfpStateService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
    
    public List<RfpQuestion> Questions
    {
        get => _questions;
        set
        {
            _questions = value ?? new List<RfpQuestion>();
            OnQuestionsChanged?.Invoke();
        }
    }
    
    public string ProjectName
    {
        get => _projectName;
        set => _projectName = value ?? "";
    }
    
    public string FileName
    {
        get => _fileName;
        set => _fileName = value ?? "";
    }
    
    public bool HasQuestions => _questions.Any();
    
    public void Clear()
    {
        _questions = new List<RfpQuestion>();
        _projectName = "";
        _fileName = "";
        OnQuestionsChanged?.Invoke();
        _ = ClearStateAsync();
    }
    
    public event Action? OnQuestionsChanged;
    
    public async Task SaveStateAsync()
    {
        try
        {
            var state = new RfpStateData
            {
                Questions = _questions,
                ProjectName = _projectName,
                FileName = _fileName
            };
            await _localStorage.SetItemAsync(StateKey, state);
        }
        catch
        {
            // Ignore storage errors during prerender
        }
    }
    
    public async Task LoadStateAsync()
    {
        if (_isInitialized) return;
        
        try
        {
            var state = await _localStorage.GetItemAsync<RfpStateData>(StateKey);
            Console.WriteLine($"[RfpStateService] LoadStateAsync: state is {(state == null ? "null" : "not null")}");
            if (state != null)
            {
                _questions = state.Questions ?? new List<RfpQuestion>();
                _projectName = state.ProjectName ?? "";
                _fileName = state.FileName ?? "";
                _isInitialized = true;
                Console.WriteLine($"[RfpStateService] Loaded {_questions.Count} questions, project: {_projectName}");
                OnQuestionsChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RfpStateService] LoadStateAsync error: {ex.Message}");
            // Ignore storage errors during prerender
        }
    }
    
    private async Task ClearStateAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(StateKey);
        }
        catch
        {
            // Ignore storage errors during prerender
        }
    }
    
    private class RfpStateData
    {
        public List<RfpQuestion> Questions { get; set; } = new();
        public string ProjectName { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}
