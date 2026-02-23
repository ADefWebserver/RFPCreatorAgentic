namespace BlazorWebApp.Client.Services;

/// <summary>
/// Interface for file system operations
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Save file using the File System Access API
    /// </summary>
    Task SaveFileAsync(byte[] bytes, string fileName);
    
    /// <summary>
    /// Download a file to the user's device
    /// </summary>
    Task DownloadFileAsync(byte[] bytes, string fileName);
}
