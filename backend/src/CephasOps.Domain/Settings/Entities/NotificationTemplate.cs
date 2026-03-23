using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class NotificationTemplate : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Channels { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

