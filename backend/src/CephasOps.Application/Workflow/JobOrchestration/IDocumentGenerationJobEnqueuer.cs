namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Enqueues document generation as a JobExecution (Phase 5). Use this instead of BackgroundJob for async document generation.
/// </summary>
public interface IDocumentGenerationJobEnqueuer
{
    /// <summary>
    /// Enqueue a document generation job. Payload: documentType, entityId, optional referenceEntity, templateId, format, dataJson, replaceExisting.
    /// CompanyId and userId are passed explicitly and also set on the job for audit.
    /// </summary>
    Task EnqueueAsync(
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
        CancellationToken cancellationToken = default);
}
