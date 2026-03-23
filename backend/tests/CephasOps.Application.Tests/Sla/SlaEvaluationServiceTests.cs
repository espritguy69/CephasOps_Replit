using CephasOps.Application.Sla;
using CephasOps.Domain.Events;
using CephasOps.Domain.Sla.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Sla;

[Collection("TenantScopeTests")]
public class SlaEvaluationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public SlaEvaluationServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "SlaEvaluation_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_NoRules_ReturnsZeroCounts()
    {
        var service = new SlaEvaluationService(_context);
        var result = await service.EvaluateAsync(null);
        result.RulesEvaluated.Should().Be(0);
        result.BreachesRecorded.Should().Be(0);
        result.WarningsRecorded.Should().Be(0);
        result.EscalationsRecorded.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateAsync_WorkflowTransition_ExceedingMaxDuration_RecordsBreach()
    {
        SetTenantScope();
        var companyId = _companyId;
        var wfDefId = Guid.NewGuid();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = wfDefId,
            CompanyId = companyId,
            EntityType = "Order",
            Name = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.SlaRules.Add(new SlaRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            RuleType = SlaEvaluationService.RuleTypeWorkflowTransition,
            TargetType = "workflow",
            TargetName = "*",
            MaxDurationSeconds = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var created = DateTime.UtcNow.AddSeconds(-120);
        var completed = DateTime.UtcNow;
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = wfDefId,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CreatedAt = created,
            StartedAt = created.AddSeconds(5),
            CompletedAt = completed,
            UpdatedAt = completed
        });
        SetTenantScope();
        await _context.SaveChangesAsync();

        SetTenantScope();
        var service = new SlaEvaluationService(_context);
        var result = await service.EvaluateAsync(_companyId);

        result.RulesEvaluated.Should().Be(1);
        result.BreachesRecorded.Should().Be(1);
        var breach = await _context.SlaBreaches.SingleOrDefaultAsync();
        breach.Should().NotBeNull();
        breach!.TargetType.Should().Be("workflow");
        breach.Severity.Should().Be(SlaEvaluationService.SeverityBreach);
        breach.Status.Should().Be("Open");
    }

    [Fact]
    public async Task EvaluateAsync_CompanyScoping_OnlyEvaluatesMatchingCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var wfDefA = Guid.NewGuid();
        var wfDefB = Guid.NewGuid();
        _context.WorkflowDefinitions.AddRange(
            new WorkflowDefinition { Id = wfDefA, CompanyId = companyA, EntityType = "Order", Name = "A", IsActive = true, CreatedAt = DateTime.UtcNow },
            new WorkflowDefinition { Id = wfDefB, CompanyId = companyB, EntityType = "Order", Name = "B", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        _context.SlaRules.Add(new SlaRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyA,
            RuleType = SlaEvaluationService.RuleTypeWorkflowTransition,
            TargetType = "workflow",
            TargetName = "*",
            MaxDurationSeconds = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var past = DateTime.UtcNow.AddSeconds(-120);
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyB,
            WorkflowDefinitionId = wfDefB,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CreatedAt = past,
            CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var service = new SlaEvaluationService(_context);
        await service.EvaluateAsync(companyA);

        var breaches = await _context.SlaBreaches.ToListAsync();
        breaches.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WarningThreshold_RecordsWarning()
    {
        SetTenantScope();
        var companyId = _companyId;
        var wfDefId = Guid.NewGuid();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = wfDefId,
            CompanyId = companyId,
            EntityType = "Order",
            Name = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.SlaRules.Add(new SlaRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            RuleType = SlaEvaluationService.RuleTypeWorkflowTransition,
            TargetType = "workflow",
            TargetName = "*",
            MaxDurationSeconds = 300,
            WarningThresholdSeconds = 50,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var created = DateTime.UtcNow.AddSeconds(-70);
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = wfDefId,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CreatedAt = created,
            CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        SetTenantScope();
        await _context.SaveChangesAsync();

        SetTenantScope();
        var service = new SlaEvaluationService(_context);
        var result = await service.EvaluateAsync(_companyId);

        result.WarningsRecorded.Should().Be(1);
        var breach = await _context.SlaBreaches.SingleOrDefaultAsync();
        breach.Should().NotBeNull();
        breach!.Severity.Should().Be(SlaEvaluationService.SeverityWarning);
    }

    [Fact]
    public async Task EvaluateAsync_ExistingOpenBreach_DoesNotDuplicate()
    {
        var companyId = _companyId;
        var ruleId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var wfDefId = Guid.NewGuid();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = wfDefId,
            CompanyId = companyId,
            EntityType = "Order",
            Name = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.SlaRules.Add(new SlaRule
        {
            Id = ruleId,
            CompanyId = companyId,
            RuleType = SlaEvaluationService.RuleTypeWorkflowTransition,
            TargetType = "workflow",
            TargetName = "*",
            MaxDurationSeconds = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowJobs.Add(new WorkflowJob
        {
            Id = targetId,
            CompanyId = companyId,
            WorkflowDefinitionId = wfDefId,
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = "Pending",
            TargetStatus = "Assigned",
            State = WorkflowJobState.Succeeded,
            CreatedAt = DateTime.UtcNow.AddSeconds(-120),
            CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SlaBreaches.Add(new SlaBreach
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            CompanyId = companyId,
            TargetType = "workflow",
            TargetId = targetId.ToString(),
            Status = "Open",
            Severity = SlaEvaluationService.SeverityBreach,
            DetectedAtUtc = DateTime.UtcNow,
            DurationSeconds = 120
        });
        await _context.SaveChangesAsync();

        var service = new SlaEvaluationService(_context);
        await service.EvaluateAsync(null);

        var count = await _context.SlaBreaches.CountAsync();
        count.Should().Be(1);
    }
}
