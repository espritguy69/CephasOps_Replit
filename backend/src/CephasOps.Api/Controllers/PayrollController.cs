using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Payroll.DTOs;
using CephasOps.Application.Payroll.Services;
using CephasOps.Domain.Authorization;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Payroll management endpoints
/// </summary>
[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICsvService _csvService;
    private readonly ApplicationDbContext _context;
    private readonly IFieldLevelSecurityFilter _fieldLevelSecurity;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollService payrollService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICsvService csvService,
        ApplicationDbContext context,
        IFieldLevelSecurityFilter fieldLevelSecurity,
        ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _csvService = csvService;
        _context = context;
        _fieldLevelSecurity = fieldLevelSecurity;
        _logger = logger;
    }

    /// <summary>
    /// Get payroll periods
    /// </summary>
    [HttpGet("periods")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollPeriodDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollPeriodDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollPeriodDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PayrollPeriodDto>>>> GetPayrollPeriods(
        [FromQuery] string? year = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var periods = await _payrollService.GetPayrollPeriodsAsync(companyId, year, status, cancellationToken);
            return this.Success(periods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll periods");
            return this.Error<List<PayrollPeriodDto>>($"Failed to get payroll periods: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get payroll period by ID
    /// </summary>
    [HttpGet("periods/{id}")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> GetPayrollPeriod(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var period = await _payrollService.GetPayrollPeriodByIdAsync(id, companyId, cancellationToken);
            if (period == null)
            {
                return this.NotFound<PayrollPeriodDto>($"Payroll period with ID {id} not found");
            }

            return this.Success(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll period: {PeriodId}", id);
            return this.Error<PayrollPeriodDto>($"Failed to get payroll period: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create payroll period
    /// </summary>
    [HttpPost("periods")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollPeriodDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> CreatePayrollPeriod(
        [FromBody] CreatePayrollPeriodDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<PayrollPeriodDto>("User context required");
        }

        try
        {
            var period = await _payrollService.CreatePayrollPeriodAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<PayrollPeriodDto>.SuccessResponse(period, "Payroll period created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return this.Error<PayrollPeriodDto>($"Failed to create payroll period: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get payroll runs
    /// </summary>
    [HttpGet("runs")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollRunDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollRunDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollRunDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PayrollRunDto>>>> GetPayrollRuns(
        [FromQuery] Guid? periodId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var runs = await _payrollService.GetPayrollRunsAsync(companyId, periodId, status, fromDate, toDate, cancellationToken);
            await _fieldLevelSecurity.ApplyPayrollRunDtosAsync(runs, cancellationToken);
            return this.Success(runs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll runs");
            return this.Error<List<PayrollRunDto>>($"Failed to get payroll runs: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get payroll run by ID
    /// </summary>
    [HttpGet("runs/{id}")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollRunDto>>> GetPayrollRun(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var run = await _payrollService.GetPayrollRunByIdAsync(id, companyId, cancellationToken);
            if (run == null)
            {
                return this.NotFound<PayrollRunDto>($"Payroll run with ID {id} not found");
            }

            await _fieldLevelSecurity.ApplyPayrollRunDtoAsync(run, cancellationToken);
            return this.Success(run);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll run: {RunId}", id);
            return this.Error<PayrollRunDto>($"Failed to get payroll run: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create payroll run
    /// </summary>
    [HttpPost("runs")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollRunDto>>> CreatePayrollRun(
        [FromBody] CreatePayrollRunDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<PayrollRunDto>("User context required");
        }

        try
        {
            var run = await _payrollService.CreatePayrollRunAsync(dto, companyId, userId.Value, cancellationToken);
            await _fieldLevelSecurity.ApplyPayrollRunDtoAsync(run, cancellationToken);
            return this.StatusCode(201, ApiResponse<PayrollRunDto>.SuccessResponse(run, "Payroll run created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll run");
            return this.Error<PayrollRunDto>($"Failed to create payroll run: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Finalize payroll run
    /// </summary>
    [HttpPost("runs/{id}/finalize")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollRunDto>>> FinalizePayrollRun(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var run = await _payrollService.FinalizePayrollRunAsync(id, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyPayrollRunDtoAsync(run, cancellationToken);
            return this.Success(run, "Payroll run finalized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing payroll run: {RunId}", id);
            return this.Error<PayrollRunDto>($"Failed to finalize payroll run: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Mark payroll run as paid
    /// </summary>
    [HttpPost("runs/{id}/mark-paid")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PayrollRunDto>>> MarkPayrollRunPaid(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var run = await _payrollService.MarkPayrollRunPaidAsync(id, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplyPayrollRunDtoAsync(run, cancellationToken);
            return this.Success(run, "Payroll run marked as paid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payroll run as paid: {RunId}", id);
            return this.Error<PayrollRunDto>($"Failed to mark payroll run as paid: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get job earning records
    /// </summary>
    [HttpGet("earnings")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<List<JobEarningRecordDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<JobEarningRecordDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<JobEarningRecordDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<JobEarningRecordDto>>>> GetJobEarningRecords(
        [FromQuery] Guid? siId = null,
        [FromQuery] string? period = null,
        [FromQuery] Guid? orderId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var records = await _payrollService.GetJobEarningRecordsAsync(companyId, siId, period, orderId, cancellationToken);
            await _fieldLevelSecurity.ApplyJobEarningRecordDtosAsync(records, cancellationToken);
            return this.Success(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job earning records");
            return this.Error<List<JobEarningRecordDto>>($"Failed to get job earning records: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get SI rate plans
    /// </summary>
    [HttpGet("si-rate-plans")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<List<SiRatePlanDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SiRatePlanDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<SiRatePlanDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SiRatePlanDto>>>> GetSiRatePlans(
        [FromQuery] Guid? siId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var (departmentScope, scopeError) = await this.ResolveDepartmentScopeOrFailAsync(
            _departmentAccessService, _departmentRequestContext, departmentId, cancellationToken);
        if (scopeError != null) return scopeError;

        try
        {
            var plans = await _payrollService.GetSiRatePlansAsync(companyId, siId, departmentScope, isActive, cancellationToken);
            await _fieldLevelSecurity.ApplySiRatePlanDtosAsync(plans, cancellationToken);
            return this.Success(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SI rate plans");
            return this.Error<List<SiRatePlanDto>>($"Failed to get SI rate plans: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get SI rate plan by ID
    /// </summary>
    [HttpGet("si-rate-plans/{id}")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SiRatePlanDto>>> GetSiRatePlan(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var plan = await _payrollService.GetSiRatePlanByIdAsync(id, companyId, cancellationToken);
            if (plan == null)
            {
                return this.NotFound<SiRatePlanDto>($"SI rate plan with ID {id} not found");
            }

            await _fieldLevelSecurity.ApplySiRatePlanDtoAsync(plan, cancellationToken);
            return this.Success(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SI rate plan: {PlanId}", id);
            return this.Error<SiRatePlanDto>($"Failed to get SI rate plan: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create SI rate plan
    /// </summary>
    [HttpPost("si-rate-plans")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SiRatePlanDto>>> CreateSiRatePlan(
        [FromBody] CreateSiRatePlanDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var plan = await _payrollService.CreateSiRatePlanAsync(dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplySiRatePlanDtoAsync(plan, cancellationToken);
            return this.StatusCode(201, ApiResponse<SiRatePlanDto>.SuccessResponse(plan, "SI rate plan created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SI rate plan");
            return this.Error<SiRatePlanDto>($"Failed to create SI rate plan: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update SI rate plan
    /// </summary>
    [HttpPut("si-rate-plans/{id}")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SiRatePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SiRatePlanDto>>> UpdateSiRatePlan(
        Guid id,
        [FromBody] CreateSiRatePlanDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var plan = await _payrollService.UpdateSiRatePlanAsync(id, dto, companyId, cancellationToken);
            await _fieldLevelSecurity.ApplySiRatePlanDtoAsync(plan, cancellationToken);
            return this.Success(plan, "SI rate plan updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<SiRatePlanDto>($"SI rate plan with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SI rate plan: {PlanId}", id);
            return this.Error<SiRatePlanDto>($"Failed to update SI rate plan: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete SI rate plan
    /// </summary>
    [HttpDelete("si-rate-plans/{id}")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSiRatePlan(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _payrollService.DeleteSiRatePlanAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("SI rate plan deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"SI rate plan with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SI rate plan: {PlanId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete SI rate plan: {ex.Message}"));
        }
    }

    // ============================================
    // SI Rate Plans - Import/Export
    // ============================================

    /// <summary>
    /// Export SI rate plans to CSV
    /// </summary>
    [HttpGet("si-rate-plans/export")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSiRatePlans(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return Unauthorized("Company context required");
        }

        var (departmentScope, scopeError) = await this.ResolveDepartmentScopeOrFailAsync(
            _departmentAccessService, _departmentRequestContext, departmentId, cancellationToken);
        if (scopeError != null) return scopeError;

        try
        {
            var plans = await _payrollService.GetSiRatePlansAsync(companyId.Value, null, departmentScope, isActive, cancellationToken);
            
            var csvData = plans.Select(p => new SiRatePlanCsvDto
            {
                ServiceInstallerName = p.ServiceInstallerName,
                ServiceInstallerCode = string.Empty, // Would need to add to DTO
                DepartmentName = p.DepartmentName ?? string.Empty,
                InstallationMethodName = p.InstallationMethodName ?? string.Empty,
                RateType = p.RateType,
                Level = p.Level,
                PrelaidRate = p.PrelaidRate,
                NonPrelaidRate = p.NonPrelaidRate,
                SduRate = p.SduRate,
                RdfPoleRate = p.RdfPoleRate,
                ActivationRate = p.ActivationRate,
                ModificationRate = p.ModificationRate,
                AssuranceRate = p.AssuranceRate,
                AssuranceRepullRate = p.AssuranceRepullRate,
                FttrRate = p.FttrRate,
                FttcRate = p.FttcRate,
                OnTimeBonus = p.OnTimeBonus,
                LatePenalty = p.LatePenalty,
                ReworkRate = p.ReworkRate,
                IsActive = p.IsActive
            }).ToList();

            var csvBytes = _csvService.ExportToCsvBytes(csvData);
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(csvBytes, "text/csv", $"si-rate-plans-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting SI rate plans");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export SI rate plans: {ex.Message}"));
        }
    }

    /// <summary>
    /// Download SI rate plans CSV template
    /// </summary>
    [HttpGet("si-rate-plans/template")]
    [RequirePermission(PermissionCatalog.PayrollView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetSiRatePlansTemplate()
    {
        try
        {
            var templateBytes = _csvService.GenerateTemplateBytes<SiRatePlanCsvDto>();
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(templateBytes, "text/csv", "si-rate-plans-template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SI rate plans template");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Import SI rate plans from CSV
    /// </summary>
    [HttpPost("si-rate-plans/import")]
    [RequirePermission(PermissionCatalog.PayrollRun)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<SiRatePlanCsvDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<SiRatePlanCsvDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<SiRatePlanCsvDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<SiRatePlanCsvDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ImportResult<SiRatePlanCsvDto>>>> ImportSiRatePlans(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        if (file == null || file.Length == 0)
        {
            return this.Error<ImportResult<SiRatePlanCsvDto>>("No file uploaded", 400);
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = _csvService.ImportFromCsv<SiRatePlanCsvDto>(stream);

            var result = new ImportResult<SiRatePlanCsvDto>
            {
                TotalRows = records.Count
            };

            // Pre-load reference data for lookups
            if (companyId == Guid.Empty)
                return BadRequest(ApiResponse.ErrorResponse("Tenant context (companyId) is required for payroll import."));
            var serviceInstallers = await _context.ServiceInstallers
                .Where(si => si.CompanyId == companyId && si.IsActive)
                .ToListAsync(cancellationToken);
            var siLookup = serviceInstallers.ToDictionary(si => si.Name, StringComparer.OrdinalIgnoreCase);

            var departments = await _context.Departments
                .Where(d => d.CompanyId == companyId && d.IsActive)
                .ToListAsync(cancellationToken);
            var departmentLookup = departments.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

            var installationMethods = await _context.InstallationMethods
                .Where(im => im.CompanyId == companyId && im.IsActive)
                .ToListAsync(cancellationToken);
            var installationMethodLookup = installationMethods.ToDictionary(im => im.Name, StringComparer.OrdinalIgnoreCase);

            // Process each record
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2))) // +2 for header row
            {
                try
                {
                    // Resolve service installer by name or code
                    Domain.ServiceInstallers.Entities.ServiceInstaller? serviceInstaller = null;
                    if (!string.IsNullOrWhiteSpace(record.ServiceInstallerName))
                    {
                        siLookup.TryGetValue(record.ServiceInstallerName.Trim(), out serviceInstaller);
                    }
                    if (serviceInstaller == null && !string.IsNullOrWhiteSpace(record.ServiceInstallerCode))
                    {
                        siLookup.TryGetValue(record.ServiceInstallerCode.Trim(), out serviceInstaller);
                    }
                    if (serviceInstaller == null)
                    {
                        throw new InvalidOperationException($"Service installer '{record.ServiceInstallerName}' not found");
                    }

                    // Resolve department by name
                    Guid? departmentId = null;
                    if (!string.IsNullOrWhiteSpace(record.DepartmentName))
                    {
                        if (departmentLookup.TryGetValue(record.DepartmentName.Trim(), out var department))
                        {
                            departmentId = department.Id;
                        }
                        else
                        {
                            _logger.LogWarning("Department '{DepartmentName}' not found for SI rate plan. Skipping department assignment.", 
                                record.DepartmentName);
                        }
                    }

                    // Resolve installation method by name
                    Guid? installationMethodId = null;
                    if (!string.IsNullOrWhiteSpace(record.InstallationMethodName))
                    {
                        if (installationMethodLookup.TryGetValue(record.InstallationMethodName.Trim(), out var installationMethod))
                        {
                            installationMethodId = installationMethod.Id;
                        }
                        else
                        {
                            _logger.LogWarning("Installation method '{InstallationMethodName}' not found for SI rate plan. Skipping installation method assignment.", 
                                record.InstallationMethodName);
                        }
                    }

                    // Map CSV DTO to CreateSiRatePlanDto
                    var createDto = new CreateSiRatePlanDto
                    {
                        DepartmentId = departmentId,
                        ServiceInstallerId = serviceInstaller.Id,
                        InstallationMethodId = installationMethodId,
                        RateType = string.IsNullOrWhiteSpace(record.RateType) ? "Junior" : record.RateType.Trim(),
                        Level = string.IsNullOrWhiteSpace(record.Level) ? "Junior" : record.Level.Trim(),
                        PrelaidRate = record.PrelaidRate,
                        NonPrelaidRate = record.NonPrelaidRate,
                        SduRate = record.SduRate,
                        RdfPoleRate = record.RdfPoleRate,
                        ActivationRate = record.ActivationRate,
                        ModificationRate = record.ModificationRate,
                        AssuranceRate = record.AssuranceRate,
                        AssuranceRepullRate = record.AssuranceRepullRate,
                        FttrRate = record.FttrRate,
                        FttcRate = record.FttcRate,
                        OnTimeBonus = record.OnTimeBonus,
                        LatePenalty = record.LatePenalty,
                        ReworkRate = record.ReworkRate
                    };

                    // Create rate plan
                    await _payrollService.CreateSiRatePlanAsync(createDto, companyId, cancellationToken);
                    
                    result.SuccessCount++;
                    result.ImportedRecords.Add(record);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = ex.Message
                    });
                    _logger.LogWarning(ex, "Error importing SI rate plan at row {RowNumber}: {ServiceInstallerName}", index, record.ServiceInstallerName);
                }
            }

            return this.Success(result, "SI rate plans imported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing SI rate plans");
            return this.Error<ImportResult<SiRatePlanCsvDto>>($"Failed to import SI rate plans: {ex.Message}", 500);
        }
    }
}

