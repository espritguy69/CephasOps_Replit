using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Services;

/// <summary>
/// Unit tests for ParserTemplateService. Tenant-scoped (no bypass).
/// Tests template matching logic
/// </summary>
[Collection("TenantScopeTests")]
public class ParserTemplateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ParserTemplateService _service;
    private readonly Mock<ILogger<ParserTemplateService>> _mockLogger;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public ParserTemplateServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ParserTemplateService>>();
        _service = new ParserTemplateService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task FindMatchingTemplateAsync_WithExactMatch_ShouldReturnTemplate()
    {
        // Arrange
        var template = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME Activation",
            Code = "TIME_ACTIVATION",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Activation*",
            Priority = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParserTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindMatchingTemplateAsync(
            "noreply@time.com.my",
            "FTTH Activation Work Order",
            null,
            null,
            false,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TIME_ACTIVATION", result.Code);
    }

    [Fact]
    public async Task FindMatchingTemplateAsync_WithNoMatch_ShouldReturnNull()
    {
        // Arrange
        var template = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME Activation",
            Code = "TIME_ACTIVATION",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Activation*",
            Priority = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParserTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindMatchingTemplateAsync(
            "other@example.com",
            "Unrelated Email",
            null,
            null,
            false,
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatchingTemplateAsync_WithMultipleMatches_ShouldReturnHighestPriority()
    {
        // Arrange
        var template1 = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME General",
            Code = "TIME_GENERAL",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Work Order*",
            Priority = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var template2 = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME Activation",
            Code = "TIME_ACTIVATION",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Activation*",
            Priority = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParserTemplates.Add(template1);
        _context.ParserTemplates.Add(template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindMatchingTemplateAsync(
            "noreply@time.com.my",
            "FTTH Activation Work Order",
            null,
            null,
            false,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TIME_ACTIVATION", result.Code); // Higher priority should win
    }

    [Fact]
    public async Task TestTemplateAsync_WithMatchingData_ShouldReturnMatched()
    {
        // Arrange
        var template = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME Activation",
            Code = "TIME_ACTIVATION",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Activation*",
            Priority = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParserTemplates.Add(template);
        await _context.SaveChangesAsync();

        var testData = new ParserTemplateTestDataDto
        {
            FromAddress = "noreply@time.com.my",
            Subject = "FTTH Activation Work Order",
            HasAttachments = false
        };

        // Act
        var result = await _service.TestTemplateAsync(template.Id, testData, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Matched);
        Assert.NotNull(result.MatchDetails);
        Assert.True(result.MatchDetails.FromAddressMatched);
        Assert.True(result.MatchDetails.SubjectMatched);
    }

    [Fact]
    public async Task TestTemplateAsync_WithNonMatchingData_ShouldReturnNotMatched()
    {
        // Arrange
        var template = new ParserTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "TIME Activation",
            Code = "TIME_ACTIVATION",
            PartnerPattern = "*@time.com.my",
            SubjectPattern = "*Activation*",
            Priority = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParserTemplates.Add(template);
        await _context.SaveChangesAsync();

        var testData = new ParserTemplateTestDataDto
        {
            FromAddress = "other@example.com",
            Subject = "Unrelated Email",
            HasAttachments = false
        };

        // Act
        var result = await _service.TestTemplateAsync(template.Id, testData, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Matched);
        Assert.NotNull(result.ErrorMessage);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}

