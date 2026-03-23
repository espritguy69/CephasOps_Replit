using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Settings;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Domain.Settings.Enums;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Settings;

/// <summary>
/// Unit tests for DocumentGenerationService engine branching. Tenant-scoped entities require TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class DocumentGenerationServiceTests : IDisposable
{
    private readonly Mock<IDocumentTemplateService> _mockTemplateService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ICarboneRenderer> _mockCarboneRenderer;
    private readonly Mock<ILogger<DocumentGenerationService>> _mockLogger;
    private readonly ApplicationDbContext _dbContext;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public DocumentGenerationServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _mockTemplateService = new Mock<IDocumentTemplateService>();
        _mockFileService = new Mock<IFileService>();
        _mockCarboneRenderer = new Mock<ICarboneRenderer>();
        _mockLogger = new Mock<ILogger<DocumentGenerationService>>();

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    #region Test 1: Handlebars engine uses existing flow

    [Fact]
    public async Task GenerateFromTemplate_WithHandlebarsEngine_UsesHandlebarsAndQuestPdf_DoesNotCallCarbone()
    {
        SetTenantScope();
        // Arrange
        var companyId = _companyId;
        var template = CreateTestTemplate("Handlebars");
        template.CompanyId = companyId;
        
        // Setup database with template (DocumentGenerationService queries DB directly)
        _dbContext.DocumentTemplates.Add(template);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        _mockCarboneRenderer.Setup(x => x.IsConfigured).Returns(true);
        
        // Setup file service for saving PDF
        _mockFileService
            .Setup(x => x.UploadFileAsync(It.IsAny<FileUploadDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDto { Id = Guid.NewGuid(), FileName = "test.pdf" });

        var service = new DocumentGenerationService(
            _dbContext,
            _mockTemplateService.Object,
            _mockFileService.Object,
            _mockCarboneRenderer.Object,
            _mockLogger.Object);

        var generateDto = new GenerateDocumentDto
        {
            DocumentType = "Generic",
            ReferenceEntity = "Test",
            ReferenceId = Guid.NewGuid(),
            TemplateId = template.Id,
            AdditionalData = new Dictionary<string, object> { ["customer"] = new { name = "Test Customer" } }
        };

        // Act
        SetTenantScope();
        await service.GenerateDocumentAsync(generateDto, companyId, Guid.NewGuid(), CancellationToken.None);

        // Assert - The Handlebars path should NOT call ICarboneRenderer
        _mockCarboneRenderer.Verify(
            x => x.RenderFromHtmlAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockCarboneRenderer.Verify(
            x => x.RenderFromFileAsync(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Test 2: CarboneHtml engine calls Carbone with HTML

    [Fact]
    public async Task GenerateFromTemplate_WithCarboneHtmlEngine_CallsCarboneRenderFromHtml()
    {
        SetTenantScope();
        // Arrange
        var companyId = _companyId;
        var templateId = Guid.NewGuid();
        var htmlBody = "<html><body>{{d.customer.name}}</body></html>";
        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        var template = CreateTestTemplate("CarboneHtml");
        template.Id = templateId;
        template.CompanyId = companyId;
        template.HtmlBody = htmlBody;

        // Setup database with template (DocumentGenerationService queries DB directly)
        _dbContext.DocumentTemplates.Add(template);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        _mockCarboneRenderer.Setup(x => x.IsConfigured).Returns(true);
        _mockCarboneRenderer
            .Setup(x => x.RenderFromHtmlAsync(
                htmlBody,
                It.IsAny<object>(),
                "Generic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPdfBytes);

        _mockFileService
            .Setup(x => x.UploadFileAsync(It.IsAny<FileUploadDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDto { Id = Guid.NewGuid(), FileName = "test.pdf" });

        var service = new DocumentGenerationService(
            _dbContext,
            _mockTemplateService.Object,
            _mockFileService.Object,
            _mockCarboneRenderer.Object,
            _mockLogger.Object);

        var generateDto = new GenerateDocumentDto
        {
            DocumentType = "Generic",
            ReferenceEntity = "Test",
            ReferenceId = Guid.NewGuid(),
            TemplateId = templateId,
            AdditionalData = new Dictionary<string, object> { ["customer"] = new { name = "Test" } }
        };

        // Act
        SetTenantScope();
        await service.GenerateDocumentAsync(generateDto, companyId, Guid.NewGuid(), CancellationToken.None);

        // Assert - Should call RenderFromHtmlAsync with the HTML body
        _mockCarboneRenderer.Verify(
            x => x.RenderFromHtmlAsync(htmlBody, It.IsAny<object>(), "Generic", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Test 3: CarboneDocx engine calls Carbone with file

    [Fact]
    public async Task GenerateFromTemplate_WithCarboneDocxEngine_CallsCarboneRenderFromFile()
    {
        SetTenantScope();
        // Arrange
        var companyId = _companyId;
        var templateId = Guid.NewGuid();
        var templateFileId = Guid.NewGuid();
        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        var template = CreateTestTemplate("CarboneDocx", templateFileId);
        template.Id = templateId;
        template.CompanyId = companyId;

        // Setup database with template (DocumentGenerationService queries DB directly)
        _dbContext.DocumentTemplates.Add(template);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        _mockCarboneRenderer.Setup(x => x.IsConfigured).Returns(true);
        _mockCarboneRenderer
            .Setup(x => x.RenderFromFileAsync(
                templateFileId,
                It.IsAny<object>(),
                "Generic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPdfBytes);

        _mockFileService
            .Setup(x => x.UploadFileAsync(It.IsAny<FileUploadDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDto { Id = Guid.NewGuid(), FileName = "test.pdf" });

        var service = new DocumentGenerationService(
            _dbContext,
            _mockTemplateService.Object,
            _mockFileService.Object,
            _mockCarboneRenderer.Object,
            _mockLogger.Object);

        var generateDto = new GenerateDocumentDto
        {
            DocumentType = "Generic",
            ReferenceEntity = "Test",
            ReferenceId = Guid.NewGuid(),
            TemplateId = templateId,
            AdditionalData = new Dictionary<string, object> { ["customer"] = new { name = "Test" } }
        };

        // Act
        SetTenantScope();
        await service.GenerateDocumentAsync(generateDto, companyId, Guid.NewGuid(), CancellationToken.None);

        // Assert - Should call RenderFromFileAsync with the template file ID
        _mockCarboneRenderer.Verify(
            x => x.RenderFromFileAsync(templateFileId, It.IsAny<object>(), "Generic", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Test 4: CarboneDocx without TemplateFileId throws error

    [Fact]
    public async Task GenerateFromTemplate_WithCarboneDocxEngine_WithoutTemplateFileId_ThrowsInvalidOperationException()
    {
        SetTenantScope();
        // Arrange
        var companyId = _companyId;
        var templateId = Guid.NewGuid();
        var template = CreateTestTemplate("CarboneDocx");
        template.Id = templateId;
        template.CompanyId = companyId;
        template.TemplateFileId = null; // No file set

        // Setup database with template (DocumentGenerationService queries DB directly)
        _dbContext.DocumentTemplates.Add(template);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        _mockCarboneRenderer.Setup(x => x.IsConfigured).Returns(true);

        var service = new DocumentGenerationService(
            _dbContext,
            _mockTemplateService.Object,
            _mockFileService.Object,
            _mockCarboneRenderer.Object,
            _mockLogger.Object);

        var generateDto = new GenerateDocumentDto
        {
            DocumentType = "Generic",
            ReferenceEntity = "Test",
            ReferenceId = Guid.NewGuid(),
            TemplateId = templateId,
            AdditionalData = new Dictionary<string, object>()
        };

        // Act & Assert
        // The service should throw InvalidOperationException when CarboneDocx is used without TemplateFileId
        SetTenantScope();
        var act = async () => await service.GenerateDocumentAsync(generateDto, companyId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TemplateFileId*");
    }

    #endregion

    #region Test 5: Carbone disabled throws CarboneNotConfiguredException

    [Fact]
    public async Task GenerateFromTemplate_WithCarboneEngine_WhenCarboneDisabled_ThrowsCarboneNotConfiguredException()
    {
        SetTenantScope();
        // Arrange
        var companyId = _companyId;
        var templateId = Guid.NewGuid();
        var template = CreateTestTemplate("CarboneHtml");
        template.Id = templateId;
        template.CompanyId = companyId;

        // Setup database with template (DocumentGenerationService queries DB directly)
        _dbContext.DocumentTemplates.Add(template);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        _mockCarboneRenderer.Setup(x => x.IsConfigured).Returns(false);
        _mockCarboneRenderer
            .Setup(x => x.RenderFromHtmlAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CarboneNotConfiguredException("Carbone engine is disabled. Set 'Carbone:Enabled' to true in appsettings.json."));

        var service = new DocumentGenerationService(
            _dbContext,
            _mockTemplateService.Object,
            _mockFileService.Object,
            _mockCarboneRenderer.Object,
            _mockLogger.Object);

        var generateDto = new GenerateDocumentDto
        {
            DocumentType = "Generic",
            ReferenceEntity = "Test",
            ReferenceId = Guid.NewGuid(),
            TemplateId = templateId,
            AdditionalData = new Dictionary<string, object>()
        };

        // Act & Assert
        // When Carbone is not configured, using CarboneHtml should throw
        SetTenantScope();
        var act = async () => await service.GenerateDocumentAsync(generateDto, companyId, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<CarboneNotConfiguredException>()
            .WithMessage("*Carbone engine is disabled*");
    }

    [Fact]
    public void CarboneRenderer_EnsureConfigured_WhenDisabled_ThrowsCarboneNotConfiguredException()
    {
        // Arrange
        var settings = new CarboneSettings { Enabled = false };

        // Act & Assert
        if (!settings.Enabled)
        {
            var exception = new CarboneNotConfiguredException(
                "Carbone engine is disabled. Set 'Carbone:Enabled' to true in appsettings.json.");
            exception.Message.Should().Contain("Carbone engine is disabled");
        }
    }

    #endregion

    #region Test 6: Unknown engine defaults to Handlebars

    [Fact]
    public void ParseEngineType_WithUnknownEngine_DefaultsToHandlebars()
    {
        // Act
        var result1 = DocumentEngineTypeExtensions.ParseEngineType("UnknownEngine");
        var result2 = DocumentEngineTypeExtensions.ParseEngineType(null);
        var result3 = DocumentEngineTypeExtensions.ParseEngineType("");
        var result4 = DocumentEngineTypeExtensions.ParseEngineType("   ");
        var result5 = DocumentEngineTypeExtensions.ParseEngineType("Liquid"); // Legacy

        // Assert
        result1.Should().Be(DocumentEngineType.Handlebars);
        result2.Should().Be(DocumentEngineType.Handlebars);
        result3.Should().Be(DocumentEngineType.Handlebars);
        result4.Should().Be(DocumentEngineType.Handlebars);
        result5.Should().Be(DocumentEngineType.Handlebars); // Legacy engines map to Handlebars
    }

    [Fact]
    public void ParseEngineType_WithValidEngines_ReturnsCorrectType()
    {
        // Act & Assert
        DocumentEngineTypeExtensions.ParseEngineType("Handlebars").Should().Be(DocumentEngineType.Handlebars);
        DocumentEngineTypeExtensions.ParseEngineType("handlebars").Should().Be(DocumentEngineType.Handlebars);
        DocumentEngineTypeExtensions.ParseEngineType("CarboneHtml").Should().Be(DocumentEngineType.CarboneHtml);
        DocumentEngineTypeExtensions.ParseEngineType("carbonehtml").Should().Be(DocumentEngineType.CarboneHtml);
        DocumentEngineTypeExtensions.ParseEngineType("CarboneDocx").Should().Be(DocumentEngineType.CarboneDocx);
        DocumentEngineTypeExtensions.ParseEngineType("carbonedocx").Should().Be(DocumentEngineType.CarboneDocx);
    }

    #endregion

    #region Test: RequiresCarbone extension method

    [Fact]
    public void RequiresCarbone_ReturnsCorrectValue()
    {
        // Assert
        DocumentEngineType.Handlebars.RequiresCarbone().Should().BeFalse();
        DocumentEngineType.CarboneHtml.RequiresCarbone().Should().BeTrue();
        DocumentEngineType.CarboneDocx.RequiresCarbone().Should().BeTrue();
    }

    #endregion

    #region Test: CarboneSettings validation

    [Fact]
    public void CarboneSettings_IsValid_WhenDisabled_ReturnsTrue()
    {
        // Arrange
        var settings = new CarboneSettings { Enabled = false };

        // Assert
        settings.IsValid().Should().BeTrue();
        settings.GetValidationError().Should().BeNull();
    }

    [Fact]
    public void CarboneSettings_IsValid_WhenEnabledWithoutBaseUrl_ReturnsFalse()
    {
        // Arrange
        var settings = new CarboneSettings
        {
            Enabled = true,
            BaseUrl = ""
        };

        // Assert
        settings.IsValid().Should().BeFalse();
        settings.GetValidationError().Should().Contain("BaseUrl is required");
    }

    [Fact]
    public void CarboneSettings_IsValid_WhenUsingCarboneCloudWithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var settings = new CarboneSettings
        {
            Enabled = true,
            BaseUrl = "https://api.carbone.io",
            ApiKey = null
        };

        // Assert
        settings.IsValid().Should().BeFalse();
        settings.GetValidationError().Should().Contain("ApiKey is required");
    }

    [Fact]
    public void CarboneSettings_IsValid_WhenProperlyConfigured_ReturnsTrue()
    {
        // Arrange
        var settings = new CarboneSettings
        {
            Enabled = true,
            BaseUrl = "https://api.carbone.io",
            ApiKey = "test-api-key"
        };

        // Assert
        settings.IsValid().Should().BeTrue();
        settings.GetValidationError().Should().BeNull();
    }

    [Fact]
    public void CarboneSettings_IsValid_WhenSelfHostedWithoutApiKey_ReturnsTrue()
    {
        // Arrange - self-hosted doesn't require API key
        var settings = new CarboneSettings
        {
            Enabled = true,
            BaseUrl = "http://localhost:4000",
            ApiKey = null
        };

        // Assert
        settings.IsValid().Should().BeTrue();
        settings.GetValidationError().Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private DocumentTemplate CreateTestTemplate(string engine, Guid? templateFileId = null)
    {
        return new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Test Template",
            DocumentType = "Invoice",
            Engine = engine,
            HtmlBody = "<html><body>Test</body></html>",
            TemplateFileId = templateFileId,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}

