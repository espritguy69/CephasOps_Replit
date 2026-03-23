using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Utilities;

public class PhoneNumberUtilityTests
{
    [Theory]
    [InlineData("+60126556688", "0126556688")]
    [InlineData("60126556688", "0126556688")]
    [InlineData("122164657", "0122164657")]
    [InlineData("016-663-9910", "0166639910")]
    [InlineData("016 663 9910", "0166639910")]
    [InlineData("+60 12 655 6688", "0126556688")]
    [InlineData("0126556688", "0126556688")]
    [InlineData("1234567890", "01234567890")]
    public void NormalizePhoneNumber_VariousFormats_ReturnsNormalizedNumber(string input, string expected)
    {
        // Act
        var result = PhoneNumberUtility.NormalizePhoneNumber(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizePhoneNumber_NullOrEmpty_ReturnsEmptyString(string? input)
    {
        // Act
        var result = PhoneNumberUtility.NormalizePhoneNumber(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("no numbers here")]
    public void NormalizePhoneNumber_NoDigits_ReturnsEmptyString(string input)
    {
        // Act
        var result = PhoneNumberUtility.NormalizePhoneNumber(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("0126556688", true)]
    [InlineData("0166639910", true)]
    [InlineData("0312345678", true)]
    [InlineData("03-12345678", true)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidMalaysianPhone_VariousInputs_ReturnsExpectedResult(string? input, bool expected)
    {
        // Act
        var result = PhoneNumberUtility.IsValidMalaysianPhone(input);

        // Assert
        result.Should().Be(expected);
    }
}

