using CephasOps.Domain.Common;

namespace CephasOps.Domain.Files.Entities;

/// <summary>
/// Represents a stored file in the system (photos, PDFs, dockets, etc.)
/// </summary>
public class File : CompanyScopedEntity
{
    /// <summary>
    /// Original file name as uploaded by the user
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path in the file system or object storage
    /// Format: files/{companyId}/{module}/{year}/{month}/{fileId}.{ext}
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file (e.g., image/jpeg, application/pdf)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Optional checksum/hash for integrity verification
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// ID of the user or service installer who uploaded the file
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Optional module/category this file belongs to (e.g., "Orders", "RMA", "SIApp")
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Optional reference to the entity this file is attached to (e.g., OrderId, RmaTicketId)
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Optional entity type (e.g., "Order", "RmaTicket", "Invoice")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// OneDrive file ID (if synced to OneDrive)
    /// </summary>
    public string? OneDriveFileId { get; set; }

    /// <summary>
    /// OneDrive web URL (direct link to file in OneDrive)
    /// </summary>
    public string? OneDriveWebUrl { get; set; }

    /// <summary>
    /// OneDrive sync status (NotSynced, Pending, Synced, Failed)
    /// </summary>
    public string OneDriveSyncStatus { get; set; } = "NotSynced";

    /// <summary>
    /// Timestamp when file was successfully synced to OneDrive
    /// </summary>
    public DateTime? OneDriveSyncedAt { get; set; }

    /// <summary>
    /// Error message if OneDrive sync failed
    /// </summary>
    public string? OneDriveSyncError { get; set; }

    /// <summary>SaaS storage lifecycle: last time the file was read or updated. Updated on access when tracking is enabled.</summary>
    public DateTime? LastAccessedAtUtc { get; set; }

    /// <summary>SaaS storage lifecycle: Hot | Warm | Cold | Archive. Used for tiering and retention.</summary>
    public string StorageTier { get; set; } = "Hot";
}

