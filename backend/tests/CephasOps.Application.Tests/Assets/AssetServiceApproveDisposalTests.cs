using CephasOps.Application.Assets.DTOs;
using CephasOps.Application.Assets.Services;
using CephasOps.Domain.Assets.Entities;
using CephasOps.Domain.Assets.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Assets;

/// <summary>
/// Focused tests for AssetService.ApproveDisposalAsync tenant-safety (explicit company-scoped asset lookup).
/// </summary>
public class AssetServiceApproveDisposalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AssetService _service;
    private readonly Guid? _previousTenantId;

    public AssetServiceApproveDisposalTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _previousTenantId = TenantScope.CurrentTenantId;
        _service = new AssetService(_context, Mock.Of<ILogger<AssetService>>());
    }

    /// <summary>
    /// ApproveDisposalAsync loads asset with explicit disposal.CompanyId; asset from another company must not be resolved or updated.
    /// </summary>
    [Fact]
    public async Task ApproveDisposalAsync_DoesNotResolveOrUpdateAssetFromAnotherCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var assetTypeA = Guid.NewGuid();
        var assetTypeB = Guid.NewGuid();
        var assetAId = Guid.NewGuid();
        var assetBId = Guid.NewGuid();
        var disposalId = Guid.NewGuid();

        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _context.Companies.Add(new Company { Id = companyA, ShortName = "A", LegalName = "Company A", IsActive = true });
            _context.Companies.Add(new Company { Id = companyB, ShortName = "B", LegalName = "Company B", IsActive = true });
            _context.Set<AssetType>().Add(new AssetType { Id = assetTypeA, CompanyId = companyA, Name = "TypeA", Code = "TA", IsActive = true });
            _context.Set<AssetType>().Add(new AssetType { Id = assetTypeB, CompanyId = companyB, Name = "TypeB", Code = "TB", IsActive = true });
            _context.Assets.Add(new Asset
            {
                Id = assetAId,
                CompanyId = companyA,
                AssetTypeId = assetTypeA,
                AssetTag = "A1",
                Name = "Asset A",
                Status = AssetStatus.Active,
                IsDeleted = false
            });
            _context.Assets.Add(new Asset
            {
                Id = assetBId,
                CompanyId = companyB,
                AssetTypeId = assetTypeB,
                AssetTag = "B1",
                Name = "Asset B",
                Status = AssetStatus.Active,
                IsDeleted = false
            });
            _context.AssetDisposals.Add(new AssetDisposal
            {
                Id = disposalId,
                CompanyId = companyA,
                AssetId = assetBId,
                DisposalMethod = DisposalMethod.Sale,
                DisposalDate = DateTime.UtcNow,
                BookValueAtDisposal = 0,
                DisposalProceeds = 0,
                GainLoss = 0,
                IsApproved = false
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        TenantScope.CurrentTenantId = companyA;
        try
        {
            await _service.ApproveDisposalAsync(
                disposalId,
                new ApproveAssetDisposalDto { Approved = true },
                companyA,
                Guid.NewGuid());
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var assetB = await _context.Assets.IgnoreQueryFilters().FirstAsync(a => a.Id == assetBId);
        assetB.Status.Should().Be(AssetStatus.Active, "asset from company B must not be updated when disposal is company A");
        assetB.CompanyId.Should().Be(companyB);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
