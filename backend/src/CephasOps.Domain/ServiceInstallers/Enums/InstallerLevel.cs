namespace CephasOps.Domain.ServiceInstallers.Enums;

/// <summary>
/// Service Installer Level - Senior or Junior
/// Note: "Subcon" is not a level - it's a Type (Subcontractor)
/// </summary>
public enum InstallerLevel
{
    /// <summary>
    /// Junior installer - Entry to intermediate level (0-2 years experience)
    /// Standard installations under guidance
    /// </summary>
    Junior = 0,
    
    /// <summary>
    /// Senior installer - 2+ years experience
    /// Can handle complex installations independently
    /// Authorized for VIP customers, troubleshooting, MDU projects
    /// </summary>
    Senior = 1
}

