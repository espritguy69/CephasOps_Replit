using CephasOps.Application.Files.DTOs;

namespace CephasOps.Application.Files.Services;

/// <summary>
/// Service for syncing files to Microsoft OneDrive using Microsoft Graph API
/// </summary>
public interface IOneDriveSyncService
{
    /// <summary>
    /// Syncs a file to OneDrive after it's been uploaded to the local system
    /// </summary>
    /// <param name="fileId">The ID of the file to sync</param>
    /// <param name="filePath">Local file path</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="module">Module/category (e.g., "Orders", "RMA", "Inventory")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OneDrive file ID and web URL if successful</returns>
    Task<(string? FileId, string? WebUrl)> SyncFileAsync(
        Guid fileId,
        string filePath,
        string fileName,
        string? module = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a parser snapshot PDF to OneDrive
    /// </summary>
    /// <param name="sessionId">Parse session ID</param>
    /// <param name="orderId">Optional order ID if snapshot is linked to an order</param>
    /// <param name="snapshotFile">File DTO for the snapshot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OneDrive file ID and web URL if successful</returns>
    Task<(string? FileId, string? WebUrl)> SyncParserSnapshotAsync(
        Guid sessionId,
        Guid? orderId,
        FileDto snapshotFile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries syncing a file that previously failed
    /// </summary>
    /// <param name="fileId">The ID of the file to retry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OneDrive file ID and web URL if successful</returns>
    Task<(string? FileId, string? WebUrl)> RetrySyncAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if OneDrive sync is enabled and configured
    /// </summary>
    /// <returns>True if sync is enabled and configured</returns>
    Task<bool> IsSyncEnabledAsync(CancellationToken cancellationToken = default);
}

