namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Context used for resolution (echo of request + effective date). Read-only debug.
/// </summary>
public class ResolutionContextDto
{
    public Guid? CompanyId { get; set; }
    public DateTime? EffectiveDateUsed { get; set; }
    public Guid? OrderTypeId { get; set; }
    public Guid? OrderSubtypeId { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? SiTier { get; set; }
    public Guid? PartnerGroupId { get; set; }
}

/// <summary>
/// IDs of matched rate(s) for debugging. Only the relevant ID is set per path.
/// </summary>
public class MatchedRuleDetailsDto
{
    public Guid? RateGroupId { get; set; }
    public Guid? BaseWorkRateId { get; set; }
    public Guid? LegacyRateId { get; set; }
    public Guid? CustomRateId { get; set; }
    public Guid? ServiceProfileId { get; set; }
}

/// <summary>
/// One modifier application for trace. Read-only debug.
/// </summary>
public class ModifierTraceItemDto
{
    public string ModifierType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // Add | Multiply
    public decimal Value { get; set; }
    public decimal AmountBefore { get; set; }
    public decimal AmountAfter { get; set; }
}

/// <summary>
/// Result of GPON rate resolution.
/// Contains both revenue (what Cephas earns) and payout (what SI earns) rates.
/// </summary>
public class GponRateResolutionResult
{
    /// <summary>
    /// Whether rate resolution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Revenue rate (what Cephas earns from partner)
    /// </summary>
    public decimal? RevenueAmount { get; set; }

    /// <summary>
    /// Source of revenue rate (GponPartnerJobRate, RateCard, etc.)
    /// </summary>
    public string? RevenueSource { get; set; }

    /// <summary>
    /// Revenue rate ID for audit trail
    /// </summary>
    public Guid? RevenueRateId { get; set; }

    /// <summary>
    /// Payout rate (what SI earns)
    /// </summary>
    public decimal? PayoutAmount { get; set; }

    /// <summary>
    /// Source of payout rate (GponSiCustomRate, GponSiJobRate, RateCard, etc.)
    /// </summary>
    public string? PayoutSource { get; set; }

    /// <summary>
    /// Payout rate ID for audit trail
    /// </summary>
    public Guid? PayoutRateId { get; set; }

    /// <summary>
    /// Gross margin (Revenue - Payout)
    /// </summary>
    public decimal? GrossMargin => RevenueAmount.HasValue && PayoutAmount.HasValue
        ? RevenueAmount.Value - PayoutAmount.Value
        : null;

    /// <summary>
    /// Margin percentage ((Revenue - Payout) / Revenue * 100)
    /// </summary>
    public decimal? MarginPercentage => RevenueAmount.HasValue && RevenueAmount.Value != 0 && PayoutAmount.HasValue
        ? (RevenueAmount.Value - PayoutAmount.Value) / RevenueAmount.Value * 100
        : null;

    /// <summary>
    /// Currency (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Resolution timestamp
    /// </summary>
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Resolution steps taken (for debugging/audit)
    /// </summary>
    public List<string> ResolutionSteps { get; set; } = new();

    /// <summary>
    /// Payout resolution path for debugging: CustomOverride, BaseWorkRate, or Legacy.
    /// Read-only; does not affect calculation.
    /// </summary>
    public string? PayoutPath { get; set; }

    /// <summary>
    /// Base payout amount before rate modifiers were applied (when path is BaseWorkRate or Legacy).
    /// Null when path is CustomOverride or when no payout was resolved.
    /// </summary>
    public decimal? BaseAmountBeforeModifiers { get; set; }

    /// <summary>
    /// Warnings for support/debugging (e.g. "Used legacy fallback"). Does not affect amounts.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Match level for payout: Custom | ExactCategory | ServiceProfile | BroadRateGroup | Legacy. Read-only debug.
    /// </summary>
    public string? ResolutionMatchLevel { get; set; }

    /// <summary>
    /// Context used for resolution (request echo + effective date). Read-only debug.
    /// </summary>
    public ResolutionContextDto? ResolutionContext { get; set; }

    /// <summary>
    /// IDs of matched rate(s). Read-only debug.
    /// </summary>
    public MatchedRuleDetailsDto? MatchedRuleDetails { get; set; }

    /// <summary>
    /// Modifiers applied in order. Read-only debug.
    /// </summary>
    public List<ModifierTraceItemDto> ModifierTrace { get; set; } = new();

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static GponRateResolutionResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static GponRateResolutionResult Succeeded(
        decimal? revenueAmount,
        string? revenueSource,
        Guid? revenueRateId,
        decimal? payoutAmount,
        string? payoutSource,
        Guid? payoutRateId) => new()
    {
        Success = true,
        RevenueAmount = revenueAmount,
        RevenueSource = revenueSource,
        RevenueRateId = revenueRateId,
        PayoutAmount = payoutAmount,
        PayoutSource = payoutSource,
        PayoutRateId = payoutRateId
    };
}

/// <summary>
/// Result of universal rate resolution
/// </summary>
public class UniversalRateResolutionResult
{
    /// <summary>
    /// Whether rate resolution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Resolved rate amount
    /// </summary>
    public decimal? RateAmount { get; set; }

    /// <summary>
    /// Source of rate (RateCard, CustomRate, etc.)
    /// </summary>
    public string? RateSource { get; set; }

    /// <summary>
    /// Rate ID for audit trail
    /// </summary>
    public Guid? RateId { get; set; }

    /// <summary>
    /// Rate card ID (if from RateCard)
    /// </summary>
    public Guid? RateCardId { get; set; }

    /// <summary>
    /// Rate card line ID (if from RateCardLine)
    /// </summary>
    public Guid? RateCardLineId { get; set; }

    /// <summary>
    /// Unit of measure (JOB, METER, SERVICE, etc.)
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Currency (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Resolution timestamp
    /// </summary>
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Resolution steps taken (for debugging/audit)
    /// </summary>
    public List<string> ResolutionSteps { get; set; } = new();
}

