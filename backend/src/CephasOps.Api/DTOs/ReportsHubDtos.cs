namespace CephasOps.Api.DTOs;

/// <summary>
/// Report definition for the Reports Hub (list + search). Not the same as Settings ReportDefinition (scheduled reports).
/// </summary>
public class ReportDefinitionHubDto
{
    public string ReportKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Category { get; set; }
    public List<ReportParameterSchemaDto> ParameterSchema { get; set; } = new();
    public bool SupportsExport { get; set; }
}

/// <summary>
/// Schema for one filter parameter (drives UI).
/// </summary>
public class ReportParameterSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, guid, datetime, int, bool
    public bool Required { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Result of running a report (table rows + paging).
/// </summary>
public class RunReportResultDto
{
    public List<object> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Request body for running a report. All fields optional; which are used depends on reportKey.
/// </summary>
public class RunReportRequestDto
{
    public Guid? DepartmentId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? AssignedSiId { get; set; }
    public string? Keyword { get; set; }
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsSerialised { get; set; }
    public bool? IsActive { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? OrderId { get; set; }
    public string? EntryType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? SiId { get; set; }
}
