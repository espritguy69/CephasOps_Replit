using CephasOps.Application.Billing.Subscription.Services;
using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Files.DTOs;
using FileEntity = CephasOps.Domain.Files.Entities.File;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Files.Services;

/// <summary>
/// Service for file upload, download, and management. Integrates storage usage tracking and quota enforcement when services are registered.
/// </summary>
public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileService> _logger;
    private readonly IOneDriveSyncService? _oneDriveSyncService;
    private readonly ITenantUsageService? _tenantUsageService;
    private readonly ISubscriptionEnforcementService? _subscriptionEnforcementService;

    public FileService(
        ApplicationDbContext context,
        IHostEnvironment environment,
        ILogger<FileService> logger,
        IOneDriveSyncService? oneDriveSyncService = null,
        ITenantUsageService? tenantUsageService = null,
        ISubscriptionEnforcementService? subscriptionEnforcementService = null)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _oneDriveSyncService = oneDriveSyncService;
        _tenantUsageService = tenantUsageService;
        _subscriptionEnforcementService = subscriptionEnforcementService;
    }

    public async Task<FileDto> UploadFileAsync(FileUploadDto uploadDto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            throw new ArgumentException("File is required and cannot be empty", nameof(uploadDto));
        }

        // SaaS hardening: enforce storage quota before writing file
        if (_subscriptionEnforcementService != null && _tenantUsageService != null)
        {
            var tenantId = await _context.Companies
                .AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => c.TenantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var currentStorage = await _tenantUsageService.GetCurrentStorageBytesAsync(companyId, cancellationToken);
                var afterUpload = currentStorage + uploadDto.File.Length;
                if (!await _subscriptionEnforcementService.IsWithinStorageLimitAsync(tenantId.Value, afterUpload, cancellationToken))
                    throw new InvalidOperationException("Storage quota exceeded. Cannot upload file.");
            }
        }

        var fileId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        // Generate storage path: files/{companyId}/{module}/{year}/{month}/{fileId}.{ext}
        var module = uploadDto.Module ?? "General";
        var extension = Path.GetExtension(uploadDto.File.FileName);
        var storagePath = Path.Combine("files", companyId.ToString(), module, year.ToString(), month.ToString(), $"{fileId}{extension}").Replace('\\', '/');

        // Ensure directory exists
        var basePath = _environment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "uploads", storagePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save file to disk
        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await uploadDto.File.CopyToAsync(fileStream, cancellationToken);
        }

        // Calculate checksum (optional, using simple hash for now)
        var checksum = await CalculateChecksumAsync(fullPath);

        // Create file entity
        var file = new FileEntity
        {
            Id = fileId,
            CompanyId = companyId,
            FileName = uploadDto.File.FileName,
            StoragePath = storagePath,
            ContentType = uploadDto.File.ContentType,
            SizeBytes = uploadDto.File.Length,
            Checksum = checksum,
            CreatedById = userId,
            CreatedAt = now,
            Module = uploadDto.Module,
            EntityId = uploadDto.EntityId,
            EntityType = uploadDto.EntityType
        };

        _context.Set<FileEntity>().Add(file);
        await _context.SaveChangesAsync(cancellationToken);

        if (_tenantUsageService != null)
            await _tenantUsageService.RecordStorageDeltaAsync(companyId, uploadDto.File.Length, cancellationToken);

        _logger.LogInformation("File uploaded: {FileId}, Company: {CompanyId}, Size: {SizeBytes}", fileId, companyId, uploadDto.File.Length);

        // Trigger OneDrive sync in background (fire and forget)
        if (_oneDriveSyncService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _oneDriveSyncService.SyncFileAsync(fileId, fullPath, uploadDto.File.FileName, uploadDto.Module, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing file {FileId} to OneDrive in background", fileId);
                }
            }, cancellationToken);
        }

        return new FileDto
        {
            Id = file.Id,
            CompanyId = file.CompanyId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Checksum = file.Checksum,
            CreatedById = file.CreatedById,
            CreatedAt = file.CreatedAt,
            Module = file.Module,
            EntityId = file.EntityId,
            EntityType = file.EntityType
        };
    }

    public async Task<(Stream FileStream, string FileName, string ContentType)> DownloadFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.CompanyId == companyId, cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        var basePath = _environment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "uploads", file.StoragePath);

        if (!System.IO.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found on disk: {fullPath}");
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return (fileStream, file.FileName, file.ContentType);
    }

    public async Task DeleteFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.CompanyId == companyId, cancellationToken);

        if (file == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        var sizeBytes = file.SizeBytes;

        // Delete physical file
        var basePath = _environment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "uploads", file.StoragePath);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }

        // Delete database record
        _context.Set<FileEntity>().Remove(file);
        await _context.SaveChangesAsync(cancellationToken);

        if (_tenantUsageService != null)
            await _tenantUsageService.RecordStorageDeltaAsync(companyId, -sizeBytes, cancellationToken);

        _logger.LogInformation("File deleted: {FileId}, Company: {CompanyId}", fileId, companyId);
    }

    public async Task<FileDto?> GetFileMetadataAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.CompanyId == companyId, cancellationToken);

        if (file == null)
        {
            return null;
        }

        return new FileDto
        {
            Id = file.Id,
            CompanyId = file.CompanyId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Checksum = file.Checksum,
            CreatedById = file.CreatedById,
            CreatedAt = file.CreatedAt,
            Module = file.Module,
            EntityId = file.EntityId,
            EntityType = file.EntityType
        };
    }

    public async Task<byte[]?> GetFileContentAsync(Guid fileId, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        // Tenant-safe: resolve effective company so we never return content across tenants (e.g. when called from job without scope).
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
        {
            _logger.LogWarning("GetFileContentAsync called for file {FileId} without company context; returning null.", fileId);
            return null;
        }

        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.CompanyId == effectiveCompanyId.Value, cancellationToken);

        if (file == null)
        {
            return null;
        }

        var basePath = _environment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, "uploads", file.StoragePath);

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("File not found on disk: {FileId}, Path: {Path}", fileId, fullPath);
            return null;
        }

        return await System.IO.File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<FileDto?> GetFileInfoAsync(Guid fileId, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
        {
            _logger.LogDebug("GetFileInfoAsync called for file {FileId} without company context; returning null.", fileId);
            return null;
        }

        var file = await _context.Set<FileEntity>()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.CompanyId == effectiveCompanyId.Value, cancellationToken);

        if (file == null)
        {
            return null;
        }

        return new FileDto
        {
            Id = file.Id,
            CompanyId = file.CompanyId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Checksum = file.Checksum,
            CreatedById = file.CreatedById,
            CreatedAt = file.CreatedAt,
            Module = file.Module,
            EntityId = file.EntityId,
            EntityType = file.EntityType
        };
    }

    public async Task<List<FileDto>> GetFilesAsync(Guid companyId, string? module = null, Guid? entityId = null, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FileEntity>()
            .Where(f => f.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(f => f.Module == module);
        }

        if (entityId.HasValue)
        {
            query = query.Where(f => f.EntityId == entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(f => f.EntityType == entityType);
        }

        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return files.Select(f => new FileDto
        {
            Id = f.Id,
            CompanyId = f.CompanyId,
            FileName = f.FileName,
            ContentType = f.ContentType,
            SizeBytes = f.SizeBytes,
            Checksum = f.Checksum,
            CreatedById = f.CreatedById,
            CreatedAt = f.CreatedAt,
            Module = f.Module,
            EntityId = f.EntityId,
            EntityType = f.EntityType
        }).ToList();
    }

    private async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToBase64String(hash);
    }
}

