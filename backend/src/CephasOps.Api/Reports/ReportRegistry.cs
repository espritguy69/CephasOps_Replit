using CephasOps.Api.DTOs;

namespace CephasOps.Api.Reports;

/// <summary>
/// In-memory registry of runnable reports for the Reports Hub. Drives /api/reports/definitions.
/// </summary>
public static class ReportRegistry
{
    public static IReadOnlyList<ReportDefinitionHubDto> GetAll()
    {
        return new List<ReportDefinitionHubDto>
        {
            new()
            {
                ReportKey = "orders-list",
                Name = "Orders list",
                Description = "Search and filter orders by department, date range, status, installer, appointment date.",
                Tags = new List<string> { "operations", "orders", "list" },
                Category = "Operations",
                SupportsExport = true,
                ParameterSchema = new List<ReportParameterSchemaDto>
                {
                    new() { Name = "departmentId", Type = "guid", Required = true, Label = "Department" },
                    new() { Name = "status", Type = "string", Required = false, Label = "Status" },
                    new() { Name = "fromDate", Type = "datetime", Required = false, Label = "From date" },
                    new() { Name = "toDate", Type = "datetime", Required = false, Label = "To date" },
                    new() { Name = "assignedSiId", Type = "guid", Required = false, Label = "Installer" },
                    new() { Name = "keyword", Type = "string", Required = false, Label = "Keyword" },
                    new() { Name = "page", Type = "int", Required = false, Label = "Page" },
                    new() { Name = "pageSize", Type = "int", Required = false, Label = "Page size" }
                }
            },
            new()
            {
                ReportKey = "materials-list",
                Name = "Materials list",
                Description = "List materials by department, category, serialised/non-serial.",
                Tags = new List<string> { "inventory", "materials", "list" },
                Category = "Inventory",
                SupportsExport = true,
                ParameterSchema = new List<ReportParameterSchemaDto>
                {
                    new() { Name = "departmentId", Type = "guid", Required = true, Label = "Department" },
                    new() { Name = "search", Type = "string", Required = false, Label = "Search" },
                    new() { Name = "category", Type = "string", Required = false, Label = "Category" },
                    new() { Name = "isSerialised", Type = "bool", Required = false, Label = "Serialised only" },
                    new() { Name = "isActive", Type = "bool", Required = false, Label = "Active only" }
                }
            },
            new()
            {
                ReportKey = "stock-summary",
                Name = "Stock summary",
                Description = "Ledger-based on-hand, reserved, and available by material and location.",
                Tags = new List<string> { "inventory", "stock", "ledger" },
                Category = "Inventory",
                SupportsExport = true,
                ParameterSchema = new List<ReportParameterSchemaDto>
                {
                    new() { Name = "departmentId", Type = "guid", Required = true, Label = "Department" },
                    new() { Name = "locationId", Type = "guid", Required = false, Label = "Location" },
                    new() { Name = "materialId", Type = "guid", Required = false, Label = "Material" }
                }
            },
            new()
            {
                ReportKey = "ledger",
                Name = "Ledger report",
                Description = "Stock ledger entries: receive, transfer, allocate, issue, return with date range and filters.",
                Tags = new List<string> { "inventory", "ledger", "movements" },
                Category = "Inventory",
                SupportsExport = true,
                ParameterSchema = new List<ReportParameterSchemaDto>
                {
                    new() { Name = "departmentId", Type = "guid", Required = true, Label = "Department" },
                    new() { Name = "materialId", Type = "guid", Required = false, Label = "Material" },
                    new() { Name = "locationId", Type = "guid", Required = false, Label = "Location" },
                    new() { Name = "orderId", Type = "guid", Required = false, Label = "Order" },
                    new() { Name = "entryType", Type = "string", Required = false, Label = "Entry type" },
                    new() { Name = "fromDate", Type = "datetime", Required = false, Label = "From date" },
                    new() { Name = "toDate", Type = "datetime", Required = false, Label = "To date" },
                    new() { Name = "page", Type = "int", Required = false, Label = "Page" },
                    new() { Name = "pageSize", Type = "int", Required = false, Label = "Page size" }
                }
            },
            new()
            {
                ReportKey = "scheduler-utilization",
                Name = "Scheduler utilization",
                Description = "Schedule slots and assignments by installer and date range.",
                Tags = new List<string> { "operations", "scheduler", "utilization" },
                Category = "Operations",
                SupportsExport = true,
                ParameterSchema = new List<ReportParameterSchemaDto>
                {
                    new() { Name = "departmentId", Type = "guid", Required = true, Label = "Department" },
                    new() { Name = "fromDate", Type = "datetime", Required = true, Label = "From date" },
                    new() { Name = "toDate", Type = "datetime", Required = true, Label = "To date" },
                    new() { Name = "siId", Type = "guid", Required = false, Label = "Installer" }
                }
            }
        };
    }

    public static ReportDefinitionHubDto? GetByKey(string reportKey)
    {
        return GetAll().FirstOrDefault(r => string.Equals(r.ReportKey, reportKey, StringComparison.OrdinalIgnoreCase));
    }
}
