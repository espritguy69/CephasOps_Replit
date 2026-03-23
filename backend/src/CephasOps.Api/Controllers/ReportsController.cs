using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Api.DTOs;
using CephasOps.Api.Reports;
using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Scheduler.DTOs;
using CephasOps.Application.Scheduler.Services;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Reports Hub: definitions and run. Department scope enforced on run; 403 when user has no access.
/// </summary>
[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IInventoryService _inventoryService;
    private readonly IStockLedgerService _stockLedgerService;
    private readonly ISchedulerService _schedulerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICsvService _csvService;
    private readonly IReportExportFormatService _reportExportFormatService;
    private readonly ITenantUsageService? _tenantUsageService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IOrderService orderService,
        IInventoryService inventoryService,
        IStockLedgerService stockLedgerService,
        ISchedulerService schedulerService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICsvService csvService,
        IReportExportFormatService reportExportFormatService,
        ILogger<ReportsController> logger,
        ITenantUsageService? tenantUsageService = null)
    {
        _orderService = orderService;
        _inventoryService = inventoryService;
        _stockLedgerService = stockLedgerService;
        _schedulerService = schedulerService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _csvService = csvService;
        _reportExportFormatService = reportExportFormatService;
        _tenantUsageService = tenantUsageService;
        _logger = logger;
    }

    /// <summary>Get all report definitions for the hub (search, tags, run).</summary>
    [HttpGet("definitions")]
    [RequirePermission(PermissionCatalog.ReportsView)]
    [ProducesResponseType(typeof(ApiResponse<List<ReportDefinitionHubDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<ReportDefinitionHubDto>>> GetDefinitions()
    {
        var list = ReportRegistry.GetAll().ToList();
        return this.Success(list);
    }

    /// <summary>Get a single report definition by key.</summary>
    [HttpGet("definitions/{reportKey}")]
    [RequirePermission(PermissionCatalog.ReportsView)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionHubDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<ReportDefinitionHubDto>> GetDefinition(string reportKey)
    {
        var def = ReportRegistry.GetByKey(reportKey);
        if (def == null)
            return this.NotFound<ReportDefinitionHubDto>($"Report '{reportKey}' not found");
        return this.Success(def);
    }

    /// <summary>Run a report. Department scope required; 403 if user has no access to the department.</summary>
    [HttpPost("{reportKey}/run")]
    [RequirePermission(PermissionCatalog.ReportsView)]
    [ProducesResponseType(typeof(ApiResponse<RunReportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RunReportResultDto>>> RunReport(
        string reportKey,
        [FromBody] RunReportRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var def = ReportRegistry.GetByKey(reportKey);
        if (def == null)
            return this.NotFound<RunReportResultDto>($"Report '{reportKey}' not found");

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));

        Guid? resolvedDeptId = null;
        try
        {
            var deptId = request?.DepartmentId ?? _departmentRequestContext.DepartmentId;
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(deptId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }

        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));

        var req = request ?? new RunReportRequestDto();

        try
        {
            var result = reportKey.ToLowerInvariant() switch
            {
                "orders-list" => await RunOrdersListAsync(companyId, resolvedDeptId.Value, req, cancellationToken),
                "materials-list" => await RunMaterialsListAsync(companyId, resolvedDeptId.Value, req, cancellationToken),
                "stock-summary" => await RunStockSummaryAsync(companyId, resolvedDeptId.Value, req, cancellationToken),
                "ledger" => await RunLedgerAsync(companyId, resolvedDeptId.Value, req, cancellationToken),
                "scheduler-utilization" => await RunSchedulerUtilizationAsync(companyId, resolvedDeptId.Value, req, cancellationToken),
                _ => null
            };

            if (result == null)
                return this.NotFound<RunReportResultDto>($"Report '{reportKey}' has no handler");

            return this.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running report {ReportKey}", reportKey);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to run report: {ex.Message}"));
        }
    }

    /// <summary>Export stock-summary report. Department scope required. Format: csv (default), xlsx, pdf. Tenant: companyId from current user only (no query override).</summary>
    [HttpGet("stock-summary/export")]
    [RequirePermission(PermissionCatalog.ReportsExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportStockSummary(
        [FromQuery] string? format = "csv",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? materialId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "xlsx" && normalizedFormat != "pdf")
            return BadRequest(ApiResponse.ErrorResponse("Format must be csv, xlsx, or pdf"));

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var summary = await _stockLedgerService.GetStockSummaryAsync(companyId, resolvedDeptId.Value, locationId, materialId, cancellationToken);
            var rows = summary.ByLocation;
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (companyId.HasValue && _tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);
            if (normalizedFormat == "csv")
            {
                var csvBytes = _csvService.ExportToCsvBytes(rows);
                return File(csvBytes, "text/csv", $"stock-summary-{dateStr}.csv");
            }
            if (normalizedFormat == "xlsx")
            {
                var xlsxBytes = _reportExportFormatService.ExportToExcelBytes(rows, "Stock Summary");
                return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"stock-summary-{dateStr}.xlsx");
            }
            var pdfBytes = _reportExportFormatService.ExportToPdfBytes(rows, "Stock Summary", DateTime.UtcNow);
            return File(pdfBytes, "application/pdf", $"stock-summary-{dateStr}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting stock-summary report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Export failed: {ex.Message}"));
        }
    }

    /// <summary>Export orders-list report. Department scope required. Exports up to 10000 rows. Format: csv (default), xlsx, pdf.</summary>
    [HttpGet("orders-list/export")]
    [RequirePermission(PermissionCatalog.ReportsExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportOrdersList(
        [FromQuery] string? format = "csv",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? assignedSiId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "xlsx" && normalizedFormat != "pdf")
            return BadRequest(ApiResponse.ErrorResponse("Format must be csv, xlsx, or pdf"));

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            const int exportPageSize = 10_000;
            var paged = await _orderService.GetOrdersPagedAsync(
                companyId, resolvedDeptId.Value, status, null, assignedSiId, null, fromDate, toDate,
                keyword, 1, exportPageSize, cancellationToken);
            var rows = paged.Items;
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (companyId.HasValue && _tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);
            if (normalizedFormat == "csv")
            {
                var csvBytes = _csvService.ExportToCsvBytes(rows);
                return File(csvBytes, "text/csv", $"orders-list-{dateStr}.csv");
            }
            if (normalizedFormat == "xlsx")
            {
                var xlsxBytes = _reportExportFormatService.ExportToExcelBytes(rows, "Orders List");
                return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"orders-list-{dateStr}.xlsx");
            }
            var pdfBytes = _reportExportFormatService.ExportToPdfBytes(rows, "Orders List", DateTime.UtcNow);
            return File(pdfBytes, "application/pdf", $"orders-list-{dateStr}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders-list report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Export failed: {ex.Message}"));
        }
    }

    /// <summary>Export ledger report. Department scope required. Exports up to 10000 rows. Format: csv (default), xlsx, pdf.</summary>
    [HttpGet("ledger/export")]
    [RequirePermission(PermissionCatalog.ReportsExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportLedger(
        [FromQuery] string? format = "csv",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] string? entryType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "xlsx" && normalizedFormat != "pdf")
            return BadRequest(ApiResponse.ErrorResponse("Format must be csv, xlsx, or pdf"));

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            const int exportPageSize = 10_000;
            var filter = new LedgerFilterDto
            {
                MaterialId = materialId,
                LocationId = locationId,
                OrderId = orderId,
                EntryType = entryType,
                FromDate = fromDate,
                ToDate = toDate,
                Page = 1,
                PageSize = exportPageSize
            };
            var result = await _stockLedgerService.GetLedgerAsync(filter, companyId, resolvedDeptId.Value, cancellationToken);
            var rows = result.Items;
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (companyId.HasValue && _tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);
            if (normalizedFormat == "csv")
            {
                var csvBytes = _csvService.ExportToCsvBytes(rows);
                return File(csvBytes, "text/csv", $"ledger-{dateStr}.csv");
            }
            if (normalizedFormat == "xlsx")
            {
                var xlsxBytes = _reportExportFormatService.ExportToExcelBytes(rows, "Ledger");
                return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ledger-{dateStr}.xlsx");
            }
            var pdfBytes = _reportExportFormatService.ExportToPdfBytes(rows, "Ledger Report", DateTime.UtcNow);
            return File(pdfBytes, "application/pdf", $"ledger-{dateStr}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting ledger report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Export failed: {ex.Message}"));
        }
    }

    /// <summary>Export materials-list report. Department scope required. Format: csv (default), xlsx, pdf.</summary>
    [HttpGet("materials-list/export")]
    [RequirePermission(PermissionCatalog.ReportsExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMaterialsList(
        [FromQuery] string? format = "csv",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "xlsx" && normalizedFormat != "pdf")
            return BadRequest(ApiResponse.ErrorResponse("Format must be csv, xlsx, or pdf"));

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var materials = await _inventoryService.GetMaterialsAsync(companyId, resolvedDeptId.Value, category, null, isActive, cancellationToken);
            var rows = materials.Select(m => new MaterialCsvDto
            {
                Code = m.ItemCode ?? string.Empty,
                Description = m.Description ?? string.Empty,
                CategoryName = m.Category ?? string.Empty,
                UnitOfMeasure = m.UnitOfMeasure ?? string.Empty,
                UnitCost = m.DefaultCost ?? 0,
                IsSerialised = m.IsSerialised,
                MinStockLevel = null,
                ReorderPoint = null,
                IsActive = m.IsActive
            }).ToList();
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (companyId.HasValue && _tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);
            if (normalizedFormat == "csv")
            {
                var csvBytes = _csvService.ExportToCsvBytes(rows);
                return File(csvBytes, "text/csv", $"materials-{dateStr}.csv");
            }
            if (normalizedFormat == "xlsx")
            {
                var xlsxBytes = _reportExportFormatService.ExportToExcelBytes(rows, "Materials");
                return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"materials-{dateStr}.xlsx");
            }
            var pdfBytes = _reportExportFormatService.ExportToPdfBytes(rows, "Materials List", DateTime.UtcNow);
            return File(pdfBytes, "application/pdf", $"materials-{dateStr}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting materials-list report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Export failed: {ex.Message}"));
        }
    }

    /// <summary>Export scheduler-utilization report. Department scope required. Format: csv (default), xlsx, pdf.</summary>
    [HttpGet("scheduler-utilization/export")]
    [RequirePermission(PermissionCatalog.ReportsExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportSchedulerUtilization(
        [FromQuery] string? format = "csv",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? siId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "xlsx" && normalizedFormat != "pdf")
            return BadRequest(ApiResponse.ErrorResponse("Format must be csv, xlsx, or pdf"));
        if (!fromDate.HasValue || !toDate.HasValue)
            return BadRequest(ApiResponse.ErrorResponse("FromDate and ToDate are required"));

        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null && !_currentUserService.IsSuperAdmin)
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        Guid? resolvedDeptId = null;
        try
        {
            resolvedDeptId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }
        if (resolvedDeptId == null)
            return StatusCode(403, ApiResponse.ErrorResponse("Department scope required"));
        try
        {
            var calendar = await _schedulerService.GetCalendarAsync(companyId, fromDate.Value, toDate.Value, resolvedDeptId.Value, cancellationToken);
            var slots = calendar.SelectMany(c => c.Slots).ToList();
            if (siId.HasValue)
                slots = slots.Where(s => s.ServiceInstallerId == siId.Value).ToList();
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (companyId.HasValue && _tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);
            if (normalizedFormat == "csv")
            {
                var csvBytes = _csvService.ExportToCsvBytes(slots);
                return File(csvBytes, "text/csv", $"scheduler-utilization-{dateStr}.csv");
            }
            if (normalizedFormat == "xlsx")
            {
                var xlsxBytes = _reportExportFormatService.ExportToExcelBytes(slots, "Scheduler Utilization");
                return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"scheduler-utilization-{dateStr}.xlsx");
            }
            var pdfBytes = _reportExportFormatService.ExportToPdfBytes(slots, "Scheduler Utilization", DateTime.UtcNow);
            return File(pdfBytes, "application/pdf", $"scheduler-utilization-{dateStr}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting scheduler-utilization report");
            return StatusCode(500, ApiResponse.ErrorResponse($"Export failed: {ex.Message}"));
        }
    }

    private async Task<RunReportResultDto> RunOrdersListAsync(Guid? companyId, Guid departmentId, RunReportRequestDto req, CancellationToken ct)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 50 : Math.Min(req.PageSize, 500);
        var paged = await _orderService.GetOrdersPagedAsync(
            companyId, departmentId, req.Status, null, req.AssignedSiId, null, req.FromDate, req.ToDate,
            req.Keyword, page, pageSize, ct);
        var items = paged.Items.Cast<object>().ToList();
        return new RunReportResultDto { Items = items, TotalCount = paged.TotalCount, Page = paged.Page, PageSize = paged.PageSize };
    }

    private async Task<RunReportResultDto> RunMaterialsListAsync(Guid? companyId, Guid departmentId, RunReportRequestDto req, CancellationToken ct)
    {
        var materials = await _inventoryService.GetMaterialsAsync(
            companyId, departmentId, req.Category, req.Search, req.IsActive, ct);
        var list = materials.AsEnumerable();
        if (req.IsSerialised == true)
            list = list.Where(m => m.IsSerialised);
        var items = list.Cast<object>().ToList();
        return new RunReportResultDto { Items = items, TotalCount = items.Count };
    }

    private async Task<RunReportResultDto> RunStockSummaryAsync(Guid? companyId, Guid departmentId, RunReportRequestDto req, CancellationToken ct)
    {
        var summary = await _stockLedgerService.GetStockSummaryAsync(
            companyId, departmentId, req.LocationId, req.MaterialId, ct);
        var items = summary.ByLocation.Cast<object>().ToList();
        return new RunReportResultDto { Items = items, TotalCount = items.Count };
    }

    private async Task<RunReportResultDto> RunLedgerAsync(Guid? companyId, Guid departmentId, RunReportRequestDto req, CancellationToken ct)
    {
        var filter = new LedgerFilterDto
        {
            MaterialId = req.MaterialId,
            LocationId = req.LocationId,
            OrderId = req.OrderId,
            EntryType = req.EntryType,
            FromDate = req.FromDate,
            ToDate = req.ToDate,
            Page = req.Page,
            PageSize = req.PageSize
        };
        var result = await _stockLedgerService.GetLedgerAsync(filter, companyId, departmentId, ct);
        var items = result.Items.Cast<object>().ToList();
        return new RunReportResultDto
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    private async Task<RunReportResultDto> RunSchedulerUtilizationAsync(Guid? companyId, Guid departmentId, RunReportRequestDto req, CancellationToken ct)
    {
        if (!req.FromDate.HasValue || !req.ToDate.HasValue)
            throw new ArgumentException("FromDate and ToDate are required for scheduler utilization report.");
        var calendar = await _schedulerService.GetCalendarAsync(companyId, req.FromDate.Value, req.ToDate.Value, departmentId, ct);
        var slots = calendar.SelectMany(c => c.Slots).ToList();
        if (req.SiId.HasValue)
            slots = slots.Where(s => s.ServiceInstallerId == req.SiId.Value).ToList();
        var items = slots.Cast<object>().ToList();
        return new RunReportResultDto { Items = items, TotalCount = items.Count };
    }
}
