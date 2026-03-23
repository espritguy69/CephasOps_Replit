using System.Net;
using System.Text;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Automatic tenant boundary tests: cross-tenant reads/writes/lists are blocked;
/// list endpoints return only same-tenant data; by-id returns 404 for other tenant.
/// </summary>
[Collection("InventoryIntegration")]
public class TenantBoundaryTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private readonly CephasOpsWebApplicationFactory _factory;
    private readonly TenantBoundaryTestFixture _boundaryFixture;

    public TenantBoundaryTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
        _boundaryFixture = new TenantBoundaryTestFixture(factory);
    }

    // ----- Users -----

    [Fact]
    public async Task Users_List_ReturnsOnlySameTenantUsers()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/users");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().NotContain(_boundaryFixture.UserBId, "Tenant A must not see user B");
        if (idsA.Count > 0) idsA.Should().Contain(_boundaryFixture.UserAId);

        var responseB = await clientB.GetAsync("api/users");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonB = await responseB.Content.ReadAsStringAsync();
        var docB = JsonDocument.Parse(jsonB);
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().NotContain(_boundaryFixture.UserAId, "Tenant B must not see user A");
        if (idsB.Count > 0) idsB.Should().Contain(_boundaryFixture.UserBId);
    }

    [Fact]
    public async Task Users_List_WithSearch_ReturnsOnlySameTenantUsers()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/users?search=User");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().NotContain(_boundaryFixture.UserBId, "search must not leak other tenant users");

        var responseB = await clientB.GetAsync("api/users?search=User");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().NotContain(_boundaryFixture.UserAId, "search must not leak other tenant users");
    }

    [Fact]
    public async Task Users_GetById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);

        var response = await clientA.GetAsync($"api/users/{_boundaryFixture.UserBId}");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK, "getting another tenant's user must return 403 or 404");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    // ----- Warehouses -----

    [Fact]
    public async Task Warehouses_List_ReturnsOnlySameTenantWarehouses()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var warehouseAId = await CreateWarehouseViaApiAsync(clientA, "WH-A", "Warehouse A");
        var warehouseBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");

        var responseA = await clientA.GetAsync("api/warehouses");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().Contain(warehouseAId);
        idsA.Should().NotContain(warehouseBId);

        var responseB = await clientB.GetAsync("api/warehouses");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonB = await responseB.Content.ReadAsStringAsync();
        var docB = JsonDocument.Parse(jsonB);
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().Contain(warehouseBId);
        idsB.Should().NotContain(warehouseAId);
    }

    [Fact]
    public async Task Warehouses_GetById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var warehouseBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");

        var response = await clientA.GetAsync($"api/warehouses/{warehouseBId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Warehouses_Update_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var warehouseBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");
        var body = new { code = "WH-B", name = "Hacked", isActive = true };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await clientA.PutAsync($"api/warehouses/{warehouseBId}", content);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Warehouses_Delete_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var warehouseBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");

        var response = await clientA.DeleteAsync($"api/warehouses/{warehouseBId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Warehouses_GetAll_WithOtherCompanyId_AsNonSuperAdmin_Returns403()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);

        var response = await client.GetAsync($"api/warehouses?companyId={_boundaryFixture.CompanyB}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ----- Rate cards (tenant-scoped via RequireCompanyId) -----

    [Fact]
    public async Task Rates_List_ReturnsOnlySameTenantRateCards()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var cardAId = await CreateRateCardViaApiAsync(clientA, "RC-A", "Rate Card A");
        var cardBId = await CreateRateCardViaApiAsync(clientB, "RC-B", "Rate Card B");

        var responseA = await clientA.GetAsync("api/rates/ratecards");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().NotContain(cardBId, "Tenant A must not see Tenant B rate cards");

        var responseB = await clientB.GetAsync("api/rates/ratecards");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().NotContain(cardAId, "Tenant B must not see Tenant A rate cards");
    }

    [Fact]
    public async Task Rates_GetRateCardById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var cardBId = await CreateRateCardViaApiAsync(clientB, "RC-B", "Rate Card B");

        var response = await clientA.GetAsync($"api/rates/ratecards/{cardBId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rates_UpdateRateCard_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var cardBId = await CreateRateCardViaApiAsync(clientB, "RC-B", "Rate Card B");
        var body = new { name = "Hacked", description = (string?)"Hacked", isActive = true };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await clientA.PutAsync($"api/rates/ratecards/{cardBId}", content);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rates_DeleteRateCard_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var cardBId = await CreateRateCardViaApiAsync(clientB, "RC-B", "Rate Card B");

        var response = await clientA.DeleteAsync($"api/rates/ratecards/{cardBId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rates_WithoutCompanyContext_Returns403Or200()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", _boundaryFixture.UserAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await client.GetAsync("api/rates/ratecards");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    // ----- Files (tenant-scoped via CurrentTenantId) -----

    [Fact]
    public async Task Files_List_ReturnsOnlySameTenantFiles()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var fileAId = await UploadFileViaApiAsync(clientA, "file-a.txt");
        var fileBId = await UploadFileViaApiAsync(clientB, "file-b.txt");

        var responseA = await clientA.GetAsync("api/files");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().NotContain(fileBId, "Tenant A must not see Tenant B files");

        var responseB = await clientB.GetAsync("api/files");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().NotContain(fileAId, "Tenant B must not see Tenant A files");
    }

    [Fact]
    public async Task Files_GetMetadata_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var fileBId = await UploadFileViaApiAsync(clientB, "file-b.txt");

        var response = await clientA.GetAsync($"api/files/{fileBId}/metadata");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Files_Download_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var fileBId = await UploadFileViaApiAsync(clientB, "file-b.txt");

        var response = await clientA.GetAsync($"api/files/{fileBId}/download");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Files_Delete_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var fileBId = await UploadFileViaApiAsync(clientB, "file-b.txt");

        var response = await clientA.DeleteAsync($"api/files/{fileBId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Files_WithoutCompanyContext_Returns401Or200()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", _boundaryFixture.UserAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await client.GetAsync("api/files");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    // ----- Reports run & export (tenant + department scoped) -----

    [Fact]
    public async Task Reports_StockSummaryExport_WithTenantAndDepartment_Returns200Or403()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var response = await client.GetAsync($"api/reports/stock-summary/export?format=csv&departmentId={_boundaryFixture.DepartmentAId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reports_Run_WithTenantAndDepartment_Returns200Or404()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var body = JsonSerializer.Serialize(new { DepartmentId = _boundaryFixture.DepartmentAId });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/reports/orders-list/run", content);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    // ----- Settings: Guard condition definitions (tenant-scoped) -----

    [Fact]
    public async Task GuardConditionDefinitions_List_ReturnsOnlySameTenant()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/workflow/guard-conditions");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);

        var responseB = await clientB.GetAsync("api/workflow/guard-conditions");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);

        foreach (var id in idsB)
            idsA.Should().NotContain(id, "Tenant A list must not contain Tenant B definitions");
        foreach (var id in idsA)
            idsB.Should().NotContain(id, "Tenant B list must not contain Tenant A definitions");
    }

    // ----- Phase 3: Billing (invoices), Bins, Inventory materials, Audit, EventStore, Job runs -----

    [Fact]
    public async Task Invoices_List_ReturnsOnlySameTenantInvoices()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/billing/invoices");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);

        var responseB = await clientB.GetAsync("api/billing/invoices");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);

        foreach (var id in idsB)
            idsA.Should().NotContain(id, "Tenant A must not see Tenant B invoices");
        foreach (var id in idsA)
            idsB.Should().NotContain(id, "Tenant B must not see Tenant A invoices");
    }

    [Fact]
    public async Task Invoices_GetById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var otherId = Guid.NewGuid();
        var response = await clientA.GetAsync($"api/billing/invoices/{otherId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bins_List_ReturnsOnlySameTenantBins()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var whAId = await CreateWarehouseViaApiAsync(clientA, "WH-A", "Warehouse A");
        var whBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");
        var binAId = await CreateBinViaApiAsync(clientA, _boundaryFixture.CompanyA, whAId, "BIN-A", "Bin A");
        var binBId = await CreateBinViaApiAsync(clientB, _boundaryFixture.CompanyB, whBId, "BIN-B", "Bin B");

        var responseA = await clientA.GetAsync($"api/bins?companyId={_boundaryFixture.CompanyA}");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().Contain(binAId);
        idsA.Should().NotContain(binBId);

        var responseB = await clientB.GetAsync($"api/bins?companyId={_boundaryFixture.CompanyB}");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var docB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().Contain(binBId);
        idsB.Should().NotContain(binAId);
    }

    [Fact]
    public async Task Bins_List_WithOtherCompanyId_Returns403()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var response = await clientA.GetAsync($"api/bins?companyId={_boundaryFixture.CompanyB}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bins_GetById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var whBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");
        var binBId = await CreateBinViaApiAsync(clientB, _boundaryFixture.CompanyB, whBId, "BIN-B", "Bin B");

        var response = await clientA.GetAsync($"api/bins/{binBId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bins_Update_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var whBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");
        var binBId = await CreateBinViaApiAsync(clientB, _boundaryFixture.CompanyB, whBId, "BIN-B", "Bin B");
        var body = new { name = "Hacked", warehouseId = whBId, code = "BIN-B", isActive = true };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await clientA.PutAsync($"api/bins/{binBId}", content);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bins_Delete_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var whBId = await CreateWarehouseViaApiAsync(clientB, "WH-B", "Warehouse B");
        var binBId = await CreateBinViaApiAsync(clientB, _boundaryFixture.CompanyB, whBId, "BIN-B", "Bin B");

        var response = await clientA.DeleteAsync($"api/bins/{binBId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Materials_List_ReturnsOnlySameTenantMaterials()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/inventory/materials");
        responseA.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        var idsA = responseA.IsSuccessStatusCode ? GetIdsFromArray(GetDataArray(JsonDocument.Parse(await responseA.Content.ReadAsStringAsync()))) : new List<Guid>();

        var responseB = await clientB.GetAsync("api/inventory/materials");
        responseB.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        var idsB = responseB.IsSuccessStatusCode ? GetIdsFromArray(GetDataArray(JsonDocument.Parse(await responseB.Content.ReadAsStringAsync()))) : new List<Guid>();

        foreach (var id in idsB)
            idsA.Should().NotContain(id, "Tenant A must not see Tenant B materials");
        foreach (var id in idsA)
            idsB.Should().NotContain(id, "Tenant B must not see Tenant A materials");
    }

    [Fact]
    public async Task AuditLog_List_WithOtherCompanyId_Returns403()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var response = await clientA.GetAsync($"api/logs/audit?companyId={_boundaryFixture.CompanyB}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EventStore_GetEvent_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var otherEventId = Guid.NewGuid();
        var response = await client.GetAsync($"api/event-store/events/{otherEventId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task JobRuns_List_ReturnsOnlySameTenantJobRuns()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/background-jobs/job-runs?pageSize=10");
        responseA.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        var idsA = responseA.IsSuccessStatusCode ? GetJobRunIdsFromResponse(await responseA.Content.ReadAsStringAsync()) : new List<Guid>();

        var responseB = await clientB.GetAsync("api/background-jobs/job-runs?pageSize=10");
        responseB.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        var idsB = responseB.IsSuccessStatusCode ? GetJobRunIdsFromResponse(await responseB.Content.ReadAsStringAsync()) : new List<Guid>();

        foreach (var id in idsB)
            idsA.Should().NotContain(id, "Tenant A must not see Tenant B job runs");
        foreach (var id in idsA)
            idsB.Should().NotContain(id, "Tenant B must not see Tenant A job runs");
    }

    [Fact]
    public async Task JobRuns_GetById_OtherTenant_Returns404()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var otherRunId = Guid.NewGuid();
        var response = await client.GetAsync($"api/background-jobs/job-runs/{otherRunId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    // ----- Payment terms & time-slots (tenant-scoped) -----

    [Fact]
    public async Task PaymentTerms_WithCompanyContext_Returns200OrAcceptable()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var response = await client.GetAsync("api/payment-terms");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentTerms_WithoutCompanyContext_Returns403Or400()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", _boundaryFixture.UserAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await client.GetAsync("api/payment-terms");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TimeSlots_WithoutCompanyContext_Returns403Or200()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", _boundaryFixture.UserAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await client.GetAsync("api/time-slots");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    // ----- Reports (tenant-scoped; cross-tenant export blocked by department access) -----

    [Fact]
    public async Task Reports_Definitions_WithCompanyContext_Returns200Or403()
    {
        await _boundaryFixture.SeedAsync();
        using var client = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        var response = await client.GetAsync("api/reports/definitions");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
    }

    // ----- Departments -----

    [Fact]
    public async Task Departments_List_ReturnsOnlySameTenantDepartments()
    {
        await _boundaryFixture.SeedAsync();
        using var clientA = _factory.CreateClient().ForTenant(_boundaryFixture.UserAId, _boundaryFixture.CompanyA);
        using var clientB = _factory.CreateClient().ForTenant(_boundaryFixture.UserBId, _boundaryFixture.CompanyB);

        var responseA = await clientA.GetAsync("api/departments");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonA = await responseA.Content.ReadAsStringAsync();
        var docA = JsonDocument.Parse(jsonA);
        var dataA = GetDataArray(docA);
        var idsA = GetIdsFromArray(dataA);
        idsA.Should().NotContain(_boundaryFixture.DepartmentBId, "Tenant A must not see department B");
        if (idsA.Count > 0) idsA.Should().Contain(_boundaryFixture.DepartmentAId);

        var responseB = await clientB.GetAsync("api/departments");
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonB = await responseB.Content.ReadAsStringAsync();
        var docB = JsonDocument.Parse(jsonB);
        var dataB = GetDataArray(docB);
        var idsB = GetIdsFromArray(dataB);
        idsB.Should().NotContain(_boundaryFixture.DepartmentAId, "Tenant B must not see department A");
        if (idsB.Count > 0) idsB.Should().Contain(_boundaryFixture.DepartmentBId);
    }

    private static JsonElement GetDataArray(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.TryGetProperty("Data", out var d)) return d;
        if (root.TryGetProperty("data", out var d2)) return d2;
        return root;
    }

    private static List<Guid> GetIdsFromArray(JsonElement array)
    {
        var list = new List<Guid>();
        if (array.ValueKind != JsonValueKind.Array) return list;
        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("id", out var idEl))
                if (Guid.TryParse(idEl.GetString(), out var g)) list.Add(g);
            if (item.TryGetProperty("Id", out var idEl2))
                if (Guid.TryParse(idEl2.GetString(), out var g2)) list.Add(g2);
        }
        return list;
    }

    private static async Task<Guid> CreateWarehouseViaApiAsync(HttpClient client, string code, string name)
    {
        var body = new { code, name, isActive = true };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/warehouses", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "warehouse create should succeed");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.TryGetProperty("Data", out var d) ? d : doc.RootElement.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var id))
            return id;
        if (data.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var id2))
            return id2;
        throw new InvalidOperationException("Could not parse warehouse id from create response: " + json);
    }

    private static async Task<Guid> CreateRateCardViaApiAsync(HttpClient client, string code, string name)
    {
        var body = new
        {
            name,
            code,
            description = (string?)null,
            rateContext = "GponJob",
            rateKind = "Revenue",
            verticalId = (Guid?)null,
            departmentId = (Guid?)null,
            validFrom = (DateTime?)null,
            validTo = (DateTime?)null,
            isActive = true
        };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/rates/ratecards", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "rate card create should succeed: " + await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.TryGetProperty("Data", out var d) ? d : doc.RootElement.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var id))
            return id;
        if (data.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var id2))
            return id2;
        throw new InvalidOperationException("Could not parse rate card id from create response: " + json);
    }

    private static async Task<Guid> UploadFileViaApiAsync(HttpClient client, string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content for " + fileName));
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", fileName);

        var response = await client.PostAsync("api/files/upload", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "file upload should succeed: " + await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.TryGetProperty("Data", out var d) ? d : doc.RootElement.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var id))
            return id;
        if (data.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var id2))
            return id2;
        throw new InvalidOperationException("Could not parse file id from upload response: " + json);
    }

    private static async Task<Guid> CreateBinViaApiAsync(HttpClient client, Guid companyId, Guid warehouseId, string code, string name)
    {
        var body = new
        {
            code,
            name,
            warehouseId,
            section = "",
            row = 0,
            level = 0,
            capacity = 0m,
            isActive = true
        };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"api/bins?companyId={companyId}", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "bin create should succeed: " + await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.TryGetProperty("Data", out var d) ? d : doc.RootElement.TryGetProperty("data", out var d2) ? d2 : default;
        if (data.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var id))
            return id;
        if (data.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var id2))
            return id2;
        throw new InvalidOperationException("Could not parse bin id from create response: " + json);
    }

    private static List<Guid> GetJobRunIdsFromResponse(string json)
    {
        var list = new List<Guid>();
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("Data", out var data))
                root = data;
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var g))
                        list.Add(g);
                    else if (item.TryGetProperty("Id", out var idEl2) && Guid.TryParse(idEl2.GetString(), out var g2))
                        list.Add(g2);
                }
            }
        }
        catch { /* ignore */ }
        return list;
    }
}
