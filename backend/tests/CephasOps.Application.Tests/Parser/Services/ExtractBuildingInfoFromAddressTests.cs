using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Common.Services;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Services;

/// <summary>
/// Unit tests for ParserService.ExtractBuildingInfoFromAddress (invoked via reflection).
/// Covers keyword priority (MENARA > WISMA > ... > MEDAN), split on comma/newline, whole-word match, street/unit skip.
/// </summary>
public class ExtractBuildingInfoFromAddressTests
{
    private static (string? buildingName, string? city, string? postcode) InvokeExtractBuildingInfoFromAddress(string? addressText)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<ParserService>>();
        var service = new ParserService(
            context,
            Mock.Of<IOrderService>(),
            Mock.Of<ITimeExcelParserService>(),
            Mock.Of<IExcelToPdfService>(),
            Mock.Of<IPdfTextExtractionService>(),
            Mock.Of<IPdfOrderParserService>(),
            Mock.Of<IFileService>(),
            Mock.Of<IBuildingMatchingService>(),
            Mock.Of<IBuildingService>(),
            Mock.Of<IEncryptionService>(),
            Mock.Of<IParsedOrderDraftEnrichmentService>(),
            logger.Object,
            null);

        var method = typeof(ParserService).GetMethod("ExtractBuildingInfoFromAddress", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        var result = method!.Invoke(service, new object?[] { addressText });
        var tuple = ((string?, string?, string?))result!;
        return tuple;
    }

    [Fact]
    public void Menara_takes_priority_over_medan()
    {
        var address = "Level 21, Unit 5, MENARA ALFA BANGSAR, Jalan Maarof, BANGSAR, 59100, KUALA LUMPUR";
        var (buildingName, city, postcode) = InvokeExtractBuildingInfoFromAddress(address);
        buildingName.Should().Be("MENARA ALFA BANGSAR");
        postcode.Should().Be("59100");
        city.Should().Be("KUALA LUMPUR");
    }

    [Fact]
    public void Wisma_matched_whole_word_and_priority_over_medan()
    {
        var address = "Wisma ABC, Medan Jaya, 50000, Kuala Lumpur";
        var (buildingName, city, postcode) = InvokeExtractBuildingInfoFromAddress(address);
        buildingName.Should().Be("Wisma ABC");
        postcode.Should().Be("50000");
        city.Should().Be("Kuala Lumpur");
    }

    [Fact]
    public void Medan_whole_word_does_not_match_inside_other_word()
    {
        var address = "Subang Medan Complex, Jalan SS15, 47500 Petaling Jaya";
        var (buildingName, _, _) = InvokeExtractBuildingInfoFromAddress(address);
        buildingName.Should().Be("Subang Medan Complex");
    }

    [Fact]
    public void Split_on_newline_and_comma_trim_empty()
    {
        var address = "Unit 10\n\nPangsapuri Vista,\r\nJalan X, 60000, Kota";
        var (buildingName, city, postcode) = InvokeExtractBuildingInfoFromAddress(address);
        buildingName.Should().Be("Pangsapuri Vista");
        postcode.Should().Be("60000");
        city.Should().Be("Kota");
    }

    [Fact]
    public void Jalan_segment_ignored_for_building_name()
    {
        var address = "Jalan Sultan Ismail, Menara KLCC, 50088 Kuala Lumpur";
        var (buildingName, _, _) = InvokeExtractBuildingInfoFromAddress(address);
        buildingName.Should().Be("Menara KLCC");
    }
}
