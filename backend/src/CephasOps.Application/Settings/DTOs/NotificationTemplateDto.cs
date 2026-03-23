namespace CephasOps.Application.Settings.DTOs;

public class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Channels { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

