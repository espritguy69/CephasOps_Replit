using System.Diagnostics;
using System.Net.Http.Headers;
using CephasOps.Application.Integration;

namespace CephasOps.Api.Integration;

/// <summary>
/// Sends outbound integration payloads via HTTP. Uses a dedicated HttpClient (no auth by default).
/// </summary>
public class OutboundHttpDispatcher : IOutboundHttpDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OutboundHttpDispatcher(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HttpDispatchResult> SendAsync(
        string url,
        string httpMethod,
        string payloadJson,
        IReadOnlyDictionary<string, string> headers,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("IntegrationOutbound");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));

        using var request = new HttpRequestMessage(new HttpMethod(httpMethod), url);
        request.Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

        foreach (var (key, value) in headers)
        {
            if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
            else
                request.Headers.TryAddWithoutValidation(key, value);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            var snippet = body.Length > 2000 ? body[..2000] : body;
            return new HttpDispatchResult
            {
                Success = response.IsSuccessStatusCode,
                HttpStatusCode = (int)response.StatusCode,
                ResponseBodySnippet = snippet,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"{response.StatusCode}: {snippet}",
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HttpDispatchResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }
}
