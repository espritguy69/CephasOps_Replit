using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Rate Engine API Controller.
/// Provides CRUD operations for rate cards and GPON-specific rate tables.
/// Per RATE_ENGINE.md and GPON_RATECARDS.md specifications.
/// </summary>
[ApiController]
[Route("api/rates")]
[Authorize]
public class RatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRateEngineService _rateEngineService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFieldLevelSecurityFilter _fieldLevelSecurity;
    private readonly ILogger<RatesController> _logger;

    public RatesController(
        ApplicationDbContext context,
        IRateEngineService rateEngineService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IFieldLevelSecurityFilter fieldLevelSecurity,
        ILogger<RatesController> logger)
    {
        _context = context;
        _rateEngineService = rateEngineService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _fieldLevelSecurity = fieldLevelSecurity;
        _logger = logger;
    }

    #region Rate Resolution

    /// <summary>
    /// Resolve GPON rates (revenue and payout) for a job
    /// </summary>
    [HttpPost("resolve")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<GponRateResolutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GponRateResolutionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GponRateResolutionResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponRateResolutionResult>>> ResolveRates(
        [FromBody] GponRateResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _rateEngineService.ResolveGponRatesAsync(request);
            await _fieldLevelSecurity.ApplyGponRateResolutionResultAsync(result, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving rates");
            return this.Error<GponRateResolutionResult>($"Rate resolution failed: {ex.Message}", 400);
        }
    }

    /// <summary>
    /// Resolve universal rates using flexible dimensions
    /// </summary>
    [HttpPost("resolve/universal")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<UniversalRateResolutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UniversalRateResolutionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UniversalRateResolutionResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UniversalRateResolutionResult>>> ResolveUniversalRates(
        [FromBody] UniversalRateResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _rateEngineService.ResolveUniversalRateAsync(request);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving universal rates");
            return this.Error<UniversalRateResolutionResult>($"Rate resolution failed: {ex.Message}", 400);
        }
    }

    #endregion

    #region Rate Cards

    /// <summary>
    /// Get all rate cards
    /// </summary>
    [HttpGet("ratecards")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<List<RateCardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<RateCardDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<RateCardDto>>>> GetRateCards(
        [FromQuery] string? rateContext = null,
        [FromQuery] string? rateKind = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var query = _context.RateCards
            .Include(rc => rc.Lines)
            .Where(rc => rc.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(rateContext) && Enum.TryParse<Domain.Rates.Enums.RateContext>(rateContext, true, out var ctx))
        {
            query = query.Where(rc => rc.RateContext == ctx);
        }

        if (!string.IsNullOrEmpty(rateKind) && Enum.TryParse<Domain.Rates.Enums.RateKind>(rateKind, true, out var kind))
        {
            query = query.Where(rc => rc.RateKind == kind);
        }

        if (isActive.HasValue)
        {
            query = query.Where(rc => rc.IsActive == isActive.Value);
        }

        var rateCards = await query.OrderBy(rc => rc.Name).ToListAsync(cancellationToken);

        return this.Success(rateCards.Select(MapToRateCardDto).ToList());
    }

    /// <summary>
    /// Get a single rate card by ID
    /// </summary>
    [HttpGet("ratecards/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RateCardDto>>> GetRateCard(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rateCard = await _context.RateCards
            .Include(rc => rc.Lines)
            .Where(rc => rc.Id == id && rc.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (rateCard == null)
        {
            return this.NotFound<RateCardDto>("Rate card not found");
        }

        return this.Success(MapToRateCardDto(rateCard));
    }

    /// <summary>
    /// Create a new rate card
    /// </summary>
    [HttpPost("ratecards")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RateCardDto>>> CreateRateCard(
        [FromBody] CreateRateCardDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        if (!Enum.TryParse<Domain.Rates.Enums.RateContext>(dto.RateContext, true, out var rateContext))
        {
            return this.Error<RateCardDto>($"Invalid rate context: {dto.RateContext}", 400);
        }

        if (!Enum.TryParse<Domain.Rates.Enums.RateKind>(dto.RateKind, true, out var rateKind))
        {
            return this.Error<RateCardDto>($"Invalid rate kind: {dto.RateKind}", 400);
        }

        var rateCard = new RateCard
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            RateContext = rateContext,
            RateKind = rateKind,
            VerticalId = dto.VerticalId,
            DepartmentId = dto.DepartmentId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RateCards.Add(rateCard);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(201, ApiResponse<RateCardDto>.SuccessResponse(MapToRateCardDto(rateCard), "Rate card created successfully"));
    }

    /// <summary>
    /// Update a rate card
    /// </summary>
    [HttpPut("ratecards/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RateCardDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RateCardDto>>> UpdateRateCard(
        Guid id,
        [FromBody] UpdateRateCardDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rateCard = await _context.RateCards
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.CompanyId == companyId, cancellationToken);
        if (rateCard == null)
        {
            return this.NotFound<RateCardDto>("Rate card not found");
        }

        if (dto.Name != null) rateCard.Name = dto.Name;
        if (dto.Description != null) rateCard.Description = dto.Description;
        if (dto.ValidFrom.HasValue) rateCard.ValidFrom = dto.ValidFrom;
        if (dto.ValidTo.HasValue) rateCard.ValidTo = dto.ValidTo;
        if (dto.IsActive.HasValue) rateCard.IsActive = dto.IsActive.Value;

        rateCard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return this.Success(MapToRateCardDto(rateCard), "Rate card updated successfully");
    }

    /// <summary>
    /// Delete a rate card
    /// </summary>
    [HttpDelete("ratecards/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteRateCard(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rateCard = await _context.RateCards
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.CompanyId == companyId, cancellationToken);
        if (rateCard == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse("Rate card not found"));
        }

        _context.RateCards.Remove(rateCard);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(204, ApiResponse.SuccessResponse("Rate card deleted successfully"));
    }

    #endregion

    #region Rate Card Lines

    /// <summary>
    /// Get rate card lines for a specific rate card
    /// </summary>
    [HttpGet("ratecards/{rateCardId:guid}/lines")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<List<RateCardLineDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<RateCardLineDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<RateCardLineDto>>>> GetRateCardLines(
        Guid rateCardId,
        CancellationToken cancellationToken = default)
    {
        var lines = await _context.RateCardLines
            .Where(l => l.RateCardId == rateCardId)
            .OrderBy(l => l.Dimension1)
            .ThenBy(l => l.Dimension2)
            .ToListAsync(cancellationToken);

        return this.Success(lines.Select(MapToRateCardLineDto).ToList());
    }

    /// <summary>
    /// Create a rate card line
    /// </summary>
    [HttpPost("ratecards/{rateCardId:guid}/lines")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RateCardLineDto>>> CreateRateCardLine(
        Guid rateCardId,
        [FromBody] CreateRateCardLineDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rateCard = await _context.RateCards
            .FirstOrDefaultAsync(rc => rc.Id == rateCardId && rc.CompanyId == companyId, cancellationToken);
        if (rateCard == null)
        {
            return this.NotFound<RateCardLineDto>("Rate card not found");
        }

        if (!Enum.TryParse<Domain.Rates.Enums.UnitOfMeasure>(dto.UnitOfMeasure, true, out var uom))
        {
            uom = Domain.Rates.Enums.UnitOfMeasure.Job;
        }

        var line = new RateCardLine
        {
            Id = Guid.NewGuid(),
            RateCardId = rateCardId,
            Dimension1 = dto.Dimension1,
            Dimension2 = dto.Dimension2,
            Dimension3 = dto.Dimension3,
            Dimension4 = dto.Dimension4,
            PartnerGroupId = dto.PartnerGroupId,
            PartnerId = dto.PartnerId,
            RateAmount = dto.RateAmount,
            UnitOfMeasure = uom,
            Currency = dto.Currency ?? "MYR",
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RateCardLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(201, ApiResponse<RateCardLineDto>.SuccessResponse(MapToRateCardLineDto(line), "Rate card line created successfully"));
    }

    /// <summary>
    /// Update a rate card line
    /// </summary>
    [HttpPut("ratecardlines/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RateCardLineDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RateCardLineDto>>> UpdateRateCardLine(
        Guid id,
        [FromBody] UpdateRateCardLineDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var line = await _context.RateCardLines
            .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId, cancellationToken);
        if (line == null)
        {
            return this.NotFound<RateCardLineDto>("Rate card line not found");
        }

        if (dto.Dimension1 != null) line.Dimension1 = dto.Dimension1;
        if (dto.Dimension2 != null) line.Dimension2 = dto.Dimension2;
        if (dto.Dimension3 != null) line.Dimension3 = dto.Dimension3;
        if (dto.Dimension4 != null) line.Dimension4 = dto.Dimension4;
        if (dto.RateAmount.HasValue) line.RateAmount = dto.RateAmount.Value;
        if (dto.IsActive.HasValue) line.IsActive = dto.IsActive.Value;

        line.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return this.Success(MapToRateCardLineDto(line), "Rate card line updated successfully");
    }

    /// <summary>
    /// Delete a rate card line
    /// </summary>
    [HttpDelete("ratecardlines/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteRateCardLine(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var line = await _context.RateCardLines
            .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId, cancellationToken);
        if (line == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse("Rate card line not found"));
        }

        _context.RateCardLines.Remove(line);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(204, ApiResponse.SuccessResponse("Rate card line deleted successfully"));
    }

    #endregion

    #region GPON Partner Job Rates

    /// <summary>
    /// Get all GPON partner job rates (revenue rates)
    /// </summary>
    /// <param name="partnerGroupId">Filter by partner group ID (optional).</param>
    /// <param name="partnerId">Filter by partner ID (optional).</param>
    /// <param name="orderTypeId">Filter by order type ID (optional).</param>
    /// <param name="orderCategoryId">Order category ID (preferred). Maps to same filter as installationTypeId.</param>
    /// <param name="installationTypeId">Deprecated: use orderCategoryId. Kept for backward compatibility; same as OrderCategoryId.</param>
    /// <param name="isActive">Filter by active status (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("gpon/partner-rates")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<List<GponPartnerJobRateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GponPartnerJobRateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GponPartnerJobRateDto>>>> GetGponPartnerJobRates(
        [FromQuery] Guid? partnerGroupId = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? orderTypeId = null,
        [FromQuery] Guid? orderCategoryId = null,
        [FromQuery] Guid? installationTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var effectiveOrderCategoryId = orderCategoryId ?? installationTypeId;

        var query = _context.GponPartnerJobRates.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId);
        }

        if (partnerGroupId.HasValue)
        {
            query = query.Where(r => r.PartnerGroupId == partnerGroupId);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(r => r.PartnerId == partnerId);
        }

        if (orderTypeId.HasValue)
        {
            query = query.Where(r => r.OrderTypeId == orderTypeId);
        }

        if (effectiveOrderCategoryId.HasValue)
        {
            query = query.Where(r => r.OrderCategoryId == effectiveOrderCategoryId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rates = await query.OrderBy(r => r.PartnerGroupId).ToListAsync(cancellationToken);

        // Batch lookup related entities for name fields
        var partnerGroupIds = rates.Where(r => r.PartnerGroupId != Guid.Empty)
            .Select(r => r.PartnerGroupId).Distinct().ToList();
        var partnerIds = rates.Where(r => r.PartnerId.HasValue)
            .Select(r => r.PartnerId!.Value).Distinct().ToList();
        var orderTypeIds = rates.Select(r => r.OrderTypeId).Distinct().ToList();
        var orderCategoryIds = rates.Select(r => r.OrderCategoryId).Distinct().ToList();
        var installationMethodIds = rates.Where(r => r.InstallationMethodId.HasValue)
            .Select(r => r.InstallationMethodId!.Value).Distinct().ToList();

        var partnerGroups = partnerGroupIds.Any() 
            ? await _context.PartnerGroups
                .Where(pg => partnerGroupIds.Contains(pg.Id))
                .ToDictionaryAsync(pg => pg.Id, pg => pg.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var partners = partnerIds.Any()
            ? await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var orderTypes = orderTypeIds.Any()
            ? await _context.OrderTypes
                .Where(ot => orderTypeIds.Contains(ot.Id))
                .ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategoryIds.Any()
            ? await _context.OrderCategories
                .Where(oc => orderCategoryIds.Contains(oc.Id))
                .ToDictionaryAsync(oc => oc.Id, oc => oc.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethodIds.Any()
            ? await _context.InstallationMethods
                .Where(im => installationMethodIds.Contains(im.Id))
                .ToDictionaryAsync(im => im.Id, im => im.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return this.Success(rates.Select(r => MapToGponPartnerJobRateDto(
            r, partnerGroups, partners, orderTypes, orderCategories, installationMethods)).ToList());
    }

    /// <summary>
    /// Create a GPON partner job rate
    /// </summary>
    [HttpPost("gpon/partner-rates")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponPartnerJobRateDto>>> CreateGponPartnerJobRate(
        [FromBody] CreateGponPartnerJobRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = new GponPartnerJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PartnerGroupId = dto.PartnerGroupId,
            PartnerId = dto.PartnerId,
            OrderTypeId = dto.OrderTypeId,
            OrderCategoryId = dto.OrderCategoryId,
            InstallationMethodId = dto.InstallationMethodId,
            RevenueAmount = dto.RevenueAmount,
            Currency = dto.Currency ?? "MYR",
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = dto.IsActive ?? true,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.GponPartnerJobRates.Add(rate);
        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var partnerGroup = await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId && pg.CompanyId == rate.CompanyId, cancellationToken);
        var partner = rate.PartnerId.HasValue
            ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == rate.PartnerId.Value && p.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();
        var partners = partner != null 
            ? new Dictionary<Guid, string> { { partner.Id, partner.Name } }
            : new Dictionary<Guid, string>();
        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();

        return this.StatusCode(201, ApiResponse<GponPartnerJobRateDto>.SuccessResponse(
            MapToGponPartnerJobRateDto(rate, partnerGroups, partners, orderTypes, orderCategories, installationMethods), 
            "GPON partner job rate created successfully"));
    }

    /// <summary>
    /// Update a GPON partner job rate
    /// </summary>
    [HttpPut("gpon/partner-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GponPartnerJobRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponPartnerJobRateDto>>> UpdateGponPartnerJobRate(
        Guid id,
        [FromBody] UpdateGponPartnerJobRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponPartnerJobRates
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return this.NotFound<GponPartnerJobRateDto>("Rate not found");
        }

        if (dto.RevenueAmount.HasValue) rate.RevenueAmount = dto.RevenueAmount.Value;
        if (dto.ValidFrom.HasValue) rate.ValidFrom = dto.ValidFrom;
        if (dto.ValidTo.HasValue) rate.ValidTo = dto.ValidTo;
        if (dto.IsActive.HasValue) rate.IsActive = dto.IsActive.Value;
        if (dto.Notes != null) rate.Notes = dto.Notes;

        rate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var partnerGroup = await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId && pg.CompanyId == rate.CompanyId, cancellationToken);
        var partner = rate.PartnerId.HasValue
            ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == rate.PartnerId.Value && p.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();
        var partners = partner != null 
            ? new Dictionary<Guid, string> { { partner.Id, partner.Name } }
            : new Dictionary<Guid, string>();
        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();

        return this.Success(
            MapToGponPartnerJobRateDto(rate, partnerGroups, partners, orderTypes, orderCategories, installationMethods), 
            "GPON partner job rate updated successfully");
    }

    /// <summary>
    /// Delete a GPON partner job rate
    /// </summary>
    [HttpDelete("gpon/partner-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteGponPartnerJobRate(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponPartnerJobRates
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse("Rate not found"));
        }

        _context.GponPartnerJobRates.Remove(rate);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(204, ApiResponse.SuccessResponse("GPON partner job rate deleted successfully"));
    }

    /// <summary>
    /// Export GPON partner rates to CSV
    /// </summary>
    [HttpGet("gpon/partner-rates/export")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGponPartnerRates(
        [FromQuery] Guid? partnerGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var query = _context.GponPartnerJobRates.AsQueryable();

        if (companyId != Guid.Empty)
        {
            query = query.Where(r => r.CompanyId == companyId);
        }

        if (partnerGroupId.HasValue)
        {
            query = query.Where(r => r.PartnerGroupId == partnerGroupId);
        }

        var rates = await query.ToListAsync(cancellationToken);

        var csv = "Id,PartnerGroupId,PartnerId,OrderTypeId,OrderCategoryId,InstallationMethodId,RevenueAmount,Currency,ValidFrom,ValidTo,IsActive,Notes\n";
        foreach (var r in rates)
        {
            csv += $"{r.Id},{r.PartnerGroupId},{r.PartnerId},{r.OrderTypeId},{r.OrderCategoryId},{r.InstallationMethodId},{r.RevenueAmount},{r.Currency},{r.ValidFrom},{r.ValidTo},{r.IsActive},{r.Notes}\n";
        }

        // File downloads don't use ApiResponse envelope - they return file content directly
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "gpon-partner-rates.csv");
    }

    #endregion

    #region GPON SI Job Rates

    /// <summary>
    /// Get all GPON SI job rates (payout rates by level)
    /// </summary>
    /// <param name="orderTypeId">Filter by order type ID (optional).</param>
    /// <param name="orderCategoryId">Order category ID (preferred). Maps to same filter as installationTypeId.</param>
    /// <param name="installationTypeId">Deprecated: use orderCategoryId. Kept for backward compatibility; same as OrderCategoryId.</param>
    /// <param name="siLevel">Filter by SI level (optional).</param>
    /// <param name="isActive">Filter by active status (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("gpon/si-rates")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<List<GponSiJobRateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GponSiJobRateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GponSiJobRateDto>>>> GetGponSiJobRates(
        [FromQuery] Guid? orderTypeId = null,
        [FromQuery] Guid? orderCategoryId = null,
        [FromQuery] Guid? installationTypeId = null,
        [FromQuery] string? siLevel = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var effectiveOrderCategoryId = orderCategoryId ?? installationTypeId;

        var query = _context.GponSiJobRates.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId);
        }

        if (orderTypeId.HasValue)
        {
            query = query.Where(r => r.OrderTypeId == orderTypeId);
        }

        if (effectiveOrderCategoryId.HasValue)
        {
            query = query.Where(r => r.OrderCategoryId == effectiveOrderCategoryId);
        }

        if (!string.IsNullOrEmpty(siLevel))
        {
            query = query.Where(r => r.SiLevel == siLevel);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rates = await query.OrderBy(r => r.SiLevel).ToListAsync(cancellationToken);

        // Batch lookup related entities for name fields
        var orderTypeIds = rates.Select(r => r.OrderTypeId).Distinct().ToList();
        var orderCategoryIds = rates.Select(r => r.OrderCategoryId).Distinct().ToList();
        var installationMethodIds = rates.Where(r => r.InstallationMethodId.HasValue)
            .Select(r => r.InstallationMethodId!.Value).Distinct().ToList();
        var partnerGroupIds = rates.Where(r => r.PartnerGroupId.HasValue)
            .Select(r => r.PartnerGroupId!.Value).Distinct().ToList();

        var orderTypes = orderTypeIds.Any()
            ? await _context.OrderTypes
                .Where(ot => orderTypeIds.Contains(ot.Id))
                .ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategoryIds.Any()
            ? await _context.OrderCategories
                .Where(oc => orderCategoryIds.Contains(oc.Id))
                .ToDictionaryAsync(oc => oc.Id, oc => oc.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethodIds.Any()
            ? await _context.InstallationMethods
                .Where(im => installationMethodIds.Contains(im.Id))
                .ToDictionaryAsync(im => im.Id, im => im.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroupIds.Any()
            ? await _context.PartnerGroups
                .Where(pg => partnerGroupIds.Contains(pg.Id))
                .ToDictionaryAsync(pg => pg.Id, pg => pg.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return this.Success(rates.Select(r => MapToGponSiJobRateDto(
            r, orderTypes, orderCategories, installationMethods, partnerGroups)).ToList());
    }

    /// <summary>
    /// Create a GPON SI job rate
    /// </summary>
    [HttpPost("gpon/si-rates")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponSiJobRateDto>>> CreateGponSiJobRate(
        [FromBody] CreateGponSiJobRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderTypeId = dto.OrderTypeId,
            OrderCategoryId = dto.OrderCategoryId,
            InstallationMethodId = dto.InstallationMethodId,
            SiLevel = dto.SiLevel,
            PartnerGroupId = dto.PartnerGroupId,
            PayoutAmount = dto.PayoutAmount,
            Currency = dto.Currency ?? "MYR",
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = dto.IsActive ?? true,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.GponSiJobRates.Add(rate);
        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var partnerGroup = rate.PartnerGroupId.HasValue
            ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId.Value && pg.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();

        return this.StatusCode(201, ApiResponse<GponSiJobRateDto>.SuccessResponse(
            MapToGponSiJobRateDto(rate, orderTypes, orderCategories, installationMethods, partnerGroups), 
            "GPON SI job rate created successfully"));
    }

    /// <summary>
    /// Update a GPON SI job rate
    /// </summary>
    [HttpPut("gpon/si-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GponSiJobRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponSiJobRateDto>>> UpdateGponSiJobRate(
        Guid id,
        [FromBody] UpdateGponSiJobRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponSiJobRates.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return this.NotFound<GponSiJobRateDto>("Rate not found");
        }

        if (dto.PayoutAmount.HasValue) rate.PayoutAmount = dto.PayoutAmount.Value;
        if (dto.SiLevel != null) rate.SiLevel = dto.SiLevel;
        if (dto.ValidFrom.HasValue) rate.ValidFrom = dto.ValidFrom;
        if (dto.ValidTo.HasValue) rate.ValidTo = dto.ValidTo;
        if (dto.IsActive.HasValue) rate.IsActive = dto.IsActive.Value;
        if (dto.Notes != null) rate.Notes = dto.Notes;

        rate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var partnerGroup = rate.PartnerGroupId.HasValue
            ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId.Value && pg.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();

        return this.Success(
            MapToGponSiJobRateDto(rate, orderTypes, orderCategories, installationMethods, partnerGroups), 
            "GPON SI job rate updated successfully");
    }

    /// <summary>
    /// Delete a GPON SI job rate
    /// </summary>
    [HttpDelete("gpon/si-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteGponSiJobRate(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponSiJobRates.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse("Rate not found"));
        }

        _context.GponSiJobRates.Remove(rate);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(204, ApiResponse.SuccessResponse("GPON SI job rate deleted successfully"));
    }

    /// <summary>
    /// Export GPON SI rates to CSV
    /// </summary>
    [HttpGet("gpon/si-rates/export")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGponSiRates(
        [FromQuery] string? siLevel = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var query = _context.GponSiJobRates.AsQueryable();

        if (companyId != Guid.Empty)
        {
            query = query.Where(r => r.CompanyId == companyId);
        }

        if (!string.IsNullOrEmpty(siLevel))
        {
            query = query.Where(r => r.SiLevel == siLevel);
        }

        var rates = await query.ToListAsync(cancellationToken);

        var csv = "Id,OrderTypeId,OrderCategoryId,InstallationMethodId,SiLevel,PartnerGroupId,PayoutAmount,Currency,ValidFrom,ValidTo,IsActive,Notes\n";
        foreach (var r in rates)
        {
            csv += $"{r.Id},{r.OrderTypeId},{r.OrderCategoryId},{r.InstallationMethodId},{r.SiLevel},{r.PartnerGroupId},{r.PayoutAmount},{r.Currency},{r.ValidFrom},{r.ValidTo},{r.IsActive},{r.Notes}\n";
        }

        // File downloads don't use ApiResponse envelope - they return file content directly
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "gpon-si-rates.csv");
    }

    #endregion

    #region GPON SI Custom Rates

    /// <summary>
    /// Get all GPON SI custom rates (per-SI overrides)
    /// </summary>
    [HttpGet("gpon/si-custom-rates")]
    [RequirePermission(PermissionCatalog.RatesView)]
    [ProducesResponseType(typeof(ApiResponse<List<GponSiCustomRateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GponSiCustomRateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GponSiCustomRateDto>>>> GetGponSiCustomRates(
        [FromQuery] Guid? serviceInstallerId = null,
        [FromQuery] Guid? orderTypeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var query = _context.GponSiCustomRates.AsQueryable();

        if (companyId != Guid.Empty)
        {
            query = query.Where(r => r.CompanyId == companyId);
        }

        if (serviceInstallerId.HasValue)
        {
            query = query.Where(r => r.ServiceInstallerId == serviceInstallerId);
        }

        if (orderTypeId.HasValue)
        {
            query = query.Where(r => r.OrderTypeId == orderTypeId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rates = await query.OrderBy(r => r.ServiceInstallerId).ToListAsync(cancellationToken);

        // Batch lookup related entities for name fields
        var serviceInstallerIds = rates.Select(r => r.ServiceInstallerId).Distinct().ToList();
        var orderTypeIds = rates.Select(r => r.OrderTypeId).Distinct().ToList();
        var orderCategoryIds = rates.Select(r => r.OrderCategoryId).Distinct().ToList();
        var installationMethodIds = rates.Where(r => r.InstallationMethodId.HasValue)
            .Select(r => r.InstallationMethodId!.Value).Distinct().ToList();
        var partnerGroupIds = rates.Where(r => r.PartnerGroupId.HasValue)
            .Select(r => r.PartnerGroupId!.Value).Distinct().ToList();

        var serviceInstallers = serviceInstallerIds.Any()
            ? await _context.ServiceInstallers
                .Where(si => serviceInstallerIds.Contains(si.Id))
                .ToDictionaryAsync(si => si.Id, si => si.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var orderTypes = orderTypeIds.Any()
            ? await _context.OrderTypes
                .Where(ot => orderTypeIds.Contains(ot.Id))
                .ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategoryIds.Any()
            ? await _context.OrderCategories
                .Where(oc => orderCategoryIds.Contains(oc.Id))
                .ToDictionaryAsync(oc => oc.Id, oc => oc.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethodIds.Any()
            ? await _context.InstallationMethods
                .Where(im => installationMethodIds.Contains(im.Id))
                .ToDictionaryAsync(im => im.Id, im => im.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroupIds.Any()
            ? await _context.PartnerGroups
                .Where(pg => partnerGroupIds.Contains(pg.Id))
                .ToDictionaryAsync(pg => pg.Id, pg => pg.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return this.Success(rates.Select(r => MapToGponSiCustomRateDto(
            r, serviceInstallers, orderTypes, orderCategories, installationMethods, partnerGroups)).ToList());
    }

    /// <summary>
    /// Create a GPON SI custom rate
    /// </summary>
    [HttpPost("gpon/si-custom-rates")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponSiCustomRateDto>>> CreateGponSiCustomRate(
        [FromBody] CreateGponSiCustomRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;

        var rate = new GponSiCustomRate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ServiceInstallerId = dto.ServiceInstallerId,
            OrderTypeId = dto.OrderTypeId,
            OrderCategoryId = dto.OrderCategoryId,
            InstallationMethodId = dto.InstallationMethodId,
            PartnerGroupId = dto.PartnerGroupId,
            CustomPayoutAmount = dto.CustomPayoutAmount,
            Currency = dto.Currency ?? "MYR",
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = dto.IsActive ?? true,
            Reason = dto.Reason,
            ApprovedByUserId = userId,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.GponSiCustomRates.Add(rate);
        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var serviceInstaller = await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == rate.ServiceInstallerId && si.CompanyId == rate.CompanyId, cancellationToken);
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var partnerGroup = rate.PartnerGroupId.HasValue
            ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId.Value && pg.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var serviceInstallers = serviceInstaller != null 
            ? new Dictionary<Guid, string> { { serviceInstaller.Id, serviceInstaller.Name } }
            : new Dictionary<Guid, string>();
        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();

        return this.StatusCode(201, ApiResponse<GponSiCustomRateDto>.SuccessResponse(
            MapToGponSiCustomRateDto(rate, serviceInstallers, orderTypes, orderCategories, installationMethods, partnerGroups), 
            "GPON SI custom rate created successfully"));
    }

    /// <summary>
    /// Update a GPON SI custom rate
    /// </summary>
    [HttpPut("gpon/si-custom-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GponSiCustomRateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GponSiCustomRateDto>>> UpdateGponSiCustomRate(
        Guid id,
        [FromBody] UpdateGponSiCustomRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponSiCustomRates
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return this.NotFound<GponSiCustomRateDto>("Rate not found");
        }

        if (dto.CustomPayoutAmount.HasValue) rate.CustomPayoutAmount = dto.CustomPayoutAmount.Value;
        if (dto.ValidFrom.HasValue) rate.ValidFrom = dto.ValidFrom;
        if (dto.ValidTo.HasValue) rate.ValidTo = dto.ValidTo;
        if (dto.IsActive.HasValue) rate.IsActive = dto.IsActive.Value;
        if (dto.Reason != null) rate.Reason = dto.Reason;

        rate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // For create/update, we need to lookup names individually (company-scoped to satisfy CEPHAS004)
        var serviceInstaller = await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == rate.ServiceInstallerId && si.CompanyId == rate.CompanyId, cancellationToken);
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == rate.OrderTypeId && ot.CompanyId == rate.CompanyId, cancellationToken);
        var orderCategory = await _context.OrderCategories.FirstOrDefaultAsync(oc => oc.Id == rate.OrderCategoryId && oc.CompanyId == rate.CompanyId, cancellationToken);
        var installationMethod = rate.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == rate.InstallationMethodId.Value && im.CompanyId == rate.CompanyId, cancellationToken)
            : null;
        var partnerGroup = rate.PartnerGroupId.HasValue
            ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == rate.PartnerGroupId.Value && pg.CompanyId == rate.CompanyId, cancellationToken)
            : null;

        var serviceInstallers = serviceInstaller != null 
            ? new Dictionary<Guid, string> { { serviceInstaller.Id, serviceInstaller.Name } }
            : new Dictionary<Guid, string>();
        var orderTypes = orderType != null 
            ? new Dictionary<Guid, string> { { orderType.Id, orderType.Name } }
            : new Dictionary<Guid, string>();
        var orderCategories = orderCategory != null 
            ? new Dictionary<Guid, string> { { orderCategory.Id, orderCategory.Name } }
            : new Dictionary<Guid, string>();
        var installationMethods = installationMethod != null 
            ? new Dictionary<Guid, string> { { installationMethod.Id, installationMethod.Name } }
            : new Dictionary<Guid, string>();
        var partnerGroups = partnerGroup != null 
            ? new Dictionary<Guid, string> { { partnerGroup.Id, partnerGroup.Name } }
            : new Dictionary<Guid, string>();

        return this.Success(
            MapToGponSiCustomRateDto(rate, serviceInstallers, orderTypes, orderCategories, installationMethods, partnerGroups), 
            "GPON SI custom rate updated successfully");
    }

    /// <summary>
    /// Delete a GPON SI custom rate
    /// </summary>
    [HttpDelete("gpon/si-custom-rates/{id:guid}")]
    [RequirePermission(PermissionCatalog.RatesEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteGponSiCustomRate(Guid id, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        var rate = await _context.GponSiCustomRates
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);
        if (rate == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse("Rate not found"));
        }

        _context.GponSiCustomRates.Remove(rate);
        await _context.SaveChangesAsync(cancellationToken);

        return this.StatusCode(204, ApiResponse.SuccessResponse("GPON SI custom rate deleted successfully"));
    }

    #endregion

    #region DTO Mappings

    private static RateCardDto MapToRateCardDto(RateCard rc) => new()
    {
        Id = rc.Id,
        Name = rc.Name,
        Code = rc.Name, // Use Name as Code since entity doesn't have Code
        Description = rc.Description,
        RateContext = rc.RateContext.ToString(),
        RateKind = rc.RateKind.ToString(),
        VerticalId = rc.VerticalId,
        DepartmentId = rc.DepartmentId,
        ValidFrom = rc.ValidFrom,
        ValidTo = rc.ValidTo,
        IsActive = rc.IsActive,
        LinesCount = rc.Lines?.Count ?? 0,
        CreatedAt = rc.CreatedAt,
        UpdatedAt = rc.UpdatedAt
    };

    private static RateCardLineDto MapToRateCardLineDto(RateCardLine l) => new()
    {
        Id = l.Id,
        RateCardId = l.RateCardId,
        Dimension1 = l.Dimension1,
        Dimension2 = l.Dimension2,
        Dimension3 = l.Dimension3,
        Dimension4 = l.Dimension4,
        PartnerGroupId = l.PartnerGroupId,
        PartnerId = l.PartnerId,
        RateAmount = l.RateAmount,
        UnitOfMeasure = l.UnitOfMeasure.ToString(),
        Currency = l.Currency,
        IsActive = l.IsActive,
        Notes = l.ExtraJson, // Map ExtraJson to Notes for DTO compatibility
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    private static GponPartnerJobRateDto MapToGponPartnerJobRateDto(
        GponPartnerJobRate r,
        Dictionary<Guid, string> partnerGroups,
        Dictionary<Guid, string> partners,
        Dictionary<Guid, string> orderTypes,
        Dictionary<Guid, string> orderCategories,
        Dictionary<Guid, string> installationMethods) => new()
    {
        Id = r.Id,
        PartnerGroupId = r.PartnerGroupId,
        PartnerGroupName = partnerGroups.GetValueOrDefault(r.PartnerGroupId),
        PartnerId = r.PartnerId,
        PartnerName = r.PartnerId.HasValue ? partners.GetValueOrDefault(r.PartnerId.Value) : null,
        OrderTypeId = r.OrderTypeId,
        OrderTypeName = orderTypes.GetValueOrDefault(r.OrderTypeId),
        OrderCategoryId = r.OrderCategoryId,
        OrderCategoryName = orderCategories.GetValueOrDefault(r.OrderCategoryId),
        InstallationMethodId = r.InstallationMethodId,
        InstallationMethodName = r.InstallationMethodId.HasValue 
            ? installationMethods.GetValueOrDefault(r.InstallationMethodId.Value) 
            : null,
        RevenueAmount = r.RevenueAmount,
        Currency = r.Currency,
        ValidFrom = r.ValidFrom,
        ValidTo = r.ValidTo,
        IsActive = r.IsActive,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static GponSiJobRateDto MapToGponSiJobRateDto(
        GponSiJobRate r,
        Dictionary<Guid, string> orderTypes,
        Dictionary<Guid, string> orderCategories,
        Dictionary<Guid, string> installationMethods,
        Dictionary<Guid, string> partnerGroups) => new()
    {
        Id = r.Id,
        OrderTypeId = r.OrderTypeId,
        OrderTypeName = orderTypes.GetValueOrDefault(r.OrderTypeId),
        OrderCategoryId = r.OrderCategoryId,
        OrderCategoryName = orderCategories.GetValueOrDefault(r.OrderCategoryId),
        InstallationMethodId = r.InstallationMethodId,
        InstallationMethodName = r.InstallationMethodId.HasValue 
            ? installationMethods.GetValueOrDefault(r.InstallationMethodId.Value) 
            : null,
        SiLevel = r.SiLevel,
        PartnerGroupId = r.PartnerGroupId,
        PartnerGroupName = r.PartnerGroupId.HasValue 
            ? partnerGroups.GetValueOrDefault(r.PartnerGroupId.Value) 
            : null,
        PayoutAmount = r.PayoutAmount,
        Currency = r.Currency,
        ValidFrom = r.ValidFrom,
        ValidTo = r.ValidTo,
        IsActive = r.IsActive,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static GponSiCustomRateDto MapToGponSiCustomRateDto(
        GponSiCustomRate r,
        Dictionary<Guid, string> serviceInstallers,
        Dictionary<Guid, string> orderTypes,
        Dictionary<Guid, string> orderCategories,
        Dictionary<Guid, string> installationMethods,
        Dictionary<Guid, string> partnerGroups) => new()
    {
        Id = r.Id,
        ServiceInstallerId = r.ServiceInstallerId,
        ServiceInstallerName = serviceInstallers.GetValueOrDefault(r.ServiceInstallerId),
        OrderTypeId = r.OrderTypeId,
        OrderTypeName = orderTypes.GetValueOrDefault(r.OrderTypeId),
        OrderCategoryId = r.OrderCategoryId,
        OrderCategoryName = orderCategories.GetValueOrDefault(r.OrderCategoryId),
        InstallationMethodId = r.InstallationMethodId,
        InstallationMethodName = r.InstallationMethodId.HasValue 
            ? installationMethods.GetValueOrDefault(r.InstallationMethodId.Value) 
            : null,
        PartnerGroupId = r.PartnerGroupId,
        PartnerGroupName = r.PartnerGroupId.HasValue 
            ? partnerGroups.GetValueOrDefault(r.PartnerGroupId.Value) 
            : null,
        CustomPayoutAmount = r.CustomPayoutAmount,
        Currency = r.Currency,
        ValidFrom = r.ValidFrom,
        ValidTo = r.ValidTo,
        IsActive = r.IsActive,
        Reason = r.Reason,
        ApprovedByUserId = r.ApprovedByUserId,
        ApprovedAt = r.ApprovedAt,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    #endregion
}

#region DTOs

public class RateCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RateContext { get; set; } = string.Empty;
    public string RateKind { get; set; } = string.Empty;
    public Guid? VerticalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public int LinesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRateCardDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RateContext { get; set; } = string.Empty;
    public string RateKind { get; set; } = string.Empty;
    public Guid? VerticalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateRateCardDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
}

public class RateCardLineDto
{
    public Guid Id { get; set; }
    public Guid RateCardId { get; set; }
    public string? Dimension1 { get; set; }
    public string? Dimension2 { get; set; }
    public string? Dimension3 { get; set; }
    public string? Dimension4 { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal RateAmount { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Currency { get; set; } = "MYR";
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRateCardLineDto
{
    public Guid RateCardId { get; set; }
    public string? Dimension1 { get; set; }
    public string? Dimension2 { get; set; }
    public string? Dimension3 { get; set; }
    public string? Dimension4 { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public decimal RateAmount { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Currency { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UpdateRateCardLineDto
{
    public string? Dimension1 { get; set; }
    public string? Dimension2 { get; set; }
    public string? Dimension3 { get; set; }
    public string? Dimension4 { get; set; }
    public decimal? RateAmount { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class GponPartnerJobRateDto
{
    public Guid Id { get; set; }
    public Guid PartnerGroupId { get; set; }
    public string? PartnerGroupName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public Guid OrderCategoryId { get; set; }
    public string? OrderCategoryName { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public decimal RevenueAmount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGponPartnerJobRateDto
{
    public Guid PartnerGroupId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public Guid OrderCategoryId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public decimal RevenueAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UpdateGponPartnerJobRateDto
{
    public decimal? RevenueAmount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class GponSiJobRateDto
{
    public Guid Id { get; set; }
    public Guid OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public Guid OrderCategoryId { get; set; }
    public string? OrderCategoryName { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public string SiLevel { get; set; } = string.Empty;
    public Guid? PartnerGroupId { get; set; }
    public string? PartnerGroupName { get; set; }
    public decimal PayoutAmount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGponSiJobRateDto
{
    public Guid OrderTypeId { get; set; }
    public Guid OrderCategoryId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string SiLevel { get; set; } = string.Empty;
    public Guid? PartnerGroupId { get; set; }
    public decimal PayoutAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UpdateGponSiJobRateDto
{
    public decimal? PayoutAmount { get; set; }
    public string? SiLevel { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class GponSiCustomRateDto
{
    public Guid Id { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public string? ServiceInstallerName { get; set; }
    public Guid OrderTypeId { get; set; }
    public string? OrderTypeName { get; set; }
    public Guid OrderCategoryId { get; set; }
    public string? OrderCategoryName { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public string? PartnerGroupName { get; set; }
    public decimal CustomPayoutAmount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGponSiCustomRateDto
{
    public Guid ServiceInstallerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public Guid OrderCategoryId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public Guid? PartnerGroupId { get; set; }
    public decimal CustomPayoutAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

public class UpdateGponSiCustomRateDto
{
    public decimal? CustomPayoutAmount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
    public string? Reason { get; set; }
}

#endregion

