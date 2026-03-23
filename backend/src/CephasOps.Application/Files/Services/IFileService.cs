using CephasOps.Application.Files.DTOs;

namespace CephasOps.Application.Files.Services;

/// <summary>
/// Service for file upload, download, and management
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Upload a file and store it
    /// </summary>
    Task<FileDto> UploadFileAsync(FileUploadDto uploadDto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file by ID
    /// </summary>
    Task<(Stream FileStream, string FileName, string ContentType)> DownloadFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file by ID (if allowed)
    /// </summary>
    Task DeleteFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata by ID
    /// </summary>
    Task<FileDto?> GetFileMetadataAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file content as byte array (for internal use, e.g., Carbone templates).
    /// When companyId is provided (or when in tenant scope), only returns content for that company's file.
    /// </summary>
    Task<byte[]?> GetFileContentAsync(Guid fileId, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata by file ID. When companyId is provided (or when in tenant scope), only returns info for that company's file.
    /// For internal use (e.g. Carbone template name); pass companyId when available for tenant safety.
    /// </summary>
    Task<FileDto?> GetFileInfoAsync(Guid fileId, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of files with optional filters
    /// </summary>
    Task<List<FileDto>> GetFilesAsync(Guid companyId, string? module = null, Guid? entityId = null, string? entityType = null, CancellationToken cancellationToken = default);
}

