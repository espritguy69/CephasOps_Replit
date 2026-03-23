using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CephasOps.Application.Tests.Companies;

public class CompanyServiceTests
{
    [Fact]
    public async Task CreateCompanyAsync_PersistsCompany()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        var result = await service.CreateCompanyAsync(new CreateCompanyDto
        {
            LegalName = "Cephas Sdn Bhd",
            ShortName = "CEPHAS",
            Vertical = "ISP"
        });

        result.Id.Should().NotBeEmpty();
        result.ShortName.Should().Be("CEPHAS");

        var savedCompany = await context.Set<CephasOps.Domain.Companies.Entities.Company>()
            .SingleAsync();
        savedCompany.ShortName.Should().Be("CEPHAS");
        savedCompany.Vertical.Should().Be("ISP");
    }

    [Fact]
    public async Task CreateCompanyAsync_ThrowsWhenShortNameExists()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        await service.CreateCompanyAsync(new CreateCompanyDto
        {
            LegalName = "Cephas Sdn Bhd",
            ShortName = "CEPHAS",
            Vertical = "ISP"
        });

        var duplicateCall = () => service.CreateCompanyAsync(new CreateCompanyDto
        {
            LegalName = "Cephas Trading",
            ShortName = "cephas",
            Vertical = "Trading"
        });

        await duplicateCall.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetCompaniesAsync_FiltersBySearchTerm()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        // Single-company model: only one company allowed
        await service.CreateCompanyAsync(new CreateCompanyDto
        {
            LegalName = "Kingsman Classic Sdn Bhd",
            ShortName = "KINGSMAN",
            Vertical = "POS"
        });

        var filtered = await service.GetCompaniesAsync(search: "king");

        filtered.Should().HaveCount(1);
        filtered.Should().Contain(c => c.ShortName == "KINGSMAN");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CompanyServiceTests_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CompanyService CreateService(ApplicationDbContext context)
    {
        return new CompanyService(context, NullLogger<CompanyService>.Instance);
    }
}


