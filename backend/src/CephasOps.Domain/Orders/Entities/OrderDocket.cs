using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order docket entity - links orders to docket files
/// </summary>
public class OrderDocket : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public Guid FileId { get; set; }
    public Guid? UploadedBySiId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string UploadSource { get; set; } = string.Empty; // SIApp, AdminPortal, Import
    public bool IsFinal { get; set; }
    public string? Notes { get; set; }
}

