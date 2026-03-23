using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Carbone document rendering implementation
/// Calls Carbone HTTP API to render templates to PDF
/// </summary>
public class CarboneRenderer : ICarboneRenderer
{
    private readonly HttpClient _httpClient;
    private readonly CarboneSettings _settings;
    private readonly IFileService _fileService;
    private readonly ILogger<CarboneRenderer> _logger;

    public CarboneRenderer(
        HttpClient httpClient,
        IOptions<CarboneSettings> settings,
        IFileService fileService,
        ILogger<CarboneRenderer> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _fileService = fileService;
        _logger = logger;

        ConfigureHttpClient();
    }

    public bool IsConfigured => _settings.Enabled && _settings.IsValid();

    private void ConfigureHttpClient()
    {
        if (!_settings.Enabled)
            return;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        _httpClient.DefaultRequestHeaders.Add("carbone-version", _settings.ApiVersion);
    }

    public async Task<byte[]> RenderFromHtmlAsync(
        string htmlTemplate, 
        object data, 
        string documentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogInformation(
            "Carbone: Rendering HTML template for {DocumentType}, template length: {Length} chars",
            documentType, htmlTemplate.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Upload HTML template to get templateId
            var templateId = await UploadTemplateAsync(
                Encoding.UTF8.GetBytes(htmlTemplate), 
                "template.html",
                cancellationToken);

            _logger.LogDebug("Carbone: Template uploaded, templateId: {TemplateId}", templateId);

            // Step 2: Render with data
            var pdfBytes = await RenderTemplateAsync(templateId, data, documentType, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Carbone: Rendered {DocumentType} successfully in {ElapsedMs}ms, PDF size: {Size} bytes",
                documentType, stopwatch.ElapsedMilliseconds, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex) when (ex is not CarboneNotConfiguredException && ex is not CarboneRenderException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "Carbone: Failed to render {DocumentType} after {ElapsedMs}ms",
                documentType, stopwatch.ElapsedMilliseconds);
            
            throw new CarboneRenderException(
                $"Failed to render document using Carbone: {ex.Message}", 
                ex, 
                documentType);
        }
    }

    public async Task<byte[]> RenderFromFileAsync(
        Guid templateFileId, 
        object data, 
        string documentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogInformation(
            "Carbone: Rendering from file template {TemplateFileId} for {DocumentType}",
            templateFileId, documentType);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Get template file content from FileService
            var fileBytes = await _fileService.GetFileContentAsync(templateFileId, null, cancellationToken);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new CarboneRenderException(
                    $"Template file {templateFileId} not found or empty",
                    documentType);
            }

            // Determine file extension (assume DOCX if unknown)
            var fileInfo = await _fileService.GetFileInfoAsync(templateFileId, null, cancellationToken);
            var fileName = fileInfo?.FileName ?? "template.docx";

            _logger.LogDebug("Carbone: Template file loaded, size: {Size} bytes, name: {FileName}", 
                fileBytes.Length, fileName);

            // Step 2: Upload template to Carbone
            var templateId = await UploadTemplateAsync(fileBytes, fileName, cancellationToken);

            _logger.LogDebug("Carbone: Template uploaded, templateId: {TemplateId}", templateId);

            // Step 3: Render with data
            var pdfBytes = await RenderTemplateAsync(templateId, data, documentType, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Carbone: Rendered {DocumentType} from file successfully in {ElapsedMs}ms, PDF size: {Size} bytes",
                documentType, stopwatch.ElapsedMilliseconds, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex) when (ex is not CarboneNotConfiguredException && ex is not CarboneRenderException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "Carbone: Failed to render {DocumentType} from file {TemplateFileId} after {ElapsedMs}ms",
                documentType, templateFileId, stopwatch.ElapsedMilliseconds);
            
            throw new CarboneRenderException(
                $"Failed to render document from file using Carbone: {ex.Message}", 
                ex, 
                documentType);
        }
    }

    private void EnsureConfigured()
    {
        if (!_settings.Enabled)
        {
            throw new CarboneNotConfiguredException(
                "Carbone engine is disabled. Set 'Carbone:Enabled' to true in appsettings.json.");
        }

        var validationError = _settings.GetValidationError();
        if (validationError != null)
        {
            throw new CarboneNotConfiguredException(validationError);
        }
    }

    /// <summary>
    /// Upload template to Carbone and get templateId
    /// </summary>
    private async Task<string> UploadTemplateAsync(
        byte[] templateBytes, 
        string fileName,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(templateBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(fileContent, "template", fileName);

        var response = await _httpClient.PostAsync("template", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Carbone: Template upload failed with status {StatusCode}: {Error}",
                (int)response.StatusCode, errorBody);
            
            throw new CarboneRenderException(
                $"Failed to upload template to Carbone: {response.StatusCode} - {errorBody}",
                statusCode: (int)response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        
        if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
            !dataElement.TryGetProperty("templateId", out var templateIdElement))
        {
            throw new CarboneRenderException(
                $"Unexpected response from Carbone template upload: {responseJson}");
        }

        return templateIdElement.GetString() ?? throw new CarboneRenderException("Template ID is null");
    }

    /// <summary>
    /// Render template with data and get PDF
    /// </summary>
    private async Task<byte[]> RenderTemplateAsync(
        string templateId,
        object data,
        string documentType,
        CancellationToken cancellationToken)
    {
        // Wrap data in { "d": { ... } } format for Carbone
        var wrappedData = new { d = data };
        var jsonPayload = JsonSerializer.Serialize(wrappedData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _logger.LogDebug("Carbone: Rendering template {TemplateId} with payload size: {Size} bytes",
            templateId, jsonPayload.Length);

        var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        // Carbone render endpoint: POST /render/{templateId}
        var response = await _httpClient.PostAsync(
            $"render/{templateId}", 
            requestContent, 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Carbone: Render failed for {DocumentType} with status {StatusCode}: {Error}",
                documentType, (int)response.StatusCode, errorBody);
            
            throw new CarboneRenderException(
                $"Failed to render document using Carbone: {response.StatusCode} - {errorBody}",
                documentType,
                (int)response.StatusCode);
        }

        // Response contains the rendered PDF
        var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        if (pdfBytes.Length == 0)
        {
            throw new CarboneRenderException(
                "Carbone returned empty PDF content",
                documentType);
        }

        return pdfBytes;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            _ => "application/octet-stream"
        };
    }
}

