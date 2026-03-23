using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Departments.Services;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Billing ratecard endpoints
/// </summary>
[ApiController]
[Route("api/billing/ratecards")]
[Authorize]
public class BillingRatecardController : ControllerBase
{
    private readonly IBillingRatecardService _ratecardService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICsvService _csvService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BillingRatecardController> _logger;

    public BillingRatecardController(
        IBillingRatecardService ratecardService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICsvService csvService,
        ApplicationDbContext context,
        ILogger<BillingRatecardController> logger)
    {
        _ratecardService = ratecardService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _csvService = csvService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all billing ratecards for the current company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BillingRatecardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BillingRatecardDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BillingRatecardDto>>>> GetBillingRatecards(
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? orderTypeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? serviceCategory = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }

        try
        {
            var ratecards = await _ratecardService.GetBillingRatecardsAsync(
                companyId, partnerId, orderTypeId, departmentScope, serviceCategory, installationMethodId, isActive, cancellationToken);
            return this.Success(ratecards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing ratecards");
            return this.InternalServerError<List<BillingRatecardDto>>($"Failed to get billing ratecards: {ex.Message}");
        }
    }

    /// <summary>
    /// Get billing ratecard by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BillingRatecardDto>>> GetBillingRatecard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var ratecard = await _ratecardService.GetBillingRatecardByIdAsync(id, companyId, cancellationToken);
            if (ratecard == null)
            {
                return this.NotFound<BillingRatecardDto>($"Billing ratecard with ID {id} not found");
            }

            return this.Success(ratecard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing ratecard: {RatecardId}", id);
            return this.InternalServerError<BillingRatecardDto>($"Failed to get billing ratecard: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new billing ratecard
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BillingRatecardDto>>> CreateBillingRatecard(
        [FromBody] CreateBillingRatecardDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var ratecard = await _ratecardService.CreateBillingRatecardAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetBillingRatecard), new { id = ratecard.Id }, ratecard, "Billing ratecard created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BillingRatecardDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing ratecard");
            return this.InternalServerError<BillingRatecardDto>($"Failed to create billing ratecard: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing billing ratecard
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BillingRatecardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BillingRatecardDto>>> UpdateBillingRatecard(
        Guid id,
        [FromBody] UpdateBillingRatecardDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var ratecard = await _ratecardService.UpdateBillingRatecardAsync(id, dto, companyId, cancellationToken);
            return this.Success(ratecard, "Billing ratecard updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<BillingRatecardDto>($"Billing ratecard with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating billing ratecard: {RatecardId}", id);
            return this.InternalServerError<BillingRatecardDto>($"Failed to update billing ratecard: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a billing ratecard
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBillingRatecard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _ratecardService.DeleteBillingRatecardAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Billing ratecard with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting billing ratecard: {RatecardId}", id);
            return this.InternalServerError($"Failed to delete billing ratecard: {ex.Message}");
        }
    }

    // ============================================
    // Partner Rates - Import/Export
    // ============================================

    /// <summary>
    /// Export partner rates to CSV
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPartnerRates(
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == null)
        {
            return Unauthorized("Company context required");
        }

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "You do not have access to this department" });
        }

        try
        {
            var rates = await _ratecardService.GetBillingRatecardsAsync(
                companyId.Value, partnerId, null, departmentScope, null, null, isActive, cancellationToken);
            
            var csvData = rates.Select(r => new PartnerRateCsvDto
            {
                PartnerName = r.PartnerName ?? "",
                PartnerCode = "",
                DepartmentName = r.DepartmentName ?? "",
                OrderTypeName = r.OrderTypeName ?? "",
                ServiceCategory = r.ServiceCategory ?? "",
                InstallationMethodName = r.InstallationMethodName ?? "",
                Description = r.Description ?? "",
                Amount = r.Amount,
                TaxRate = r.TaxRate,
                IsActive = r.IsActive
            }).ToList();

            var csvBytes = _csvService.ExportToCsvBytes(csvData);
            return File(csvBytes, "text/csv", $"partner-rates-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting partner rates");
            return StatusCode(500, new { error = "Failed to export partner rates", message = ex.Message });
        }
    }

    /// <summary>
    /// Download partner rates CSV template
    /// </summary>
    [HttpGet("template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetPartnerRatesTemplate()
    {
        try
        {
            var templateBytes = _csvService.GenerateTemplateBytes<PartnerRateCsvDto>();
            return File(templateBytes, "text/csv", "partner-rates-template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating partner rates template");
            return StatusCode(500, new { error = "Failed to generate template", message = ex.Message });
        }
    }

    /// <summary>
    /// Import partner rates from CSV
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<PartnerRateCsvDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<PartnerRateCsvDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<PartnerRateCsvDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ImportResult<PartnerRateCsvDto>>>> ImportPartnerRates(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        if (file == null || file.Length == 0)
        {
            return this.BadRequest<ImportResult<PartnerRateCsvDto>>("No file uploaded");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = _csvService.ImportFromCsv<PartnerRateCsvDto>(stream);

            var result = new ImportResult<PartnerRateCsvDto>
            {
                TotalRows = records.Count
            };

            // Pre-load reference data for lookups (tenant-scoped; companyId from RequireCompanyId)
            var partners = await _context.Partners
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .ToListAsync(cancellationToken);
            var partnerLookup = partners.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var departments = await _context.Departments
                .Where(d => d.CompanyId == companyId && d.IsActive)
                .ToListAsync(cancellationToken);
            var departmentLookup = departments.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

            var orderTypes = await _context.OrderTypes
                .Where(ot => ot.CompanyId == companyId && ot.IsActive)
                .ToListAsync(cancellationToken);
            var orderTypeLookup = orderTypes.ToDictionary(ot => ot.Name, StringComparer.OrdinalIgnoreCase);

            var installationMethods = await _context.InstallationMethods
                .Where(im => im.CompanyId == companyId && im.IsActive)
                .ToListAsync(cancellationToken);
            var installationMethodLookup = installationMethods.ToDictionary(im => im.Name, StringComparer.OrdinalIgnoreCase);

            // Process each record
            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                try
                {
                    // Resolve partner by name or code
                    Domain.Companies.Entities.Partner? partner = null;
                    if (!string.IsNullOrWhiteSpace(record.PartnerName))
                    {
                        partnerLookup.TryGetValue(record.PartnerName.Trim(), out partner);
                    }
                    if (partner == null && !string.IsNullOrWhiteSpace(record.PartnerCode))
                    {
                        partnerLookup.TryGetValue(record.PartnerCode.Trim(), out partner);
                    }
                    if (partner == null)
                    {
                        throw new InvalidOperationException($"Partner '{record.PartnerName}' not found");
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
                            _logger.LogWarning("Department '{DepartmentName}' not found for partner rate. Skipping department assignment.", 
                                record.DepartmentName);
                        }
                    }

                    // Resolve order type by name
                    Guid? orderTypeId = null;
                    if (!string.IsNullOrWhiteSpace(record.OrderTypeName))
                    {
                        if (orderTypeLookup.TryGetValue(record.OrderTypeName.Trim(), out var orderType))
                        {
                            orderTypeId = orderType.Id;
                        }
                        else
                        {
                            _logger.LogWarning("Order type '{OrderTypeName}' not found for partner rate. Skipping order type assignment.", 
                                record.OrderTypeName);
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
                            _logger.LogWarning("Installation method '{InstallationMethodName}' not found for partner rate. Skipping installation method assignment.", 
                                record.InstallationMethodName);
                        }
                    }

                    // Map CSV DTO to CreateBillingRatecardDto
                    var createDto = new CreateBillingRatecardDto
                    {
                        DepartmentId = departmentId,
                        PartnerId = partner.Id,
                        OrderTypeId = orderTypeId,
                        ServiceCategory = string.IsNullOrWhiteSpace(record.ServiceCategory) ? null : record.ServiceCategory.Trim(),
                        InstallationMethodId = installationMethodId,
                        Description = string.IsNullOrWhiteSpace(record.Description) ? null : record.Description.Trim(),
                        Amount = record.Amount,
                        TaxRate = record.TaxRate,
                        IsActive = record.IsActive
                    };

                    // Create rate card
                    await _ratecardService.CreateBillingRatecardAsync(createDto, companyId, cancellationToken);
                    
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
                    _logger.LogWarning(ex, "Error importing partner rate at row {RowNumber}: {PartnerName}", index, record.PartnerName);
                }
            }

            return this.Success(result, "Partner rates imported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing partner rates");
            return this.InternalServerError<ImportResult<PartnerRateCsvDto>>($"Failed to import partner rates: {ex.Message}");
        }
    }
}

