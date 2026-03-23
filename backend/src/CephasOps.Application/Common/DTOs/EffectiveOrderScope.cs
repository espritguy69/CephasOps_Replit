namespace CephasOps.Application.Common.DTOs;

/// <summary>
/// Resolved order scope context for workflow and other scope-based resolution.
/// Aligns with WORKFLOW_RESOLUTION_RULES.md: Partner → Department → Order Type → General.
/// </summary>
public class EffectiveOrderScope
{
    /// <summary>Partner ID from the order (for partner-scoped workflow/rates).</summary>
    public Guid? PartnerId { get; set; }

    /// <summary>Department ID from the order (for department-scoped workflow/rates).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Order type code for scope: parent's Code when subtype (e.g. MODIFICATION), else own Code (e.g. ACTIVATION).</summary>
    public string? OrderTypeCode { get; set; }
}
