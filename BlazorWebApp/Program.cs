using Blazored.LocalStorage;
using BlazorWebApp.Client.Services;
using BlazorWebApp.Components;
using BlazorWebApp.Models;
using BlazorWebApp.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add HttpClient for prerendering
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost/") });

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Radzen Components
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Add Application Services
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IKnowledgebaseService, KnowledgebaseService>();
builder.Services.AddScoped<IRfpProcessingService, RfpProcessingService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IRfpStateService, ServerRfpStateService>();

// Add Server-side Document Service
builder.Services.AddSingleton<ServerDocumentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebApp.Client._Imports).Assembly);

// API endpoint for document generation (runs on server with full .NET capabilities)
app.MapPost("/api/generate-document", (GenerateDocumentRequest request, ServerDocumentService documentService) =>
{
    try
    {
        var documentBytes = documentService.GenerateRfpResponseDocument(request.Questions, request.Summary);
        return Results.File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
            $"RFP_Response_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to generate document: {ex.Message}");
    }
});

app.Run();
