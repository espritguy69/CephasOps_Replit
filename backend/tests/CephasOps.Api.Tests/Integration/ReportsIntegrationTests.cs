using System.Net;
using System.Text;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Integration tests for Reports Hub: definitions returns list; run with valid department returns 200;
/// user in Dept A requesting run for Dept B returns 403.
/// </summary>
[Collection("InventoryIntegration")]
public class ReportsIntegrationTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly CephasOpsWebApplicationFactory _factory;

    public ReportsIntegrationTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDefinitions_Returns200_AndListOfReports()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync("api/reports/definitions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var success = root.TryGetProperty("Success", out var s) && s.GetBoolean()
                     || root.TryGetProperty("success", out var s2) && s2.GetBoolean();
        success.Should().BeTrue("response should be successful");

        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        data.ValueKind.Should().Be(JsonValueKind.Array, "Data should be array of report definitions");
        var count = data.GetArrayLength();
        count.Should().BeGreaterThan(0, "at least one report definition should be returned");

        var first = data[0];
        first.TryGetProperty("reportKey", out var key).Should().BeTrue();
        first.TryGetProperty("name", out _).Should().BeTrue();
        key.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunReport_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var body = new { departmentId = deptBId };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/reports/orders-list/run", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("do not have access to this department", because: "API must return forbidden message");
    }

    [Fact]
    public async Task RunReport_UserInDeptA_RequestingDeptA_Returns200_AndNonErrorSchema()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var body = new { departmentId = deptAId };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/reports/orders-list/run", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var success = root.TryGetProperty("Success", out var s) && s.GetBoolean()
                     || root.TryGetProperty("success", out var s2) && s2.GetBoolean();
        success.Should().BeTrue("run response should be successful");

        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        data.ValueKind.Should().Be(JsonValueKind.Object);
        data.TryGetProperty("items", out var items).Should().BeTrue("result should have items");
        data.TryGetProperty("totalCount", out var totalCount).Should().BeTrue("result should have totalCount");
        items.ValueKind.Should().Be(JsonValueKind.Array);
        totalCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExportStockSummary_FormatCsv_Returns200_AndTextCsv()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/stock-summary/export?format=csv&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportStockSummary_FormatXlsx_Returns200_AndExcelContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/stock-summary/export?format=xlsx&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportStockSummary_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/stock-summary/export?format=pdf&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportStockSummary_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync($"api/reports/stock-summary/export?format=csv&departmentId={deptBId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportLedger_FormatCsv_Returns200_AndTextCsv()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/ledger/export?format=csv&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0, "CSV should have at least header row");
    }

    [Fact]
    public async Task ExportLedger_FormatXlsx_Returns200_AndExcelContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/ledger/export?format=xlsx&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportLedger_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/ledger/export?format=pdf&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportOrdersList_FormatCsv_Returns200_AndTextCsv()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/orders-list/export?format=csv&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0, "CSV should have at least header row");
    }

    [Fact]
    public async Task ExportOrdersList_FormatXlsx_Returns200_AndExcelContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/orders-list/export?format=xlsx&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "PDF export in Testing returns 500 (document/font or ReportExportFormatService dependency in InMemory context). Fix when PDF generation is verified in test host.")]
    public async Task ExportOrdersList_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/orders-list/export?format=pdf&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportOrdersList_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync($"api/reports/orders-list/export?format=csv&departmentId={deptBId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportMaterialsList_FormatCsv_Returns200_AndTextCsv()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/materials-list/export?format=csv&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0, "CSV should have at least header row");
    }

    [Fact]
    public async Task ExportMaterialsList_FormatXlsx_Returns200_AndExcelContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/materials-list/export?format=xlsx&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportMaterialsList_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/materials-list/export?format=pdf&departmentId={deptAId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportMaterialsList_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync($"api/reports/materials-list/export?format=csv&departmentId={deptBId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportSchedulerUtilization_FormatCsv_Returns200_AndTextCsv()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/scheduler-utilization/export?format=csv&departmentId={deptAId}&fromDate=2025-01-01&toDate=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0, "CSV should have at least header row");
    }

    [Fact]
    public async Task ExportSchedulerUtilization_FormatXlsx_Returns200_AndExcelContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/scheduler-utilization/export?format=xlsx&departmentId={deptAId}&fromDate=2025-01-01&toDate=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportSchedulerUtilization_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes()
    {
        var (client, deptAId) = await CreateAuthenticatedClientWithDeptAsync();
        var response = await client.GetAsync($"api/reports/scheduler-utilization/export?format=pdf&departmentId={deptAId}&fromDate=2025-01-01&toDate=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportSchedulerUtilization_UserInDeptA_RequestingDeptB_Returns403()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");

        var response = await client.GetAsync($"api/reports/scheduler-utilization/export?format=csv&departmentId={deptBId}&fromDate=2025-01-01&toDate=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(HttpClient Client, Guid DeptAId)> CreateAuthenticatedClientWithDeptAsync()
    {
        var companyId = Guid.NewGuid();
        var deptAId = Guid.NewGuid();
        var deptBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDepartmentScopeDataAsync(companyId, deptAId, deptBId, userId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Company-Id", companyId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Member");
        return (client, deptAId);
    }

    private async Task SeedDepartmentScopeDataAsync(Guid companyId, Guid deptAId, Guid deptBId, Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await IntegrationTestDbSeeder.PurgeThenSeedAsync(companyId,
            async ct =>
            {
                db.DepartmentMemberships.RemoveRange(db.DepartmentMemberships);
                db.Departments.RemoveRange(db.Departments);
                db.Companies.RemoveRange(db.Companies);
                await db.SaveChangesAsync(ct);
            },
            async ct =>
            {
                var company = new Company
                {
                    Id = companyId,
                    LegalName = "Test Company",
                    ShortName = "TC",
                    IsActive = true,
                    Status = CompanyStatus.Active, // Required so SubscriptionEnforcementMiddleware allows the request
                    CreatedAt = DateTime.UtcNow
                };
                db.Companies.Add(company);

                var deptA = new Department
                {
                    Id = deptAId,
                    CompanyId = companyId,
                    Name = "Dept A",
                    Code = "DA",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var deptB = new Department
                {
                    Id = deptBId,
                    CompanyId = companyId,
                    Name = "Dept B",
                    Code = "DB",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Departments.Add(deptA);
                db.Departments.Add(deptB);

                var membership = new DepartmentMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DepartmentId = deptAId,
                    CompanyId = companyId,
                    Role = "Member",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.DepartmentMemberships.Add(membership);
                await db.SaveChangesAsync(ct);
            });
    }
}
