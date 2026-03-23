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
/// Focused tests for AssetService.CreateDisposalAsync tenant-safety (explicit company-scoped asset lookup when companyId is provided).
/// </summary>
public class AssetServiceCreateDisposalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AssetService _service;
    private readonly Guid? _previousTenantId;

    public AssetServiceCreateDisposalTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _previousTenantId = TenantScope.CurrentTenantId;
        _service = new AssetService(_context, Mock.Of<ILogger<AssetService>>());
    }

    /// <summary>
    /// When companyId is provided, CreateDisposalAsync must not resolve an asset from another company; must throw and not create a disposal.
    /// </summary>
    [Fact]
    public async Task CreateDisposalAsync_WhenCompanyIdProvided_DoesNotCreateDisposalForAssetFromAnotherCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var assetTypeA = Guid.NewGuid();
        var assetTypeB = Guid.NewGuid();
        var assetAId = Guid.NewGuid();
        var assetBId = Guid.NewGuid();

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
                CurrentBookValue = 100,
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
                CurrentBookValue = 200,
                IsDeleted = false
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        var dto = new CreateAssetDisposalDto
        {
            AssetId = assetBId,
            DisposalMethod = DisposalMethod.Sale,
            DisposalDate = DateTime.UtcNow,
            DisposalProceeds = 150
        };

        TenantScope.CurrentTenantId = companyA;
        try
        {
            Func<Task> act = () => _service.CreateDisposalAsync(dto, companyA, Guid.NewGuid());
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var disposalsForAssetB = await _context.AssetDisposals
            .IgnoreQueryFilters()
            .Where(d => d.AssetId == assetBId)
            .ToListAsync();
        disposalsForAssetB.Should().BeEmpty("disposal must not be created for another company's asset");
    }

    /// <summary>
    /// When companyId is provided and the asset belongs to that company, disposal is created successfully.
    /// </summary>
    [Fact]
    public async Task CreateDisposalAsync_WhenCompanyIdProvided_AndSameCompanyAsset_CreatesDisposal()
    {
        var companyA = Guid.NewGuid();
        var assetTypeA = Guid.NewGuid();
        var assetAId = Guid.NewGuid();

        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _context.Companies.Add(new Company { Id = companyA, ShortName = "A", LegalName = "Company A", IsActive = true });
            _context.Set<AssetType>().Add(new AssetType { Id = assetTypeA, CompanyId = companyA, Name = "TypeA", Code = "TA", IsActive = true });
            _context.Assets.Add(new Asset
            {
                Id = assetAId,
                CompanyId = companyA,
                AssetTypeId = assetTypeA,
                AssetTag = "A1",
                Name = "Asset A",
                Status = AssetStatus.Active,
                CurrentBookValue = 100,
                IsDeleted = false
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        var dto = new CreateAssetDisposalDto
        {
            AssetId = assetAId,
            DisposalMethod = DisposalMethod.Sale,
            DisposalDate = DateTime.UtcNow,
            DisposalProceeds = 80
        };

        TenantScope.CurrentTenantId = companyA;
        AssetDisposalDto result;
        try
        {
            result = await _service.CreateDisposalAsync(dto, companyA, Guid.NewGuid());
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        result.Should().NotBeNull();
        result.AssetId.Should().Be(assetAId);
        result.CompanyId.Should().Be(companyA);

        var disposal = await _context.AssetDisposals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.AssetId == assetAId);
        disposal.Should().NotBeNull();
        disposal!.CompanyId.Should().Be(companyA);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
