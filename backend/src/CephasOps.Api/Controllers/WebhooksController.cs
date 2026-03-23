using CephasOps.Application.Integration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Inbound webhook receiver. Receives POST by connector key, verifies and processes via IInboundWebhookRuntime.
/// Phase 10. No auth by default (external systems); verification is per-connector.
/// </summary>
[ApiController]
[Route("api/integration/webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IInboundWebhookRuntime _runtime;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IInboundWebhookRuntime runtime, ILogger<WebhooksController> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    /// <summary>
    /// Process inbound webhook for the given connector key.
    /// POST /api/integration/webhooks/{connectorKey}
    /// Optional header: X-Company-Id (guid), X-Event-Id (external id), X-Signature, X-Timestamp (verification).
    /// </summary>
    [HttpPost("{connectorKey}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Receive(
        [FromRoute] string connectorKey,
        [FromHeader(Name = "X-Company-Id")] Guid? companyId = null,
        [FromHeader(Name = "X-Event-Id")] string? externalEventId = null,
        [FromHeader(Name = "X-Signature")] string? signature = null,
        [FromHeader(Name = "X-Timestamp")] string? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectorKey))
            return BadRequest("Connector key required.");

        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(cancellationToken);

        var request = new InboundWebhookRequest
        {
            ConnectorKey = connectorKey.Trim(),
            CompanyId = companyId,
            SignatureHeader = signature,
            TimestampHeader = timestamp,
            ExternalEventId = externalEventId,
            RequestBody = body,
            ContentType = Request.ContentType
        };

        var result = await _runtime.ProcessAsync(request, cancellationToken);

        if (!result.Accepted)
        {
            _logger.LogWarning("Webhook rejected: ConnectorKey={Key}, Reason={Reason}", connectorKey, result.FailureReason);
            return StatusCode(result.SuggestedHttpStatusCode ?? 400, new { accepted = false, reason = result.FailureReason });
        }

        return Ok(new
        {
            accepted = true,
            receiptId = result.ReceiptId,
            idempotencyReused = result.IdempotencyReused,
            verificationPassed = result.VerificationPassed
        });
    }
}
