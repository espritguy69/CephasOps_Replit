namespace CephasOps.Application.Integration;

/// <summary>
/// Sends outbound integration payload via HTTP. Implemented in Infrastructure with HttpClient.
/// </summary>
public interface IOutboundHttpDispatcher
{
    Task<HttpDispatchResult> SendAsync(
        string url,
        string httpMethod,
        string payloadJson,
        IReadOnlyDictionary<string, string> headers,
        int timeoutSeconds,
        CancellationToken cancellationToken = default);
}

public sealed class HttpDispatchResult
{
    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseBodySnippet { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
}
