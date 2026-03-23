using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Builds document generation payload and enqueues via IJobExecutionEnqueuer (Phase 5).
/// </summary>
public class DocumentGenerationJobEnqueuer : IDocumentGenerationJobEnqueuer
{
    private readonly IJobExecutionEnqueuer _enqueuer;
    private readonly ILogger<DocumentGenerationJobEnqueuer> _logger;

    public DocumentGenerationJobEnqueuer(IJobExecutionEnqueuer enqueuer, ILogger<DocumentGenerationJobEnqueuer> logger)
    {
        _enqueuer = enqueuer;
        _logger = logger;
    }

    public async Task EnqueueAsync(
        string documentType,
        Guid entityId,
        Guid companyId,
        Guid? userId = null,
        string? referenceEntity = null,
        Guid? templateId = null,
        string? format = null,
        string? dataJson = null,
        bool replaceExisting = false,
        string? correlationId = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Document type is required.", nameof(documentType));
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));

        var payload = new Dictionary<string, object?>
        {
            ["documentType"] = documentType.Trim(),
            ["entityId"] = entityId.ToString(),
            ["companyId"] = companyId.ToString(),
            ["referenceEntity"] = referenceEntity ?? "Generic",
            ["format"] = format ?? "Pdf",
            ["replaceExisting"] = replaceExisting
        };
        if (userId.HasValue && userId.Value != Guid.Empty)
            payload["userId"] = userId.Value.ToString();
        if (templateId.HasValue)
            payload["templateId"] = templateId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(dataJson))
            payload["dataJson"] = dataJson;

        var payloadJson = JsonSerializer.Serialize(payload);
        await _enqueuer.EnqueueAsync(
            "DocumentGeneration",
            payloadJson,
            companyId,
            correlationId,
            causationId,
            priority: 0,
            nextRunAtUtc: null,
            maxAttempts: 5,
            cancellationToken);
        _logger.LogInformation(
            "Enqueued document generation job: {DocumentType} entity {EntityId} company {CompanyId}",
            documentType, entityId, companyId);
    }
}
