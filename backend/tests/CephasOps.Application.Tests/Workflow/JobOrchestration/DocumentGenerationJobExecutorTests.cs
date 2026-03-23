using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Workflow.JobOrchestration.Executors;
using CephasOps.Domain.Workflow.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

/// <summary>
/// Phase 5: Document generation executor — payload validation, company propagation, idempotent skip.
/// </summary>
public class DocumentGenerationJobExecutorTests
{
    private readonly Mock<IDocumentGenerationService> _documentService;
    private readonly Mock<ILogger<DocumentGenerationJobExecutor>> _logger;
    private readonly DocumentGenerationJobExecutor _executor;

    public DocumentGenerationJobExecutorTests()
    {
        _documentService = new Mock<IDocumentGenerationService>();
        _logger = new Mock<ILogger<DocumentGenerationJobExecutor>>();
        _executor = new DocumentGenerationJobExecutor(_documentService.Object, _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_When_DocumentType_Missing()
    {
        var job = new JobExecution
        {
            JobType = "DocumentGeneration",
            PayloadJson = """{"entityId":"a1b2c3d4-0000-0000-0000-000000000001","companyId":"a1b2c3d4-0000-0000-0000-000000000002"}""",
            CompanyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002")
        };
        await _executor.Invoking(e => e.ExecuteAsync(job)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*documentType*");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_When_EntityId_Missing()
    {
        var job = new JobExecution
        {
            JobType = "DocumentGeneration",
            PayloadJson = """{"documentType":"Invoice","companyId":"a1b2c3d4-0000-0000-0000-000000000002"}""",
            CompanyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002")
        };
        await _executor.Invoking(e => e.ExecuteAsync(job)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*entityId*");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_When_CompanyId_Missing()
    {
        var job = new JobExecution
        {
            JobType = "DocumentGeneration",
            PayloadJson = """{"documentType":"Invoice","entityId":"a1b2c3d4-0000-0000-0000-000000000001"}""",
            CompanyId = null
        };
        await _executor.Invoking(e => e.ExecuteAsync(job)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*CompanyId*");
    }

    [Fact]
    public async Task ExecuteAsync_Skips_When_Document_Exists_And_ReplaceExisting_False()
    {
        var companyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002");
        var entityId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
        var job = new JobExecution
        {
            JobType = "DocumentGeneration",
            PayloadJson = "{\"documentType\":\"Invoice\",\"entityId\":\"" + entityId + "\",\"companyId\":\"" + companyId + "\"}",
            CompanyId = companyId
        };
        _documentService
            .Setup(s => s.GetGeneratedDocumentsAsync(companyId, "Generic", entityId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeneratedDocumentDto> { new GeneratedDocumentDto { Id = Guid.NewGuid() } });

        var result = await _executor.ExecuteAsync(job);

        result.Should().BeTrue();
        _documentService.Verify(s => s.GenerateDocumentAsync(It.IsAny<GenerateDocumentDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Calls_GenerateDocumentAsync_When_No_Existing_Document()
    {
        var companyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002");
        var entityId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
        var job = new JobExecution
        {
            JobType = "DocumentGeneration",
            PayloadJson = "{\"documentType\":\"Invoice\",\"entityId\":\"" + entityId + "\",\"companyId\":\"" + companyId + "\"}",
            CompanyId = companyId
        };
        _documentService
            .Setup(s => s.GetGeneratedDocumentsAsync(companyId, "Generic", entityId, "Invoice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeneratedDocumentDto>());
        _documentService
            .Setup(s => s.GenerateDocumentAsync(It.IsAny<GenerateDocumentDto>(), companyId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedDocumentDto { Id = Guid.NewGuid() });

        var result = await _executor.ExecuteAsync(job);

        result.Should().BeTrue();
        _documentService.Verify(s => s.GenerateDocumentAsync(It.Is<GenerateDocumentDto>(d => d.DocumentType == "Invoice" && d.ReferenceId == entityId && d.ReferenceEntity == "Generic"), companyId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
