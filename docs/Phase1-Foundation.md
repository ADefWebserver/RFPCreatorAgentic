# Phase 1: Foundation

## Overview

Set up the foundational project structure and core infrastructure for the RFP Responder application.

---

## Checklist

- [ ] Create Blazor WebAssembly + Server project structure
- [ ] Configure Radzen Blazor components and theming
- [ ] Set up Blazored.LocalStorage
- [ ] Implement Virtual File System service

---

## Project Structure

```
RFPCreatorAgentic/
├── BlazorWebApp/                          # Server-side host project
│   ├── Program.cs
│   ├── App.razor
│   ├── _Imports.razor
│   └── Components/
│       └── Layout/
│           └── MainLayout.razor
│
├── BlazorWebApp.Client/                   # Client WASM project
│   ├── Program.cs
│   ├── _Imports.razor
│   ├── wwwroot/
│   │   └── css/
│   ├── Components/
│   │   └── Pages/
│   │       ├── Home.razor
│   │       ├── Home.razor.cs
│   │       └── Dialogs/
│   │           └── ConfigureAIDialog.razor
│   ├── Services/
│   │   ├── IKnowledgebaseService.cs
│   │   ├── KnowledgebaseService.cs
│   │   ├── IRfpProcessingService.cs
│   │   ├── RfpProcessingService.cs
│   │   ├── IDocumentService.cs
│   │   ├── DocumentService.cs
│   │   ├── IAIService.cs
│   │   ├── AIService.cs
│   │   ├── IFileSystemService.cs
│   │   └── FileSystemService.cs
│   └── Models/
│       ├── KnowledgebaseEntry.cs
│       ├── KnowledgebaseChunk.cs
│       ├── RfpQuestion.cs
│       ├── RfpResponse.cs
│       └── AIProviderSettings.cs
│
└── docs/
```

---

## Technology Stack

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Blazored.LocalStorage` | Latest | Persistent browser storage for knowledgebase and settings |
| `iText7` | Latest | PDF parsing and text extraction |
| `DocX` (Xceed) | 4.0.25105.5786 | Word document generation and export |
| `Microsoft.Extensions.AI.OpenAI` | Latest | LLM embedding and completion calls |
| `Radzen.Blazor` | Latest | UI components, theming, and DataGrid |

### Framework Requirements

- **.NET 10** (Blazor 10)
- **Blazor WebAssembly + Server (Hybrid)** with `InteractiveWebAssembly` render mode
- **Blazor WebAssembly Virtual File System Access API** for local file operations

---

## Implementation Details

### 1. Create Blazor WebAssembly + Server Project Structure

Create new Blazor Web App with WebAssembly rendering:

```bash
dotnet new blazor -n BlazorWebApp -int WebAssembly -o RFPCreatorAgentic
```

### 2. Configure Radzen Blazor Components

Install Radzen package:

```bash
dotnet add package Radzen.Blazor
```

Register in `Program.cs`:

```csharp
builder.Services.AddRadzenComponents();
```

Add to `_Imports.razor`:

```razor
@using Radzen
@using Radzen.Blazor
```

Add CSS/JS references in `App.razor` or index.html:

```html
<link rel="stylesheet" href="_content/Radzen.Blazor/css/default-base.css">
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

### 3. Set Up Blazored.LocalStorage

Install package:

```bash
dotnet add package Blazored.LocalStorage
```

Register in `Program.cs`:

```csharp
builder.Services.AddBlazoredLocalStorage();
```

### 4. Implement Virtual File System Service

Create `IFileSystemService` interface:

```csharp
public interface IFileSystemService
{
    Task SaveFileAsync(byte[] bytes, string fileName);
    Task<byte[]> ReadFileAsync(string fileName);
    Task DeleteFileAsync(string fileName);
    Task DownloadFileAsync(byte[] bytes, string fileName);
}
```

Implement `FileSystemService`:

```csharp
public class FileSystemService : IFileSystemService
{
    private readonly IJSRuntime _jsRuntime;
    
    public FileSystemService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task SaveFileAsync(byte[] bytes, string fileName)
    {
        // Use Blazor WebAssembly File System Access API
        var fileHandle = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "showSaveFilePicker", 
            new { 
                suggestedName = fileName, 
                types = new[] { 
                    new { 
                        accept = new { 
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" = new[] { ".docx" } 
                        } 
                    } 
                } 
            });
        
        var writable = await fileHandle.InvokeAsync<IJSObjectReference>("createWritable");
        await writable.InvokeVoidAsync("write", bytes);
        await writable.InvokeVoidAsync("close");
    }
    
    public async Task<byte[]> ReadFileAsync(string fileName)
    {
        // Implementation for reading files
        throw new NotImplementedException();
    }
    
    public async Task DeleteFileAsync(string fileName)
    {
        // Implementation for deleting files
        throw new NotImplementedException();
    }
    
    public async Task DownloadFileAsync(byte[] bytes, string fileName)
    {
        // Trigger browser download
        await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
    }
}
```

---

## Dependency Injection Registration

Add all foundation services to `Program.cs`:

```csharp
// Program.cs (Client)
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRadzenComponents();

builder.Services.AddScoped<IFileSystemService, FileSystemService>();
```

---

## Reference Links

- [Blazor WebAssembly Virtual File System](https://blazorhelpwebsite.com/ViewBlogPost/17069)
- [Radzen Blazor Documentation](https://razor.radzen.com)
- [Radzen Setup Guide](https://razor.radzen.com/get-started?theme=default)
