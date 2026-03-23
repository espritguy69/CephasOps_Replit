using CephasOps.Domain.Common;

namespace CephasOps.Domain.ServiceInstallers.Entities;

/// <summary>
/// Backup / emergency contact for a Service Installer.
/// </summary>
public class ServiceInstallerContact : CompanyScopedEntity
{
    public Guid ServiceInstallerId { get; set; }
    public ServiceInstaller ServiceInstaller { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Contact type, e.g. "Backup" or "Emergency".
    /// </summary>
    public string ContactType { get; set; } = "Backup";

    /// <summary>
    /// Whether this is the primary contact of this type.
    /// </summary>
    public bool IsPrimary { get; set; }
}


