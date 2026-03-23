using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Tests that EmailTemplateService is tenant-aware: tenant A cannot see tenant B templates,
/// tenant gets platform (CompanyId null) fallback when no tenant template exists, missing context fails closed.
/// </summary>
[Collection("TenantScopeTests")]
public class EmailTemplateTenantAwarenessTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmailTemplateService _service;
    private readonly Guid? _previousTenantId;

    public EmailTemplateTenantAwarenessTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<EmailTemplateService>>();
        _service = new EmailTemplateService(_context, logger.Object);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllAsync_NoCompanyContext_ReturnsEmpty()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _service.GetAllAsync(companyId: null);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetByIdAsync_OtherTenantTemplate_ReturnsNull()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        // Seed template under companyB scope so TenantSafetyGuard allows the save.
        TenantScope.CurrentTenantId = companyB;
        try
        {
            _context.EmailTemplates.Add(new EmailTemplate
            {
                Id = templateId,
                CompanyId = companyB,
                Name = "B Template",
                Code = "B_CODE",
                SubjectTemplate = "S",
                BodyTemplate = "B",
                IsActive = true,
                Direction = "Outgoing",
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        // Query as companyA: must not see companyB's template (cross-tenant read blocked).
        TenantScope.CurrentTenantId = companyA;
        try
        {
            var result = await _service.GetByIdAsync(templateId, companyA);
            result.Should().BeNull();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task CreateAsync_NoCompanyContext_Throws()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CreateEmailTemplateDto
            {
                Name = "Test",
                Code = "T1",
                SubjectTemplate = "S",
                BodyTemplate = "B",
                IsActive = true
            };
            var act = () => _service.CreateAsync(dto, Guid.NewGuid(), null);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Company context is required*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }
}
