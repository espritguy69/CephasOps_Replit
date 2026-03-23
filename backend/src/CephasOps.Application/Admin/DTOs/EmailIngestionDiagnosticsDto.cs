namespace CephasOps.Application.Admin.DTOs;

/// <summary>
/// Read-only diagnostics for email ingestion (jobs, accounts, drafts). No secrets.
/// </summary>
public class EmailIngestionDiagnosticsDto
{
    /// <summary>UTC timestamp of the last successful EmailIngest job (CompletedAt when State=Succeeded), or null.</summary>
    public DateTime? LastSuccessfulEmailIngestAt { get; set; }

    /// <summary>Count of EmailIngest jobs created in the last 24h, grouped by State (e.g. Succeeded, Failed, Queued, Running).</summary>
    public Dictionary<string, int> EmailIngestJobsLast24hByState { get; set; } = new();

    /// <summary>Per-account LastPolledAt (Id, display name, LastPolledAt only; no secrets).</summary>
    public List<EmailAccountLastPolledDto> EmailAccountsLastPolledAt { get; set; } = new();

    /// <summary>Count of ParsedOrderDrafts created today (UTC date).</summary>
    public int DraftsCreatedToday { get; set; }
}

/// <summary>Per-account last poll time for diagnostics (Id, Name, Username, LastPolledAt only; no secrets).</summary>
public class EmailAccountLastPolledDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime? LastPolledAt { get; set; }
}
