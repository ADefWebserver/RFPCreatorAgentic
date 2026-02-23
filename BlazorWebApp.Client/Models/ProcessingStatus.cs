namespace BlazorWebApp.Client.Models;

/// <summary>
/// Represents the status of a processing operation
/// </summary>
public enum ProcessingStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
