using CephasOps.Application.Workflow.JobOrchestration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

/// <summary>
/// Phase 5: Document generation enqueuer — builds payload and calls IJobExecutionEnqueuer with CompanyId.
/// </summary>
public class DocumentGenerationJobEnqueuerTests
{
    private readonly Mock<IJobExecutionEnqueuer> _enqueuer;
    private readonly Mock<ILogger<DocumentGenerationJobEnqueuer>> _logger;
    private readonly DocumentGenerationJobEnqueuer _sut;

    public DocumentGenerationJobEnqueuerTests()
    {
        _enqueuer = new Mock<IJobExecutionEnqueuer>();
        _logger = new Mock<ILogger<DocumentGenerationJobEnqueuer>>();
        _sut = new DocumentGenerationJobEnqueuer(_enqueuer.Object, _logger.Object);
    }

    [Fact]
    public async Task EnqueueAsync_Calls_Enqueuer_With_DocumentGeneration_Type_And_CompanyId()
    {
        var companyId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002");
        var entityId = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
        string? capturedPayload = null;
        Guid? capturedCompanyId = null;
        _enqueuer
            .Setup(s => s.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, Guid?, string?, Guid?, int, DateTime?, int, CancellationToken>((type, payload, cid, _, _, _, _, _, _) => { capturedPayload = payload; capturedCompanyId = cid; })
            .Returns(Task.CompletedTask);

        await _sut.EnqueueAsync("Invoice", entityId, companyId, cancellationToken: default);

        _enqueuer.Verify(s => s.EnqueueAsync("DocumentGeneration", It.IsAny<string>(), companyId, null, null, 0, null, 5, It.IsAny<CancellationToken>()), Times.Once);
        capturedPayload.Should().NotBeNull();
        capturedPayload.Should().Contain("Invoice").And.Contain(entityId.ToString()).And.Contain(companyId.ToString());
        capturedCompanyId.Should().Be(companyId);
    }

    [Fact]
    public async Task EnqueueAsync_Throws_When_CompanyId_Empty()
    {
        await _sut.Invoking(s => s.EnqueueAsync("Invoice", Guid.NewGuid(), Guid.Empty, cancellationToken: default))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Company*");
    }

    [Fact]
    public async Task EnqueueAsync_Throws_When_DocumentType_Null()
    {
        await _sut.Invoking(s => s.EnqueueAsync(null!, Guid.NewGuid(), Guid.NewGuid(), cancellationToken: default))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Document type*");
    }
}
