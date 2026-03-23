using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// File upload, download, and management endpoints
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileService fileService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file (photo, PDF, docket, etc.)
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="module">Optional module/category (e.g., "Orders", "RMA", "SIApp")</param>
    /// <param name="entityId">Optional ID of the entity this file is attached to</param>
    /// <param name="entityType">Optional type of entity (e.g., "Order", "RmaTicket")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile(
        IFormFile file,
        [FromQuery] string? module = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return this.BadRequest<FileDto>("File is required");
        }

        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;

        if (companyId == null || userId == null)
        {
            return this.Unauthorized<FileDto>("Company and user context required");
        }

        try
        {
            var uploadDto = new FileUploadDto
            {
                File = file,
                Module = module,
                EntityId = entityId,
                EntityType = entityType
            };

            var result = await _fileService.UploadFileAsync(uploadDto, companyId.Value, userId.Value, cancellationToken);
            return this.Success(result, "File uploaded successfully.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Storage quota", StringComparison.OrdinalIgnoreCase))
        {
            return this.Forbidden<FileDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return this.InternalServerError<FileDto>($"Failed to upload file: {ex.Message}");
        }
    }

    /// <summary>
    /// Download a file by ID
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    [HttpGet("{id}")]
    [HttpGet("{id}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return Unauthorized("Company context required");
        }

        try
        {
            var (fileStream, fileName, contentType) = await _fileService.DownloadFileAsync(id, companyId.Value, cancellationToken);
            return File(fileStream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound($"File with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileId}", id);
            return StatusCode(500, $"Failed to download file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file metadata by ID
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    [HttpGet("{id}/metadata")]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<FileDto>>> GetFileMetadata(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<FileDto>("Company context required");
        }

        try
        {
            var file = await _fileService.GetFileMetadataAsync(id, companyId.Value, cancellationToken);
            if (file == null)
            {
                return this.NotFound<FileDto>($"File with ID {id} not found");
            }

            return this.Success(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata: {FileId}", id);
            return this.InternalServerError<FileDto>($"Failed to get file metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Get list of files with optional filters
    /// </summary>
    /// <param name="module">Optional module filter</param>
    /// <param name="entityId">Optional entity ID filter</param>
    /// <param name="entityType">Optional entity type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of file metadata</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<FileDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<FileDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<FileDto>>>> GetFiles(
        [FromQuery] string? module = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized<List<FileDto>>("Company context required");
        }

        try
        {
            var files = await _fileService.GetFilesAsync(companyId.Value, module, entityId, entityType, cancellationToken);
            return this.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files");
            return this.InternalServerError<List<FileDto>>($"Failed to get files: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a file by ID (if allowed)
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteFile(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return this.Unauthorized("Company context required");
        }

        try
        {
            await _fileService.DeleteFileAsync(id, companyId.Value, cancellationToken);
            return this.NoContent();
        }
        catch (FileNotFoundException)
        {
            return this.NotFound($"File with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileId}", id);
            return this.InternalServerError($"Failed to delete file: {ex.Message}");
        }
    }
}
