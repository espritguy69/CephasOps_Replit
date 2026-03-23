using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Files.DTOs;

/// <summary>
/// DTO for file metadata
/// </summary>
public class FileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Module { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
}

/// <summary>
/// DTO for file upload request
/// </summary>
public class FileUploadDto
{
    public IFormFile File { get; set; } = null!;
    public string? Module { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
}

