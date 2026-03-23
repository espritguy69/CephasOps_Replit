using CephasOps.Domain.Common;

namespace CephasOps.Domain.Payroll.Entities;

/// <summary>
/// SI rate plan entity - defines payment rates for service installers
/// Rates are determined by: Department + Installer + Installation Method + Job Type
/// </summary>
public class SiRatePlan : CompanyScopedEntity
{
    /// <summary>
    /// Department ID - rates can vary by department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Service installer ID
    /// </summary>
    public Guid ServiceInstallerId { get; set; }

    /// <summary>
    /// Installation Method ID - Prelaid, Non-Prelaid, SDU, RDF Pole
    /// Links to InstallationMethod entity (Site Condition)
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Rate type: Junior, Senior, or Custom
    /// Junior/Senior use template rates, Custom allows per-installer override
    /// </summary>
    public string RateType { get; set; } = "Junior";

    /// <summary>
    /// Level (Junior, Senior, Lead, Subcon) - for categorization
    /// </summary>
    public string Level { get; set; } = string.Empty;

    // ============================================
    // Rate fields by Installation Method
    // ============================================

    /// <summary>
    /// Prelaid installation rate (easy - fibre already laid)
    /// </summary>
    public decimal? PrelaidRate { get; set; }

    /// <summary>
    /// Non-Prelaid installation rate (harder - must build infrastructure)
    /// </summary>
    public decimal? NonPrelaidRate { get; set; }

    /// <summary>
    /// SDU (Single Dwelling Unit / Landed) rate
    /// </summary>
    public decimal? SduRate { get; set; }

    /// <summary>
    /// RDF Pole installation rate
    /// </summary>
    public decimal? RdfPoleRate { get; set; }

    // ============================================
    // Rate fields by Job Type
    // ============================================

    /// <summary>
    /// Activation rate (new installation)
    /// </summary>
    public decimal? ActivationRate { get; set; }

    /// <summary>
    /// Modification rate (changes to existing)
    /// </summary>
    public decimal? ModificationRate { get; set; }

    /// <summary>
    /// Assurance rate (troubleshooting/repair)
    /// </summary>
    public decimal? AssuranceRate { get; set; }

    /// <summary>
    /// Assurance Repull rate (cable replacement)
    /// </summary>
    public decimal? AssuranceRepullRate { get; set; }

    // ============================================
    // Service Category specific rates (FTTR, FTTC)
    // ============================================

    /// <summary>
    /// FTTR (Fibre to the Room) rate
    /// </summary>
    public decimal? FttrRate { get; set; }

    /// <summary>
    /// FTTC (Fibre to the Curb) rate
    /// </summary>
    public decimal? FttcRate { get; set; }

    // ============================================
    // Bonus and Penalty
    // ============================================

    /// <summary>
    /// On-time completion bonus
    /// </summary>
    public decimal? OnTimeBonus { get; set; }

    /// <summary>
    /// Late completion penalty
    /// </summary>
    public decimal? LatePenalty { get; set; }

    /// <summary>
    /// Rework/redo rate (when job needs to be redone)
    /// </summary>
    public decimal? ReworkRate { get; set; }

    /// <summary>
    /// Whether this rate plan is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (null = still valid)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }
}

