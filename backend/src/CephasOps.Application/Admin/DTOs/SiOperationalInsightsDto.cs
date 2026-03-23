namespace CephasOps.Application.Admin.DTOs;

/// <summary>
/// Lightweight operational intelligence for Service Installer (SI) field operations.
/// Read-only aggregation from OrderStatusLog, Order, OrderMaterialReplacement, etc.
/// Used for visibility and operator decisions; not enforcement.
/// </summary>
public class SiOperationalInsightsDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    /// <summary>Time window (e.g. last 90 days) used for aggregations.</summary>
    public int WindowDays { get; set; }
    public string? DataQualityNote { get; set; }

    public SiCompletionPerformanceDto CompletionPerformance { get; set; } = new();
    public SiRescheduleBlockerPatternsDto RescheduleBlockerPatterns { get; set; } = new();
    public SiMaterialReplacementPatternsDto MaterialReplacementPatterns { get; set; } = new();
    public SiAssuranceReworkDto AssuranceRework { get; set; } = new();
    public SiOperationalHotspotsDto OperationalHotspots { get; set; } = new();
    /// <summary>Building Reliability Score: band and contributing factors per building (same window). For prioritization only.</summary>
    public SiBuildingReliabilityDto BuildingReliability { get; set; } = new();
    /// <summary>Order failure pattern detection: recurring operational/technical patterns. For visibility and prioritization only.</summary>
    public SiOrderFailurePatternsDto OrderFailurePatterns { get; set; } = new();
    /// <summary>Pattern clusters: buildings where multiple operational signals align. For operational review only.</summary>
    public SiPatternClustersDto PatternClusters { get; set; } = new();
    public List<string> DataGaps { get; set; } = new();
}

/// <summary>
/// Pattern cluster: multiple signals aligned at same building. Read-only; does not imply certainty.
/// </summary>
public class SiPatternClustersDto
{
    public List<SiPatternClusterItemDto> Clusters { get; set; } = new();
    public string? InterpretationNote { get; set; }
}

/// <summary>
/// A single building-level cluster: signals present, sample orders, interpretation, classification.
/// </summary>
public class SiPatternClusterItemDto
{
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    /// <summary>Human-readable list of signals that aligned (e.g. "High building reliability risk", "High-churn orders present").</summary>
    public List<string> SignalsPresent { get; set; } = new();
    /// <summary>Sample order IDs at this building (e.g. high-churn or replacement-heavy), up to 5.</summary>
    public List<Guid> SampleOrderIds { get; set; } = new();
    /// <summary>Short interpretation for operators.</summary>
    public string? Interpretation { get; set; }
    /// <summary>OperationalCluster | PossibleInfrastructureIssue</summary>
    public string Classification { get; set; } = "OperationalCluster";
    /// <summary>Optional limitations note.</summary>
    public string? Limitations { get; set; }
}

/// <summary>
/// Order failure pattern detection result. Each item is one detected pattern with count, samples, and strength.
/// For operational review only; not for automated enforcement.
/// </summary>
public class SiOrderFailurePatternsDto
{
    public List<SiOrderFailurePatternItemDto> Patterns { get; set; } = new();
    /// <summary>How patterns are derived and limitations.</summary>
    public string? InterpretationNote { get; set; }
}

/// <summary>
/// A single detected pattern: name, count, optional sample IDs, explanation, strength, limitations.
/// </summary>
public class SiOrderFailurePatternItemDto
{
    /// <summary>Stable identifier for the pattern (e.g. BlockerAndRescheduleOnSameOrder).</summary>
    public string PatternId { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public int Count { get; set; }
    /// <summary>Sample order IDs (up to 5) for drill-down.</summary>
    public List<Guid> SampleOrderIds { get; set; } = new();
    /// <summary>Sample building IDs (up to 5) when pattern is building-scoped.</summary>
    public List<Guid> SampleBuildingIds { get; set; } = new();
    /// <summary>Sample installer IDs (up to 5) when pattern is installer-scoped.</summary>
    public List<Guid> SampleInstallerIds { get; set; } = new();
    /// <summary>Display names for sample installers (same order as SampleInstallerIds).</summary>
    public List<string?> SampleInstallerDisplayNames { get; set; } = new();
    /// <summary>Short explanation for operators.</summary>
    public string? Explanation { get; set; }
    /// <summary>StrongSignal | ReviewNeeded | PartialCoverage</summary>
    public string Strength { get; set; } = "ReviewNeeded";
    /// <summary>Optional limitations or heuristic note.</summary>
    public string? Limitations { get; set; }
}

/// <summary>
/// Lightweight Building Reliability Score for operational prioritization.
/// Explainable band (Low/Moderate/High Risk) and contributing factors. Not for automated enforcement.
/// </summary>
public class SiBuildingReliabilityDto
{
    public List<SiBuildingReliabilityItemDto> Buildings { get; set; } = new();
    /// <summary>How the score is derived and its limitations.</summary>
    public string? InterpretationNote { get; set; }
}

public class SiBuildingReliabilityItemDto
{
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    /// <summary>LowRisk | ModerateRisk | HighRisk</summary>
    public string Band { get; set; } = "LowRisk";
    public int RescheduleCount { get; set; }
    public int BlockerCount { get; set; }
    public int HighChurnOrderCount { get; set; }
    public int StuckOrderCount { get; set; }
    public int AssuranceWithReplacementCount { get; set; }
    public int OrdersWithReplacementsCount { get; set; }
    /// <summary>Short human-readable summary for operators.</summary>
    public string? ReasonSummary { get; set; }
}

public class SiCompletionPerformanceDto
{
    /// <summary>Average hours from last Assigned to OrderCompleted (orders completed in window).</summary>
    public double? AverageAssignedToCompleteHours { get; set; }
    /// <summary>Count of orders used for the average.</summary>
    public int OrdersCompletedInWindow { get; set; }
    /// <summary>Average by installer (TriggeredBySiId at completion); installer id and average hours.</summary>
    public List<SiInstallerAverageDto> ByInstaller { get; set; } = new();
    /// <summary>Orders currently in a non-terminal status for longer than threshold days (e.g. &gt; 7 days).</summary>
    public List<SiStuckOrderDto> OrdersStuckLongerThanDays { get; set; } = new();
    public int StuckThresholdDays { get; set; }
}

public class SiInstallerAverageDto
{
    public Guid? SiId { get; set; }
    public string? SiDisplayName { get; set; }
    public double AverageHours { get; set; }
    public int OrderCount { get; set; }
}

public class SiStuckOrderDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysInCurrentStatus { get; set; }
}

public class SiRescheduleBlockerPatternsDto
{
    /// <summary>Reschedule reasons (TransitionReason when ToStatus=ReschedulePendingApproval), count.</summary>
    public List<SiReasonCountDto> TopRescheduleReasons { get; set; } = new();
    /// <summary>Blocker reasons (TransitionReason when ToStatus=Blocker), count.</summary>
    public List<SiReasonCountDto> TopBlockerReasons { get; set; } = new();
    /// <summary>Orders with multiple reschedules or blocker transitions (churn).</summary>
    public List<SiOrderChurnDto> OrdersWithHighChurn { get; set; } = new();
    public int ChurnThresholdTransitions { get; set; }
}

public class SiReasonCountDto
{
    public string? Reason { get; set; }
    public int Count { get; set; }
}

public class SiOrderChurnDto
{
    public Guid OrderId { get; set; }
    public int TransitionCount { get; set; }
    public int RescheduleCount { get; set; }
    public int BlockerCount { get; set; }
}

public class SiMaterialReplacementPatternsDto
{
    /// <summary>Replacement reasons (OrderMaterialReplacement.ReplacementReason), count.</summary>
    public List<SiReasonCountDto> TopReplacementReasons { get; set; } = new();
    /// <summary>Installers with most replacements (ReplacedBySiId).</summary>
    public List<SiInstallerCountDto> ByInstaller { get; set; } = new();
    /// <summary>Orders with more than one replacement.</summary>
    public int OrdersWithMultipleReplacements { get; set; }
}

public class SiInstallerCountDto
{
    public Guid? SiId { get; set; }
    public string? SiDisplayName { get; set; }
    public int Count { get; set; }
}

public class SiAssuranceReworkDto
{
    /// <summary>Assurance orders (OrderType.Code=ASSURANCE) completed in window.</summary>
    public int AssuranceOrdersCompletedInWindow { get; set; }
    /// <summary>Assurance orders with at least one material replacement.</summary>
    public int AssuranceOrdersWithReplacement { get; set; }
    /// <summary>Top Issue values on Assurance orders (from Order.Issue).</summary>
    public List<SiReasonCountDto> TopAssuranceIssues { get; set; } = new();
}

public class SiOperationalHotspotsDto
{
    /// <summary>Buildings (BuildingId) with most reschedules or blocker entries in window.</summary>
    public List<SiBuildingCountDto> BuildingsWithMostDisruptions { get; set; } = new();
    /// <summary>Note when area/building/project type is partial (e.g. building name only, no area code).</summary>
    public string? CoverageNote { get; set; }
}

public class SiBuildingCountDto
{
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public int RescheduleCount { get; set; }
    public int BlockerCount { get; set; }
}
