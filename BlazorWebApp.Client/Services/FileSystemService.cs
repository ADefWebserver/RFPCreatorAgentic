using Microsoft.JSInterop;

namespace BlazorWebApp.Client.Services;

/// <summary>
/// File system service implementation using browser APIs
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly IJSRuntime _jsRuntime;
    
    public FileSystemService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task SaveFileAsync(byte[] bytes, string fileName)
    {
        var base64 = Convert.ToBase64String(bytes);
        await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, base64);
    }
    
    public async Task DownloadFileAsync(byte[] bytes, string fileName)
    {
        var base64 = Convert.ToBase64String(bytes);
        await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, base64);
    }
}
