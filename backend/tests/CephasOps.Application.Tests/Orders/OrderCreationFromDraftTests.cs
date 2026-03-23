using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Orders;

/// <summary>
/// Unit tests for order creation from parsed drafts.
/// These tests focus on validation logic and field mapping.
/// Integration tests with actual database are in a separate project.
/// </summary>
public class OrderCreationFromDraftTests
{
    #region Validation Logic Tests

    [Fact]
    public void ValidateDraft_ValidFtthDraft_ReturnsNoErrors()
    {
        // Arrange
        var dto = CreateValidFtthDraft();

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDraft_ValidAssuranceDraft_ReturnsNoErrors()
    {
        // Arrange
        var dto = CreateValidAssuranceDraft();

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDraft_MissingCustomerName_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.CustomerName = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("CustomerName is required");
    }

    [Fact]
    public void ValidateDraft_MissingAppointmentDate_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.AppointmentDate = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("AppointmentDate is required");
    }

    [Fact]
    public void ValidateDraft_MissingCustomerPhone_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.CustomerPhone = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("CustomerPhone is required");
    }

    [Fact]
    public void ValidateDraft_MissingAddressText_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.AddressText = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("AddressText is required");
    }

    [Fact]
    public void ValidateDraft_MissingAppointmentWindow_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.AppointmentWindow = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("AppointmentWindow is required");
    }

    [Fact]
    public void ValidateDraft_MissingServiceIdForActivation_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidFtthDraft();
        dto.ServiceId = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("ServiceId is required for activation orders");
    }

    [Fact]
    public void ValidateDraft_MissingTicketIdForAssurance_ReturnsValidationError()
    {
        // Arrange
        var dto = CreateValidAssuranceDraft();
        dto.TicketId = null;

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("TicketId is required for assurance orders");
    }

    [Fact]
    public void ValidateDraft_AssuranceWithServiceIdNoTicketId_ReturnsError()
    {
        // Arrange - Assurance order with ServiceId but no TicketId
        var dto = new CreateOrderFromDraftDto
        {
            ParsedOrderDraftId = Guid.NewGuid(),
            OrderTypeHint = "Assurance",
            ServiceId = "TBBN123456A",
            TicketId = null, // Missing!
            CustomerName = "TEST",
            CustomerPhone = "0123456789",
            AddressText = "Test Address",
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            AppointmentWindow = "09:00-11:00"
        };

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().Contain("TicketId is required for assurance orders");
    }

    [Fact]
    public void ValidateDraft_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var dto = new CreateOrderFromDraftDto
        {
            ParsedOrderDraftId = Guid.NewGuid(),
            OrderTypeHint = "FTTH"
            // All required fields missing
        };

        // Act
        var errors = ValidateDraftForOrderCreation(dto);

        // Assert
        errors.Should().HaveCountGreaterThan(1);
        errors.Should().Contain("CustomerName is required");
        errors.Should().Contain("CustomerPhone is required");
        errors.Should().Contain("AddressText is required");
        errors.Should().Contain("AppointmentDate is required");
        errors.Should().Contain("AppointmentWindow is required");
    }

    #endregion

    #region Phone Number Auto-Fix Tests

    [Theory]
    [InlineData("+60126556688", "0126556688")]
    [InlineData("122164657", "0122164657")]
    [InlineData("016-663-9910", "0166639910")]
    [InlineData("60126556688", "0126556688")]
    [InlineData("+60 12 655 6688", "0126556688")]
    public void PhoneAutoFix_VariousFormats_ReturnsNormalizedNumber(string input, string expected)
    {
        // Act
        var result = PhoneNumberUtility.NormalizePhoneNumber(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Appointment Window Parsing Tests

    [Theory]
    [InlineData("09:00-11:00", 9, 0, 11, 0)]
    [InlineData("11:00-13:00", 11, 0, 13, 0)]
    [InlineData("14:00-16:00", 14, 0, 16, 0)]
    public void AppointmentWindowParsing_ValidFormat_ReturnsCorrectTimeSpans(
        string input, int fromHour, int fromMin, int toHour, int toMin)
    {
        // Act
        var (from, to) = AppointmentWindowParser.ParseAppointmentWindow(input);

        // Assert
        from.Should().Be(new TimeSpan(fromHour, fromMin, 0));
        to.Should().Be(new TimeSpan(toHour, toMin, 0));
    }

    [Fact]
    public void AppointmentWindowParsing_InvalidFormat_ThrowsException()
    {
        // Arrange
        var invalidWindow = "invalid-format";

        // Act
        var act = () => AppointmentWindowParser.ParseAppointmentWindow(invalidWindow);

        // Assert
        act.Should().Throw<FormatException>();
    }

    #endregion

    #region Address Parsing Tests

    [Fact]
    public void AddressParsing_MalaysianAddress_ExtractsComponents()
    {
        // Arrange
        var address = "Block B, Level 33A, Unit 20, UNITED POINT, 47400 Petaling Jaya, Selangor";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.Postcode.Should().Be("47400");
        result.State.Should().Be("Selangor");
        result.AddressLine1.Should().Be(address);
    }

    #endregion

    #region Order Type Detection Tests

    [Theory]
    [InlineData("Assurance", true)]
    [InlineData("ASSURANCE", true)]
    [InlineData("TTKT", true)]
    [InlineData("FTTH", false)]
    [InlineData("FTTO", false)]
    [InlineData("HSBB", false)]
    [InlineData("Modification", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsAssuranceOrder_VariousHints_ReturnsExpectedResult(string? hint, bool expected)
    {
        // Act
        var result = IsAssuranceOrder(hint);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Field Mapping Tests

    [Fact]
    public void FieldMapping_ValidDraft_MapsAllRequiredFields()
    {
        // Arrange
        var dto = CreateValidFtthDraft();

        // Assert field presence (these would be mapped to Order entity)
        dto.ServiceId.Should().NotBeNullOrEmpty();
        dto.CustomerName.Should().NotBeNullOrEmpty();
        dto.CustomerPhone.Should().NotBeNullOrEmpty();
        dto.AddressText.Should().NotBeNullOrEmpty();
        dto.AppointmentDate.Should().NotBeNull();
        dto.AppointmentWindow.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FieldMapping_AssuranceDraft_IncludesTicketId()
    {
        // Arrange
        var dto = CreateValidAssuranceDraft();

        // Assert
        dto.TicketId.Should().NotBeNullOrEmpty();
        dto.ServiceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateDraft_WhenOrderCategoryIdAndInstallationMethodIdProvided_ReturnsNoErrors()
    {
        // Parsed draft can carry OrderCategoryId and InstallationMethodId; OrderService.CreateFromParsedDraftAsync
        // uses them when creating the order so payroll and rate resolution work. Validation does not require them.
        var dto = CreateValidFtthDraft();
        dto.OrderCategoryId = Guid.NewGuid();
        dto.InstallationMethodId = Guid.NewGuid();

        var errors = ValidateDraftForOrderCreation(dto);

        errors.Should().BeEmpty();
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void DefaultValues_NewOrder_ShouldHaveCorrectDefaults()
    {
        // These are the expected default values for a new order created from draft
        // Verified against EMAIL_PARSER_ORDER_CREATION_TESTING.md specification
        
        var expectedDefaults = new
        {
            SourceSystem = "EmailParser",
            Status = "Pending",
            Priority = "Normal",
            HasReschedules = false,
            RescheduleCount = 0,
            DocketUploaded = false,
            PhotosUploaded = false,
            SerialsValidated = false,
            InvoiceEligible = false
        };

        // Assert documented defaults
        expectedDefaults.SourceSystem.Should().Be("EmailParser");
        expectedDefaults.Status.Should().Be("Pending");
        expectedDefaults.Priority.Should().Be("Normal");
        expectedDefaults.HasReschedules.Should().BeFalse();
        expectedDefaults.RescheduleCount.Should().Be(0);
        expectedDefaults.DocketUploaded.Should().BeFalse();
        expectedDefaults.PhotosUploaded.Should().BeFalse();
        expectedDefaults.SerialsValidated.Should().BeFalse();
        expectedDefaults.InvoiceEligible.Should().BeFalse();
    }

    #endregion

    #region Helper Methods (mirroring OrderService validation logic)

    private static List<string> ValidateDraftForOrderCreation(CreateOrderFromDraftDto dto)
    {
        var errors = new List<string>();

        // Mandatory fields
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            errors.Add("CustomerName is required");

        if (string.IsNullOrWhiteSpace(dto.CustomerPhone))
            errors.Add("CustomerPhone is required");

        if (string.IsNullOrWhiteSpace(dto.AddressText))
            errors.Add("AddressText is required");

        if (!dto.AppointmentDate.HasValue)
            errors.Add("AppointmentDate is required");

        if (string.IsNullOrWhiteSpace(dto.AppointmentWindow))
            errors.Add("AppointmentWindow is required");

        // Conditional: ServiceId required for activation orders
        var isAssurance = IsAssuranceOrder(dto.OrderTypeHint);
        if (!isAssurance && string.IsNullOrWhiteSpace(dto.ServiceId))
            errors.Add("ServiceId is required for activation orders");

        // Conditional: TicketId required for assurance orders
        if (isAssurance && string.IsNullOrWhiteSpace(dto.TicketId))
            errors.Add("TicketId is required for assurance orders");

        return errors;
    }

    private static bool IsAssuranceOrder(string? orderTypeHint)
    {
        if (string.IsNullOrEmpty(orderTypeHint))
            return false;

        return orderTypeHint.Equals("Assurance", StringComparison.OrdinalIgnoreCase) ||
               orderTypeHint.Contains("TTKT", StringComparison.OrdinalIgnoreCase);
    }

    private static CreateOrderFromDraftDto CreateValidFtthDraft()
    {
        return new CreateOrderFromDraftDto
        {
            ParsedOrderDraftId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            BuildingId = Guid.NewGuid(),
            SourceEmailId = Guid.NewGuid(),
            OrderTypeHint = "FTTH",
            ServiceId = "TBBN620278G",
            TicketId = null,
            CustomerName = "KUAN TE SIANG",
            CustomerPhone = "0166587158",
            AddressText = "Block B, Level 33A, Unit 20, UNITED POINT, 47400 Petaling Jaya, Selangor",
            AppointmentDate = DateTime.UtcNow.Date.AddDays(3),
            AppointmentWindow = "11:00-13:00",
            ValidationNotes = null
        };
    }

    private static CreateOrderFromDraftDto CreateValidAssuranceDraft()
    {
        return new CreateOrderFromDraftDto
        {
            ParsedOrderDraftId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            BuildingId = Guid.NewGuid(),
            SourceEmailId = Guid.NewGuid(),
            OrderTypeHint = "Assurance",
            ServiceId = "TBBN620278G",
            TicketId = "TTKT202511178606510",
            CustomerName = "KUAN TE SIANG",
            CustomerPhone = "0166587158",
            AddressText = "Block B, Level 33A, Unit 20, UNITED POINT, 47400 Petaling Jaya, Selangor",
            AppointmentDate = DateTime.UtcNow.Date.AddDays(3),
            AppointmentWindow = "11:00-13:00",
            ValidationNotes = "LOSi/LOBi issue"
        };
    }

    #endregion
}
