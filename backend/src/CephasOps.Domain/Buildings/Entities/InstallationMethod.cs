using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// InstallationMethod entity - represents site conditions/installation methods
/// (Prelaid, Non-prelaid MDU, SDU, RDF Pole, etc.)
/// Determines how installations are performed and affects installer payment rates.
/// </summary>
public class InstallationMethod : CompanyScopedEntity
{
    /// <summary>
    /// Department ID - installation methods can be department-specific
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Method name (e.g., "Prelaid", "Non-prelaid (MDU)", "SDU", "RDF Pole")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code (e.g., "PRELAID", "NON_PRELAID", "SDU_RDF")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category of installation (FTTH, FTTO, FTTR, FTTC, etc.)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Detailed description of the installation method
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this installation method is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for dropdowns
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Timestamp when created
    /// </summary>
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when last updated
    /// </summary>
    public new DateTime? UpdatedAt { get; set; }
}

