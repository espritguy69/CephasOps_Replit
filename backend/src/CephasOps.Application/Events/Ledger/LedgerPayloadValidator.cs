using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Validates ledger payload snapshots: valid JSON (object or array) and max size.
/// Oversized payloads are replaced with a metadata placeholder; invalid JSON is rejected.
/// </summary>
public sealed class LedgerPayloadValidator : ILedgerPayloadValidator
{
    private readonly LedgerOptions _options;

    public LedgerPayloadValidator(Microsoft.Extensions.Options.IOptions<LedgerOptions> options)
    {
        _options = options?.Value ?? new LedgerOptions();
    }

    public LedgerPayloadValidationResult Validate(
        string? payloadSnapshot,
        string ledgerFamily,
        string eventType)
    {
        if (string.IsNullOrEmpty(payloadSnapshot))
            return LedgerPayloadValidationResult.Accept(null);

        var size = System.Text.Encoding.UTF8.GetByteCount(payloadSnapshot);
        if (size > _options.MaxPayloadSizeBytes)
        {
            var placeholder = JsonSerializer.Serialize(new
            {
                _ledgerPayloadTruncated = true,
                _message = "Payload exceeded max size; replaced with placeholder.",
                _originalSizeBytes = size,
                _maxPayloadSizeBytes = _options.MaxPayloadSizeBytes,
                _ledgerFamily = ledgerFamily,
                _eventType = eventType
            });
            return LedgerPayloadValidationResult.Truncated(placeholder, size);
        }

        if (!_options.ValidateJsonPayload)
            return LedgerPayloadValidationResult.Accept(payloadSnapshot);

        try
        {
            using var doc = JsonDocument.Parse(payloadSnapshot);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object && root.ValueKind != JsonValueKind.Array)
                return LedgerPayloadValidationResult.Reject($"JSON root must be Object or Array; got {root.ValueKind}.", size);
            return LedgerPayloadValidationResult.Accept(payloadSnapshot);
        }
        catch (JsonException ex)
        {
            return LedgerPayloadValidationResult.Reject($"Invalid JSON: {ex.Message}", size);
        }
    }
}
