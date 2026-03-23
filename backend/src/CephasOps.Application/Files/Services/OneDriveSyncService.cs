using CephasOps.Application.Files.DTOs;
using CephasOps.Domain.Settings;
using CephasOps.Infrastructure.Persistence;
using FileEntity = CephasOps.Domain.Files.Entities.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Files.Services;

/// <summary>
/// Service for syncing files to Microsoft OneDrive using Microsoft Graph API
/// </summary>
public class OneDriveSyncService : IOneDriveSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly IGlobalSettingsReader _settingsReader;
    private readonly ILogger<OneDriveSyncService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OneDriveSyncService(
        ApplicationDbContext context,
        IGlobalSettingsReader settingsReader,
        ILogger<OneDriveSyncService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _settingsReader = settingsReader;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(string? FileId, string? WebUrl)> SyncFileAsync(
        Guid fileId,
        string filePath,
        string fileName,
        string? module = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if sync is enabled
            var isEnabled = await IsSyncEnabledAsync(cancellationToken);
            if (!isEnabled)
            {
                _logger.LogInformation("OneDrive sync is disabled, skipping sync for file {FileId}", fileId);
                return (null, null);
            }

            // Get OneDrive configuration
            var baseFolderPath = await _settingsReader.GetValueAsync<string>("OneDrive_BaseFolderPath", cancellationToken) ?? "CephasOps";
            var tenantId = await _settingsReader.GetValueAsync<string>("OneDrive_TenantId", cancellationToken);
            var clientId = await _settingsReader.GetValueAsync<string>("OneDrive_ClientId", cancellationToken);
            var clientSecret = await _settingsReader.GetValueAsync<string>("OneDrive_ClientSecret", cancellationToken);

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("OneDrive configuration is incomplete, cannot sync file {FileId}", fileId);
                await UpdateFileSyncStatusAsync(fileId, "Failed", errorMessage: "OneDrive configuration is incomplete", cancellationToken: cancellationToken);
                return (null, null);
            }

            // Get access token
            var accessToken = await GetAccessTokenAsync(tenantId, clientId, clientSecret, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to obtain OneDrive access token for file {FileId}", fileId);
                await UpdateFileSyncStatusAsync(fileId, "Failed", errorMessage: "Failed to obtain access token", cancellationToken: cancellationToken);
                return (null, null);
            }

            // Determine folder path based on module
            var folderPath = GetFolderPath(baseFolderPath, module);
            
            // Ensure folder exists
            var folderId = await EnsureFolderExistsAsync(accessToken, folderPath, cancellationToken);
            if (string.IsNullOrEmpty(folderId))
            {
                _logger.LogError("Failed to create/access OneDrive folder {FolderPath} for file {FileId}", folderPath, fileId);
                await UpdateFileSyncStatusAsync(fileId, "Failed", errorMessage: $"Failed to create/access folder: {folderPath}", cancellationToken: cancellationToken);
                return (null, null);
            }

            // Upload file to OneDrive
            var (oneDriveFileId, webUrl) = await UploadFileToOneDriveAsync(
                accessToken,
                folderId,
                filePath,
                fileName,
                cancellationToken);

            if (string.IsNullOrEmpty(oneDriveFileId))
            {
                _logger.LogError("Failed to upload file {FileId} to OneDrive", fileId);
                await UpdateFileSyncStatusAsync(fileId, "Failed", errorMessage: "Failed to upload file to OneDrive", cancellationToken: cancellationToken);
                return (null, null);
            }

            // Update file entity with OneDrive info
            await UpdateFileSyncStatusAsync(fileId, "Synced", errorMessage: null, oneDriveFileId: oneDriveFileId, oneDriveWebUrl: webUrl, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully synced file {FileId} to OneDrive: {FileId}", fileId, oneDriveFileId);
            return (oneDriveFileId, webUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing file {FileId} to OneDrive", fileId);
            await UpdateFileSyncStatusAsync(fileId, "Failed", errorMessage: ex.Message, cancellationToken: cancellationToken);
            return (null, null);
        }
    }

    public async Task<(string? FileId, string? WebUrl)> SyncParserSnapshotAsync(
        Guid sessionId,
        Guid? orderId,
        FileDto snapshotFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if parser snapshot sync is enabled
            var syncParserSnapshots = await _settingsReader.GetValueAsync<bool>("OneDrive_SyncParserSnapshots", cancellationToken);
            if (!syncParserSnapshots)
            {
                _logger.LogInformation("Parser snapshot sync is disabled, skipping sync for session {SessionId}", sessionId);
                return (null, null);
            }

            // Get file path
            var file = await _context.Set<FileEntity>()
                .FirstOrDefaultAsync(f => f.Id == snapshotFile.Id, cancellationToken);

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for parser snapshot sync", snapshotFile.Id);
                return (null, null);
            }

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(basePath, "uploads", file.StoragePath);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("File not found on disk: {FilePath}", fullPath);
                return (null, null);
            }

            // Determine file name
            var fileName = orderId.HasValue
                ? $"Order-{orderId}-Snapshot.pdf"
                : $"ParseSession-{sessionId}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            // Get parser snapshots path
            var parserSnapshotsPath = await _settingsReader.GetValueAsync<string>("OneDrive_ParserSnapshotsPath", cancellationToken) ?? "Parser/Snapshots";
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var module = $"{parserSnapshotsPath}/{year}/{month:D2}";

            // Sync using the regular sync method with custom module path
            return await SyncFileAsync(snapshotFile.Id, fullPath, fileName, module, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing parser snapshot for session {SessionId}", sessionId);
            return (null, null);
        }
    }

    public async Task<(string? FileId, string? WebUrl)> RetrySyncAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "uploads", file.StoragePath);

        if (!System.IO.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found on disk: {fullPath}");
        }

        return await SyncFileAsync(fileId, fullPath, file.FileName, file.Module, cancellationToken);
    }

    public async Task<bool> IsSyncEnabledAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await _settingsReader.GetValueAsync<bool>("OneDrive_Enabled", cancellationToken);
        var tenantId = await _settingsReader.GetValueAsync<string>("OneDrive_TenantId", cancellationToken);
        var clientId = await _settingsReader.GetValueAsync<string>("OneDrive_ClientId", cancellationToken);
        var clientSecret = await _settingsReader.GetValueAsync<string>("OneDrive_ClientSecret", cancellationToken);

        return enabled && !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
    }

    private async Task<string?> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await httpClient.PostAsync(tokenEndpoint, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining OneDrive access token");
            return null;
        }
    }

    private string GetFolderPath(string baseFolderPath, string? module)
    {
        if (string.IsNullOrEmpty(module))
        {
            return baseFolderPath;
        }

        return $"{baseFolderPath}/{module}";
    }

    private async Task<string?> EnsureFolderExistsAsync(string accessToken, string folderPath, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentFolderId = "root"; // Start from root

            foreach (var part in pathParts)
            {
                // Check if folder exists
                var checkUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{currentFolderId}/children?$filter=name eq '{Uri.EscapeDataString(part)}' and folder ne null";
                var checkResponse = await httpClient.GetAsync(checkUrl, cancellationToken);

                if (checkResponse.IsSuccessStatusCode)
                {
                    var checkJson = await checkResponse.Content.ReadAsStringAsync(cancellationToken);
                    var checkResult = System.Text.Json.JsonSerializer.Deserialize<GraphResponse<DriveItem>>(checkJson);

                    if (checkResult?.Value != null && checkResult.Value.Any())
                    {
                        currentFolderId = checkResult.Value.First().Id;
                        continue;
                    }
                }

                // Create folder if it doesn't exist
                var createUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{currentFolderId}/children";
                var createBody = new Dictionary<string, object>
                {
                    ["name"] = part,
                    ["folder"] = new Dictionary<string, object>(),
                    ["@microsoft.graph.conflictBehavior"] = "rename"
                };

                var createContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(createBody),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var createResponse = await httpClient.PostAsync(createUrl, createContent, cancellationToken);
                if (!createResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create folder {FolderName} in OneDrive. Status: {Status}, Response: {Response}",
                        part, createResponse.StatusCode, await createResponse.Content.ReadAsStringAsync(cancellationToken));
                    return null;
                }

                var createJson = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                var createResult = System.Text.Json.JsonSerializer.Deserialize<DriveItem>(createJson);
                currentFolderId = createResult?.Id ?? currentFolderId;
            }

            return currentFolderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring OneDrive folder exists: {FolderPath}", folderPath);
            return null;
        }
    }

    private async Task<(string? FileId, string? WebUrl)> UploadFileToOneDriveAsync(
        string accessToken,
        string folderId,
        string filePath,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Read file content
            var fileContent = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);

            // Upload file
            var uploadUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{folderId}:/{Uri.EscapeDataString(fileName)}:/content";
            var uploadContent = new ByteArrayContent(fileContent);
            uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var uploadResponse = await httpClient.PutAsync(uploadUrl, uploadContent, cancellationToken);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to upload file to OneDrive. Status: {Status}, Response: {Response}",
                    uploadResponse.StatusCode, errorContent);
                return (null, null);
            }

            var uploadJson = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            var uploadResult = System.Text.Json.JsonSerializer.Deserialize<DriveItem>(uploadJson);

            return (uploadResult?.Id, uploadResult?.WebUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to OneDrive: {FilePath}", filePath);
            return (null, null);
        }
    }

    private async Task UpdateFileSyncStatusAsync(
        Guid fileId,
        string status,
        string? errorMessage = null,
        string? oneDriveFileId = null,
        string? oneDriveWebUrl = null,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (file != null)
        {
            file.OneDriveSyncStatus = status;
            file.OneDriveSyncError = errorMessage;
            file.OneDriveSyncedAt = status == "Synced" ? DateTime.UtcNow : file.OneDriveSyncedAt;
            file.OneDriveFileId = oneDriveFileId ?? file.OneDriveFileId;
            file.OneDriveWebUrl = oneDriveWebUrl ?? file.OneDriveWebUrl;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Helper classes for JSON deserialization
    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class GraphResponse<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public List<T>? Value { get; set; }
    }

    private class DriveItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }
}

