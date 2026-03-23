using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Application.ServiceInstallers.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Tests that PnlService and SkillService fail closed when company context is missing (no "single company mode").
/// </summary>
[Collection("TenantScopeTests")]
public class PnlAndSkillTenantIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PnlService _pnlService;
    private readonly SkillService _skillService;
    private readonly Guid? _previousTenantId;

    public PnlAndSkillTenantIsolationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var pnlLogger = new Mock<ILogger<PnlService>>();
        var skillLogger = new Mock<ILogger<SkillService>>();
        _pnlService = new PnlService(_context, pnlLogger.Object);
        _skillService = new SkillService(_context, skillLogger.Object);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PnlService_GetPnlSummaryAsync_GuidEmpty_NoTenantScope_ReturnsEmptySummary()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _pnlService.GetPnlSummaryAsync(Guid.Empty);
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(0);
            result.Facts.Should().BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task PnlService_GetPnlOrderDetailsAsync_GuidEmpty_NoTenantScope_ReturnsEmptyList()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _pnlService.GetPnlOrderDetailsAsync(Guid.Empty);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task PnlService_GetPnlPeriodsAsync_GuidEmpty_NoTenantScope_ReturnsEmptyList()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _pnlService.GetPnlPeriodsAsync(Guid.Empty);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task PnlService_CreateOverheadEntryAsync_GuidEmpty_NoTenantScope_Throws()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CreateOverheadEntryDto
            {
                Period = "2026-01",
                Amount = 100,
                Description = "Test",
                AllocationBasis = "Equal"
            };
            var act = () => _pnlService.CreateOverheadEntryAsync(dto, Guid.Empty, Guid.NewGuid());
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*tenant context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task PnlService_RebuildPnlAsync_GuidEmpty_NoTenantScope_Throws()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var act = () => _pnlService.RebuildPnlAsync(Guid.Empty, "2026-01");
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*tenant context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task SkillService_GetSkillsAsync_NullCompanyId_NoTenantScope_ReturnsEmptyList()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _skillService.GetSkillsAsync(companyId: null);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task SkillService_GetSkillByIdAsync_NullCompanyId_NoTenantScope_ReturnsNull()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _skillService.GetSkillByIdAsync(Guid.NewGuid(), companyId: null);
            result.Should().BeNull();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task SkillService_CreateSkillAsync_NullCompanyId_NoTenantScope_Throws()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CreateSkillDto
            {
                Name = "Test",
                Code = "T1",
                Category = "Cat",
                IsActive = true,
                DisplayOrder = 0
            };
            var act = () => _skillService.CreateSkillAsync(dto, companyId: null);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Company context is required*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }
}
