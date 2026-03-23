using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

public class SlaEvaluationSchedulerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public SlaEvaluationSchedulerServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "SlaEvalScheduler_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ScheduleSlaEvaluationJobCoreAsync_EnqueuesOneJobPerActiveCompany_WhenNonePending()
    {
        var companyId = Guid.NewGuid();
        _context.Companies.Add(new Company { Id = companyId, IsActive = true, LegalName = "Test", ShortName = "T", Code = "T" });
        await _context.SaveChangesAsync();

        var enqueuerMock = new Mock<IJobExecutionEnqueuer>();
        enqueuerMock
            .Setup(e => e.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<SlaEvaluationSchedulerService>>();
        var scheduler = new SlaEvaluationSchedulerService(_serviceProvider, logger.Object);
        var method = typeof(SlaEvaluationSchedulerService).GetMethod("ScheduleSlaEvaluationJobCoreAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();

        await (Task)method!.Invoke(scheduler, new object[] { _context, enqueuerMock.Object, CancellationToken.None })!;

        enqueuerMock.Verify(
            e => e.EnqueueAsync("SlaEvaluation", "{}", companyId, null, null, It.IsAny<int>(), null, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleSlaEvaluationJobCoreAsync_EnqueuesPerTenant_WhenMultipleActiveCompanies()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        _context.Companies.Add(new Company { Id = companyA, IsActive = true, LegalName = "A", ShortName = "A", Code = "A" });
        _context.Companies.Add(new Company { Id = companyB, IsActive = true, LegalName = "B", ShortName = "B", Code = "B" });
        await _context.SaveChangesAsync();

        var enqueuerMock = new Mock<IJobExecutionEnqueuer>();
        enqueuerMock
            .Setup(e => e.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<SlaEvaluationSchedulerService>>();
        var scheduler = new SlaEvaluationSchedulerService(_serviceProvider, logger.Object);
        var method = typeof(SlaEvaluationSchedulerService).GetMethod("ScheduleSlaEvaluationJobCoreAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();

        await (Task)method!.Invoke(scheduler, new object[] { _context, enqueuerMock.Object, CancellationToken.None })!;

        // Per-tenant: one enqueue per active company, each with that company's id.
        enqueuerMock.Verify(
            e => e.EnqueueAsync("SlaEvaluation", "{}", companyA, null, null, It.IsAny<int>(), null, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        enqueuerMock.Verify(
            e => e.EnqueueAsync("SlaEvaluation", "{}", companyB, null, null, It.IsAny<int>(), null, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        enqueuerMock.Verify(
            e => e.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    public void Dispose() => _context.Dispose();
}
