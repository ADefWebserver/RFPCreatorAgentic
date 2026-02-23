namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents the progress of a processing operation
/// </summary>
public class ProcessingProgress
{
    public string CurrentStep { get; set; } = string.Empty;
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
    public double PercentComplete => TotalItems > 0 ? (CurrentItem / (double)TotalItems) * 100 : 0;
    public string Message { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
}
