using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Utilities;

public class AddressParserTests
{
    [Fact]
    public void ParseAddress_FullMalaysianAddress_ExtractsCityStatePostcode()
    {
        // Arrange
        var address = "Block B, Level 33A, Unit 20, UNITED POINT, Jalan Taman Batu Permai, 47400 Petaling Jaya, Selangor";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.Postcode.Should().Be("47400");
        result.State.Should().Be("Selangor");
        result.City.Should().Be("Petaling Jaya");
        result.AddressLine1.Should().Be(address);
    }

    [Fact]
    public void ParseAddress_KualaLumpurAddress_ExtractsKualaLumpur()
    {
        // Arrange
        var address = "123 Jalan Bukit Bintang, 55100 Kuala Lumpur";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.Postcode.Should().Be("55100");
        // Kuala Lumpur is detected as both state and city
        result.State.Should().NotBeNull();
        result.City.Should().Be("Kuala Lumpur");
    }

    [Fact]
    public void ParseAddress_AddressWithUnitNumber_ExtractsUnitNo()
    {
        // Arrange
        var address = "Unit 15A, Block C, Condo Heights, 50000 Kuala Lumpur";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.UnitNo.Should().Be("15A");
    }

    [Fact]
    public void ParseAddress_AddressWithBuildingName_ExtractsBuildingName()
    {
        // Arrange
        var address = "Level 10, MENARA TM, Jalan Pantai Baharu, 59200 Kuala Lumpur";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.BuildingName.Should().Contain("MENARA TM");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseAddress_NullOrEmpty_ReturnsEmptyComponents(string? input)
    {
        // Act
        var result = AddressParser.ParseAddress(input);

        // Assert
        result.FullAddress.Should().BeEmpty();
        result.Postcode.Should().BeNull();
        result.State.Should().BeNull();
        result.City.Should().BeNull();
    }

    [Theory]
    [InlineData("Johor")]
    [InlineData("Kedah")]
    [InlineData("Kelantan")]
    [InlineData("Melaka")]
    [InlineData("Negeri Sembilan")]
    [InlineData("Pahang")]
    [InlineData("Perak")]
    [InlineData("Perlis")]
    [InlineData("Pulau Pinang")]
    [InlineData("Sabah")]
    [InlineData("Sarawak")]
    [InlineData("Selangor")]
    [InlineData("Terengganu")]
    public void ParseAddress_AllMalaysianStates_ExtractsStateCorrectly(string state)
    {
        // Arrange
        var address = $"123 Test Street, 12345 Test City, {state}";

        // Act
        var result = AddressParser.ParseAddress(address);

        // Assert
        result.State.Should().Be(state);
    }
}

