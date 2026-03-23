using System.Text.Json;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Executes document generation via IDocumentGenerationService (Phase 5).
/// Payload: documentType (required), entityId (required), companyId (required), userId (optional),
/// referenceEntity (optional), templateId (optional), format (optional), dataJson (optional), replaceExisting (optional bool).
/// When replaceExisting is not true, skips generation if a document already exists for same company+type+reference (idempotent).
/// </summary>
public sealed class DocumentGenerationJobExecutor : IJobExecutor
{
    public string JobType => "DocumentGeneration";

    private readonly IDocumentGenerationService _documentService;
    private readonly ILogger<DocumentGenerationJobExecutor> _logger;

    public DocumentGenerationJobExecutor(
        IDocumentGenerationService documentService,
        ILogger<DocumentGenerationJobExecutor> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing document generation job {JobId}", job.Id);

        var payload = ParsePayload(job.PayloadJson);
        var documentType = GetRequiredString(payload, "documentType");
        if (!GetRequiredGuid(payload, "entityId", out var entityId))
            throw new ArgumentException("Payload must contain a valid entityId (guid).");
        var companyId = job.CompanyId ?? GetOptionalGuid(payload, "companyId");
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new ArgumentException("CompanyId is required for document generation (set on job or in payload).");
        var userId = GetOptionalGuid(payload, "userId") ?? Guid.Empty;
        var referenceEntity = GetOptionalString(payload, "referenceEntity") ?? "Generic";
        var templateId = GetOptionalGuid(payload, "templateId");
        var format = GetOptionalString(payload, "format") ?? "Pdf";
        var replaceExisting = GetOptionalBool(payload, "replaceExisting");

        if (!replaceExisting)
        {
            var existing = await _documentService.GetGeneratedDocumentsAsync(
                companyId.Value, referenceEntity, entityId, documentType, cancellationToken);
            if (existing.Count > 0)
            {
                _logger.LogInformation(
                    "Document generation job {JobId} skipped (idempotent): document already exists for {DocumentType} {ReferenceEntity}/{ReferenceId}",
                    job.Id, documentType, referenceEntity, entityId);
                return true;
            }
        }

        Dictionary<string, object>? additionalData = null;
        if (payload.TryGetValue("dataJson", out var dataEl) && dataEl.ValueKind == JsonValueKind.String)
        {
            var dataStr = dataEl.GetString();
            if (!string.IsNullOrWhiteSpace(dataStr))
            {
                try
                {
                    additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(dataStr);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid dataJson in document generation job {JobId}, continuing without", job.Id);
                }
            }
        }

        var dto = new GenerateDocumentDto
        {
            DocumentType = documentType,
            ReferenceEntity = referenceEntity,
            ReferenceId = entityId,
            TemplateId = templateId,
            Format = format,
            AdditionalData = additionalData
        };

        var generated = await _documentService.GenerateDocumentAsync(dto, companyId.Value, userId, cancellationToken);
        if (generated == null)
            throw new InvalidOperationException($"Document generation returned null for {documentType} entity {entityId}.");

        _logger.LogInformation(
            "Document generation job {JobId} completed: {DocumentType} entity {EntityId} -> document {DocumentId}",
            job.Id, documentType, entityId, generated.Id);
        return true;
    }

    private static Dictionary<string, JsonElement> ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return new Dictionary<string, JsonElement>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson)
                ?? new Dictionary<string, JsonElement>();
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    private static string GetRequiredString(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var el))
            throw new ArgumentException($"Payload must contain '{key}'.");
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException($"Payload '{key}' must be non-empty.");
        return s.Trim();
    }

    private static bool GetRequiredGuid(Dictionary<string, JsonElement> payload, string key, out Guid value)
    {
        value = Guid.Empty;
        if (!payload.TryGetValue(key, out var el)) return false;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out value);
    }

    private static string? GetOptionalString(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var el)) return null;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static Guid? GetOptionalGuid(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var el)) return null;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        return Guid.TryParse(s, out var g) ? g : null;
    }

    private static bool GetOptionalBool(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var el)) return false;
        if (el.ValueKind == JsonValueKind.True) return true;
        if (el.ValueKind == JsonValueKind.False) return false;
        return bool.TryParse(el.ToString(), out var b) && b;
    }
}
