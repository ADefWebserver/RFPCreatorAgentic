using BlazorWebApp.Client.Models;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// Service for managing RFP processing state across pages
/// </summary>
public interface IRfpStateService
{
    /// <summary>
    /// Gets or sets the current list of processed RFP questions
    /// </summary>
    List<RfpQuestion> Questions { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the project/RFP currently being processed
    /// </summary>
    string ProjectName { get; set; }
    
    /// <summary>
    /// Gets or sets the original RFP file name
    /// </summary>
    string FileName { get; set; }
    
    /// <summary>
    /// Returns true if there are questions loaded
    /// </summary>
    bool HasQuestions { get; }
    
    /// <summary>
    /// Clears all state
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Event raised when questions are updated
    /// </summary>
    event Action? OnQuestionsChanged;
    
    /// <summary>
    /// Saves questions to persistent storage
    /// </summary>
    Task SaveStateAsync();
    
    /// <summary>
    /// Loads questions from persistent storage
    /// </summary>
    Task LoadStateAsync();
}
