using CephasOps.Application.Common;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.Rates.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Rate Engine Service implementation.
/// Provides centralized rate resolution for all verticals.
/// Per RATE_ENGINE.md specification.
///
/// GPON payout resolution order (Phase 3 + RateModifier):
/// 1. GponSiCustomRate (per-SI overrides) - HIGHEST
/// 2. BaseWorkRate or GponSiJobRate (legacy) → then apply RateModifiers (InstallationMethod → SITier → Partner)
/// 3. GponSiJobRate (legacy default by SI level) - LOWEST
/// </summary>
public class RateEngineService : IRateEngineService
{
    private const string BaseWorkRateCacheKeyPrefix = "BWR:";
    private static readonly TimeSpan BaseWorkRateCacheTtl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateEngineService> _logger;

    public RateEngineService(
        ApplicationDbContext dbContext,
        IMemoryCache cache,
        ILogger<RateEngineService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GponRateResolutionResult> ResolveGponRatesAsync(GponRateResolutionRequest request)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("ResolveGponRates");
        var result = new GponRateResolutionResult
        {
            Success = true,
            ResolutionSteps = new List<string>()
        };

        var referenceDate = request.ReferenceDate ?? DateTime.UtcNow;
        var (_, orderSubtypeId) = await GetOrderTypeParentAndSubtypeAsync(request.OrderTypeId);
        result.ResolutionContext = new ResolutionContextDto
        {
            CompanyId = request.CompanyId,
            EffectiveDateUsed = referenceDate,
            OrderTypeId = request.OrderTypeId,
            OrderSubtypeId = orderSubtypeId,
            OrderCategoryId = request.OrderCategoryId,
            InstallationMethodId = request.InstallationMethodId,
            SiTier = request.SiLevel,
            PartnerGroupId = request.PartnerGroupId
        };

        try
        {
            // Step 1: Resolve Revenue Rate
            result.ResolutionSteps.Add($"Revenue lookup: OrderType={request.OrderTypeId}, OrderCategory={request.OrderCategoryId}");

            var revenueRate = await ResolveGponRevenueRateInternalAsync(
                request.OrderTypeId,
                request.OrderCategoryId,
                request.InstallationMethodId,
                request.PartnerGroupId,
                request.PartnerId,
                referenceDate);

            if (revenueRate != null)
            {
                result.RevenueAmount = revenueRate.RevenueAmount;
                result.RevenueSource = "GponPartnerJobRate";
                result.RevenueRateId = revenueRate.Id;
                result.ResolutionSteps.Add($"Revenue matched: {revenueRate.RevenueAmount} {result.Currency} (GponPartnerJobRate)");
            }
            else
            {
                result.ResolutionSteps.Add("Revenue: no matching partner rate found");
            }

            result.ResolutionSteps.Add($"Payout resolution: SI={request.ServiceInstallerId}, Level={request.SiLevel}");

            // Step 2a: Check for Custom Rate first (highest priority)
            if (request.ServiceInstallerId.HasValue)
            {
                var customRate = await ResolveGponCustomRateAsync(
                    request.ServiceInstallerId.Value,
                    request.OrderTypeId,
                    request.OrderCategoryId,
                    request.InstallationMethodId,
                    request.PartnerGroupId,
                    referenceDate);

                if (customRate != null)
                {
                    result.PayoutAmount = customRate.CustomPayoutAmount;
                    result.PayoutSource = "GponSiCustomRate";
                    result.PayoutRateId = customRate.Id;
                    result.PayoutPath = "CustomOverride";
                    result.ResolutionMatchLevel = "Custom";
                    result.MatchedRuleDetails = new MatchedRuleDetailsDto { CustomRateId = customRate.Id };
                    result.ResolutionSteps.Add("Checked Custom SI rate → matched");
                    result.ResolutionSteps.Add($"Custom override payout: {customRate.CustomPayoutAmount} {result.Currency} (no modifiers applied)");
                    result.ResolutionSteps.Add($"Final payout: {customRate.CustomPayoutAmount} {result.Currency}");
                    return result;
                }

                result.ResolutionSteps.Add("Checked Custom SI rate → none found");
            }
            else
            {
                result.ResolutionSteps.Add("Skipped Custom SI check (no installer selected)");
            }

            // Step 2b: Try Base Work Rate (Phase 3: Rate Group + Category + InstallationMethod + optional Subtype)
            decimal? baseAmount = null;
            string? payoutSource = null;
            Guid? payoutRateId = null;

            var baseWorkRate = await ResolveBaseWorkRateAsync(
                request.CompanyId,
                request.OrderTypeId,
                request.OrderCategoryId,
                request.InstallationMethodId,
                referenceDate);
            if (baseWorkRate != null)
            {
                baseAmount = baseWorkRate.Amount;
                payoutSource = "BaseWorkRate";
                payoutRateId = baseWorkRate.Id;
                result.PayoutPath = "BaseWorkRate";
                result.BaseAmountBeforeModifiers = baseWorkRate.Amount;
                var matchLevel = baseWorkRate.OrderCategoryId.HasValue ? "ExactCategory" : baseWorkRate.ServiceProfileId.HasValue ? "ServiceProfile" : "BroadRateGroup";
                result.ResolutionMatchLevel = matchLevel;
                result.MatchedRuleDetails = new MatchedRuleDetailsDto
                {
                    RateGroupId = baseWorkRate.RateGroupId,
                    BaseWorkRateId = baseWorkRate.Id,
                    ServiceProfileId = baseWorkRate.ServiceProfileId
                };
                result.ResolutionSteps.Add($"Matched Base Work Rate ({matchLevel}): {baseWorkRate.Amount} {baseWorkRate.Currency}");
            }

            // Step 2c: Fall back to Legacy Default Payout Rate by SI Level (GponSiJobRate)
            if (!baseAmount.HasValue && !string.IsNullOrEmpty(request.SiLevel))
            {
                var payoutRate = await ResolveGponPayoutRateInternalAsync(
                    request.CompanyId != Guid.Empty ? request.CompanyId : (Guid?)null,
                    request.OrderTypeId,
                    request.OrderCategoryId,
                    request.InstallationMethodId,
                    request.SiLevel,
                    request.PartnerGroupId,
                    referenceDate);
                if (payoutRate != null)
                {
                    baseAmount = payoutRate.PayoutAmount;
                    payoutSource = "GponSiJobRate";
                    payoutRateId = payoutRate.Id;
                    result.PayoutPath = "Legacy";
                    result.ResolutionMatchLevel = "Legacy";
                    result.BaseAmountBeforeModifiers = payoutRate.PayoutAmount;
                    result.MatchedRuleDetails = new MatchedRuleDetailsDto { LegacyRateId = payoutRate.Id };
                    result.Warnings.Add("Used legacy fallback (GponSiJobRate). Consider configuring Base Work Rate for this context.");
                    result.ResolutionSteps.Add($"Fallback: matched Legacy SI rate (GponSiJobRate): {payoutRate.PayoutAmount} {result.Currency}");
                }
            }

            if (!baseAmount.HasValue)
            {
                if (string.IsNullOrEmpty(request.SiLevel))
                {
                    result.ResolutionSteps.Add("No SI Level provided → legacy lookup skipped");
                    result.Warnings.Add("SI Level not provided; legacy payout lookup was skipped.");
                }
                else
                {
                    result.ResolutionSteps.Add($"No Base Work Rate or Legacy rate found for SI Level: {request.SiLevel}");
                    result.Warnings.Add("No payout rate found. Check Base Work Rate or legacy SI rates for this context.");
                }
                return result;
            }

            result.PayoutAmount = baseAmount;
            result.PayoutSource = payoutSource;
            result.PayoutRateId = payoutRateId;

            // Step 2d: Apply RateModifiers (InstallationMethod → SITier → Partner)
            var (adjustedAmount, modifierSteps, modifierTrace) = await ApplyRateModifiersAsync(
                request.CompanyId,
                baseAmount.Value,
                request.InstallationMethodId,
                request.SiLevel,
                request.PartnerGroupId);
            result.PayoutAmount = adjustedAmount;
            result.ModifierTrace = modifierTrace;
            foreach (var step in modifierSteps)
                result.ResolutionSteps.Add(step);
            result.ResolutionSteps.Add($"Final payout: {adjustedAmount} {result.Currency}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving GPON rates for OrderType={OrderTypeId}, OrderCategory={OrderCategoryId}",
                request.OrderTypeId, request.OrderCategoryId);

            return GponRateResolutionResult.Failed($"Rate resolution failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<decimal?> GetGponRevenueRateAsync(
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? partnerGroupId,
        Guid? partnerId = null,
        DateTime? referenceDate = null)
    {
        var rate = await ResolveGponRevenueRateInternalAsync(
            orderTypeId,
            orderCategoryId,
            installationMethodId,
            partnerGroupId,
            partnerId,
            referenceDate ?? DateTime.UtcNow);

        return rate?.RevenueAmount;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetGponPayoutRateAsync(
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? serviceInstallerId,
        string? siLevel,
        Guid? partnerGroupId = null,
        DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.UtcNow;

        // Priority 1: Check Custom Rate
        if (serviceInstallerId.HasValue)
        {
            var customRate = await ResolveGponCustomRateAsync(
                serviceInstallerId.Value,
                orderTypeId,
                orderCategoryId,
                installationMethodId,
                partnerGroupId,
                refDate);

            if (customRate != null)
            {
                return customRate.CustomPayoutAmount;
            }
        }

        // Priority 2: Base Work Rate (Phase 3; companyId null when not provided)
        decimal? baseAmount = null;
        var baseWorkRate = await ResolveBaseWorkRateAsync(null, orderTypeId, orderCategoryId, installationMethodId, refDate);
        if (baseWorkRate != null)
        {
            baseAmount = baseWorkRate.Amount;
        }

        // Priority 3: Legacy Default Payout Rate by SI Level
        if (!baseAmount.HasValue && !string.IsNullOrEmpty(siLevel))
        {
            var payoutRate = await ResolveGponPayoutRateInternalAsync(
                null,
                orderTypeId,
                orderCategoryId,
                installationMethodId,
                siLevel,
                partnerGroupId,
                refDate);
            if (payoutRate != null)
                baseAmount = payoutRate.PayoutAmount;
        }

        if (!baseAmount.HasValue)
            return null;

        // Apply RateModifiers (InstallationMethod → SITier → Partner)
        var (adjustedAmount, _, _) = await ApplyRateModifiersAsync(null, baseAmount.Value, installationMethodId, siLevel, partnerGroupId);
        return adjustedAmount;
    }

    /// <inheritdoc />
    public async Task<UniversalRateResolutionResult> ResolveUniversalRateAsync(UniversalRateResolutionRequest request)
    {
        var result = new UniversalRateResolutionResult
        {
            Success = false,
            ResolutionSteps = new List<string>()
        };

        var referenceDate = request.ReferenceDate ?? DateTime.UtcNow;

        try
        {
            // Parse rate context and kind
            if (!Enum.TryParse<RateContext>(request.RateContext, true, out var rateContext))
            {
                result.ErrorMessage = $"Invalid rate context: {request.RateContext}";
                return result;
            }

            if (!Enum.TryParse<RateKind>(request.RateKind, true, out var rateKind))
            {
                result.ErrorMessage = $"Invalid rate kind: {request.RateKind}";
                return result;
            }

            result.ResolutionSteps.Add($"Resolving {request.RateContext}/{request.RateKind} rate");

            // Step 1: Check CustomRate (if UserId provided)
            if (request.UserId.HasValue)
            {
                result.ResolutionSteps.Add($"Checking custom rate for User={request.UserId}");

                var customRate = await _dbContext.CustomRates
                    .Where(cr => cr.UserId == request.UserId.Value)
                    .Where(cr => cr.IsActive)
                    .Where(cr => cr.Dimension1 == request.Dimension1 || (cr.Dimension1 == null && request.Dimension1 == null))
                    .Where(cr => cr.Dimension2 == request.Dimension2 || (cr.Dimension2 == null && request.Dimension2 == null))
                    .Where(cr => cr.Dimension3 == request.Dimension3 || (cr.Dimension3 == null && request.Dimension3 == null))
                    .Where(cr => cr.Dimension4 == request.Dimension4 || (cr.Dimension4 == null && request.Dimension4 == null))
                    .Where(cr => (!cr.ValidFrom.HasValue || cr.ValidFrom <= referenceDate) &&
                                 (!cr.ValidTo.HasValue || cr.ValidTo >= referenceDate))
                    .FirstOrDefaultAsync();

                if (customRate != null)
                {
                    result.Success = true;
                    result.RateAmount = customRate.CustomRateAmount;
                    result.RateSource = "CustomRate";
                    result.RateId = customRate.Id;
                    result.UnitOfMeasure = customRate.UnitOfMeasure.ToString();
                    result.Currency = customRate.Currency;
                    result.ResolutionSteps.Add($"Custom rate found: {customRate.CustomRateAmount} {customRate.Currency}");
                    return result;
                }

                result.ResolutionSteps.Add("No custom rate found");
            }

            // Step 2-4: Check RateCardLines with partner hierarchy
            result.ResolutionSteps.Add("Checking rate cards");

            // Find applicable rate cards
            var rateCards = await _dbContext.RateCards
                .Where(rc => rc.RateContext == rateContext)
                .Where(rc => rc.RateKind == rateKind)
                .Where(rc => rc.IsActive)
                .Where(rc => (!rc.ValidFrom.HasValue || rc.ValidFrom <= referenceDate) &&
                             (!rc.ValidTo.HasValue || rc.ValidTo >= referenceDate))
                .Where(rc => !request.VerticalId.HasValue || rc.VerticalId == request.VerticalId || rc.VerticalId == null)
                .Where(rc => !request.DepartmentId.HasValue || rc.DepartmentId == request.DepartmentId || rc.DepartmentId == null)
                .Include(rc => rc.Lines.Where(l => l.IsActive))
                .ToListAsync();

            if (!rateCards.Any())
            {
                result.ErrorMessage = $"No active rate cards found for {request.RateContext}/{request.RateKind}";
                result.ResolutionSteps.Add(result.ErrorMessage);
                return result;
            }

            result.ResolutionSteps.Add($"Found {rateCards.Count} applicable rate cards");

            // Collect all matching lines from all rate cards
            var allLines = rateCards.SelectMany(rc => rc.Lines).ToList();

            // Filter by dimensions
            var matchingLines = allLines
                .Where(l => l.Dimension1 == request.Dimension1 || (l.Dimension1 == null && request.Dimension1 == null) || l.Dimension1 == null)
                .Where(l => l.Dimension2 == request.Dimension2 || (l.Dimension2 == null && request.Dimension2 == null) || l.Dimension2 == null)
                .Where(l => l.Dimension3 == request.Dimension3 || (l.Dimension3 == null && request.Dimension3 == null) || l.Dimension3 == null)
                .Where(l => l.Dimension4 == request.Dimension4 || (l.Dimension4 == null && request.Dimension4 == null) || l.Dimension4 == null)
                .ToList();

            if (!matchingLines.Any())
            {
                result.ErrorMessage = "No matching rate card lines found for the specified dimensions";
                result.ResolutionSteps.Add(result.ErrorMessage);
                return result;
            }

            result.ResolutionSteps.Add($"Found {matchingLines.Count} matching rate card lines");

            // Step 2: Try PartnerId match first
            if (request.PartnerId.HasValue)
            {
                var partnerLine = matchingLines.FirstOrDefault(l => l.PartnerId == request.PartnerId);
                if (partnerLine != null)
                {
                    SetResultFromRateCardLine(result, partnerLine, "RateCardLine (Partner)");
                    return result;
                }
                result.ResolutionSteps.Add("No partner-specific rate found");
            }

            // Step 3: Try PartnerGroupId match
            if (request.PartnerGroupId.HasValue)
            {
                var groupLine = matchingLines.FirstOrDefault(l => l.PartnerGroupId == request.PartnerGroupId && l.PartnerId == null);
                if (groupLine != null)
                {
                    SetResultFromRateCardLine(result, groupLine, "RateCardLine (PartnerGroup)");
                    return result;
                }
                result.ResolutionSteps.Add("No partner group rate found");
            }

            // Step 4: Fall back to system default (no partner filter)
            var defaultLine = matchingLines.FirstOrDefault(l => l.PartnerId == null && l.PartnerGroupId == null);
            if (defaultLine != null)
            {
                SetResultFromRateCardLine(result, defaultLine, "RateCardLine (Default)");
                return result;
            }

            // If no default, take the first matching line
            var anyLine = matchingLines.FirstOrDefault();
            if (anyLine != null)
            {
                SetResultFromRateCardLine(result, anyLine, "RateCardLine (Fallback)");
                return result;
            }

            result.ErrorMessage = "No applicable rate found";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving universal rate for {RateContext}/{RateKind}",
                request.RateContext, request.RateKind);

            result.ErrorMessage = $"Rate resolution failed: {ex.Message}";
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasCustomRateAsync(
        Guid userId,
        string? dimension1 = null,
        string? dimension2 = null,
        string? dimension3 = null,
        string? dimension4 = null,
        DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.UtcNow;

        return await _dbContext.CustomRates
            .Where(cr => cr.UserId == userId)
            .Where(cr => cr.IsActive)
            .Where(cr => dimension1 == null || cr.Dimension1 == dimension1)
            .Where(cr => dimension2 == null || cr.Dimension2 == dimension2)
            .Where(cr => dimension3 == null || cr.Dimension3 == dimension3)
            .Where(cr => dimension4 == null || cr.Dimension4 == dimension4)
            .Where(cr => (!cr.ValidFrom.HasValue || cr.ValidFrom <= refDate) &&
                         (!cr.ValidTo.HasValue || cr.ValidTo >= refDate))
            .AnyAsync();
    }

    #region Private Helper Methods

    private async Task<GponPartnerJobRate?> ResolveGponRevenueRateInternalAsync(
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? partnerGroupId,
        Guid? partnerId,
        DateTime referenceDate)
    {
        // Priority 1: Try exact match with PartnerId
        if (partnerId.HasValue)
        {
            var partnerRate = await _dbContext.GponPartnerJobRates
                .Where(r => r.PartnerId == partnerId)
                .Where(r => r.OrderTypeId == orderTypeId)
                .Where(r => r.OrderCategoryId == orderCategoryId)
                .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
                .Where(r => r.IsActive)
                .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                           (!r.ValidTo.HasValue || r.ValidTo >= referenceDate))
                .FirstOrDefaultAsync();

            if (partnerRate != null)
            {
                return partnerRate;
            }
        }

        // Priority 2: Try PartnerGroupId match
        if (partnerGroupId.HasValue)
        {
            var groupRate = await _dbContext.GponPartnerJobRates
                .Where(r => r.PartnerGroupId == partnerGroupId)
                .Where(r => r.PartnerId == null) // Only group-level rates
                .Where(r => r.OrderTypeId == orderTypeId)
                .Where(r => r.OrderCategoryId == orderCategoryId)
                .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
                .Where(r => r.IsActive)
                .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                           (!r.ValidTo.HasValue || r.ValidTo >= referenceDate))
                .FirstOrDefaultAsync();

            if (groupRate != null)
            {
                return groupRate;
            }
        }

        // Priority 3: System default (no partner filter)
        return await _dbContext.GponPartnerJobRates
            .Where(r => r.PartnerGroupId == partnerGroupId || !partnerGroupId.HasValue)
            .Where(r => r.OrderTypeId == orderTypeId)
            .Where(r => r.OrderCategoryId == orderCategoryId)
            .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
            .Where(r => r.IsActive)
            .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                       (!r.ValidTo.HasValue || r.ValidTo >= referenceDate))
            .FirstOrDefaultAsync();
    }

    private async Task<GponSiCustomRate?> ResolveGponCustomRateAsync(
        Guid serviceInstallerId,
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? partnerGroupId,
        DateTime referenceDate)
    {
        return await _dbContext.GponSiCustomRates
            .Where(r => r.ServiceInstallerId == serviceInstallerId)
            .Where(r => r.OrderTypeId == orderTypeId)
            .Where(r => r.OrderCategoryId == orderCategoryId)
            .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
            .Where(r => !partnerGroupId.HasValue || r.PartnerGroupId == partnerGroupId || r.PartnerGroupId == null)
            .Where(r => r.IsActive)
            .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                       (!r.ValidTo.HasValue || r.ValidTo >= referenceDate))
            .FirstOrDefaultAsync();
    }

    private async Task<GponSiJobRate?> ResolveGponPayoutRateInternalAsync(
        Guid? companyId,
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        string siLevel,
        Guid? partnerGroupId,
        DateTime referenceDate)
    {
        IQueryable<GponSiJobRate> baseFilter;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            // Explicit company-scoped read (defense-in-depth): do not rely on ambient tenant.
            baseFilter = _dbContext.GponSiJobRates
                .IgnoreQueryFilters()
                .Where(r => r.CompanyId == companyId.Value)
                .Where(r => r.OrderTypeId == orderTypeId)
                .Where(r => r.OrderCategoryId == orderCategoryId)
                .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
                .Where(r => r.SiLevel == siLevel)
                .Where(r => r.IsActive)
                .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                            (!r.ValidTo.HasValue || r.ValidTo >= referenceDate));
        }
        else
        {
            baseFilter = _dbContext.GponSiJobRates
                .Where(r => r.OrderTypeId == orderTypeId)
                .Where(r => r.OrderCategoryId == orderCategoryId)
                .Where(r => !installationMethodId.HasValue || r.InstallationMethodId == installationMethodId || r.InstallationMethodId == null)
                .Where(r => r.SiLevel == siLevel)
                .Where(r => r.IsActive)
                .Where(r => (!r.ValidFrom.HasValue || r.ValidFrom <= referenceDate) &&
                            (!r.ValidTo.HasValue || r.ValidTo >= referenceDate));
        }

        // Priority 1: Try PartnerGroup-specific rate
        if (partnerGroupId.HasValue)
        {
            var groupRate = await baseFilter
                .Where(r => r.PartnerGroupId == partnerGroupId)
                .FirstOrDefaultAsync();

            if (groupRate != null)
            {
                return groupRate;
            }
        }

        // Priority 2: System default (no partner group filter)
        return await baseFilter
            .Where(r => r.PartnerGroupId == null)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Resolve payout from Base Work Rate using Rate Group mapping and fallback hierarchy (Phase 3).
    /// Order: (a) RG+Cat+InstM+Subtype, (b) RG+Cat+InstM, (c) RG+Cat+Subtype, (d) RG+Cat, (e) RG.
    /// Results are cached to avoid DB hits on each payout.
    /// </summary>
    private async Task<BaseWorkRate?> ResolveBaseWorkRateAsync(
        Guid? companyId,
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        DateTime referenceDate)
    {
        var (parentOrderTypeId, orderSubtypeId) = await GetOrderTypeParentAndSubtypeAsync(orderTypeId);
        if (parentOrderTypeId == null)
        {
            return null;
        }

        var rateGroupId = await GetRateGroupIdFromMappingAsync(companyId, parentOrderTypeId.Value, orderSubtypeId);
        if (rateGroupId == null)
        {
            return null;
        }

        var serviceProfileId = await ResolveServiceProfileIdForOrderCategoryAsync(companyId, orderCategoryId);
        var dateOnly = referenceDate.Date;
        var serviceProfileKey = serviceProfileId.HasValue ? serviceProfileId.Value.ToString("N") : "n";
        var cacheKey = $"{BaseWorkRateCacheKeyPrefix}{companyId}:{rateGroupId}:{orderCategoryId}:{serviceProfileKey}:{installationMethodId}:{orderSubtypeId}:{dateOnly:yyyy-MM-dd}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = BaseWorkRateCacheTtl;
            return await ResolveBaseWorkRateFromDbAsync(companyId, rateGroupId.Value, orderCategoryId, installationMethodId, orderSubtypeId, referenceDate);
        });
    }

    private async Task<BaseWorkRate?> ResolveBaseWorkRateFromDbAsync(
        Guid? companyId,
        Guid rateGroupId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? orderSubtypeId,
        DateTime referenceDate)
    {
        var query = _dbContext.BaseWorkRates
            .Where(r => r.RateGroupId == rateGroupId && r.IsActive && !r.IsDeleted)
            .Where(r => (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= referenceDate) &&
                        (!r.EffectiveTo.HasValue || r.EffectiveTo >= referenceDate));
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(r => r.CompanyId == companyId.Value || r.CompanyId == null);
        }
        else
        {
            query = query.Where(r => r.CompanyId == null);
        }

        // Precedence: 1) exact OrderCategoryId, 2) ServiceProfileId (from OrderCategory mapping), 3) broad (both null).
        var candidates = await query.ToListAsync();

        var withSubtype = orderSubtypeId.HasValue;
        var withInstM = installationMethodId.HasValue;

        // --- 1) Exact OrderCategoryId match (takes precedence over profile) ---
        // (a) RG+Cat+InstM+Subtype, (b) RG+Cat+InstM, (c) RG+Cat+Subtype, (d) RG+Cat
        if (withSubtype && withInstM)
        {
            var match = candidates
                .Where(r => r.OrderCategoryId == orderCategoryId && r.ServiceProfileId == null && r.InstallationMethodId == installationMethodId && r.OrderSubtypeId == orderSubtypeId)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();
            if (match != null) return match;
        }
        if (withInstM)
        {
            var match = candidates
                .Where(r => r.OrderCategoryId == orderCategoryId && r.ServiceProfileId == null && r.InstallationMethodId == installationMethodId && r.OrderSubtypeId == null)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();
            if (match != null) return match;
        }
        if (withSubtype)
        {
            var match = candidates
                .Where(r => r.OrderCategoryId == orderCategoryId && r.ServiceProfileId == null && r.InstallationMethodId == null && r.OrderSubtypeId == orderSubtypeId)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();
            if (match != null) return match;
        }
        {
            var match = candidates
                .Where(r => r.OrderCategoryId == orderCategoryId && r.ServiceProfileId == null && r.InstallationMethodId == null && r.OrderSubtypeId == null)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();
            if (match != null) return match;
        }

        // --- 2) ServiceProfileId match (when no exact category row) ---
        var serviceProfileId = await ResolveServiceProfileIdForOrderCategoryAsync(companyId, orderCategoryId);
        if (serviceProfileId.HasValue)
        {
            if (withSubtype && withInstM)
            {
                var match = candidates
                    .Where(r => r.OrderCategoryId == null && r.ServiceProfileId == serviceProfileId && r.InstallationMethodId == installationMethodId && r.OrderSubtypeId == orderSubtypeId)
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();
                if (match != null) return match;
            }
            if (withInstM)
            {
                var match = candidates
                    .Where(r => r.OrderCategoryId == null && r.ServiceProfileId == serviceProfileId && r.InstallationMethodId == installationMethodId && r.OrderSubtypeId == null)
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();
                if (match != null) return match;
            }
            if (withSubtype)
            {
                var match = candidates
                    .Where(r => r.OrderCategoryId == null && r.ServiceProfileId == serviceProfileId && r.InstallationMethodId == null && r.OrderSubtypeId == orderSubtypeId)
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();
                if (match != null) return match;
            }
            {
                var match = candidates
                    .Where(r => r.OrderCategoryId == null && r.ServiceProfileId == serviceProfileId && r.InstallationMethodId == null && r.OrderSubtypeId == null)
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();
                if (match != null) return match;
            }
        }

        // --- 3) Broad fallback (no category, no profile) ---
        return candidates
            .Where(r => r.OrderCategoryId == null && r.ServiceProfileId == null && r.InstallationMethodId == null && r.OrderSubtypeId == null)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();
    }

    /// <summary>Resolves ServiceProfileId for an Order Category via OrderCategoryServiceProfile mapping (company-scoped).</summary>
    private async Task<Guid?> ResolveServiceProfileIdForOrderCategoryAsync(Guid? companyId, Guid orderCategoryId)
    {
        var query = _dbContext.OrderCategoryServiceProfiles
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.OrderCategoryId == orderCategoryId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(m => m.CompanyId == companyId.Value || m.CompanyId == null);
        else
            query = query.Where(m => m.CompanyId == null);
        var mapping = await query.Select(m => new { m.ServiceProfileId }).FirstOrDefaultAsync();
        return mapping?.ServiceProfileId;
    }

    private async Task<(Guid? ParentOrderTypeId, Guid? OrderSubtypeId)> GetOrderTypeParentAndSubtypeAsync(Guid orderTypeId)
    {
        var orderType = await _dbContext.OrderTypes
            .AsNoTracking()
            .Where(t => t.Id == orderTypeId && !t.IsDeleted)
            .Select(t => new { t.Id, t.ParentOrderTypeId })
            .FirstOrDefaultAsync();
        if (orderType == null)
        {
            return (null, null);
        }
        if (orderType.ParentOrderTypeId.HasValue)
        {
            return (orderType.ParentOrderTypeId, orderType.Id);
        }
        return (orderType.Id, (Guid?)null);
    }

    private async Task<Guid?> GetRateGroupIdFromMappingAsync(Guid? companyId, Guid parentOrderTypeId, Guid? orderSubtypeId)
    {
        var query = _dbContext.OrderTypeSubtypeRateGroups
            .AsNoTracking()
            .Where(m => m.OrderTypeId == parentOrderTypeId && m.OrderSubtypeId == orderSubtypeId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(m => m.CompanyId == companyId.Value || m.CompanyId == null);
        }
        else
        {
            query = query.Where(m => m.CompanyId == null);
        }
        var mapping = await query.Select(m => new { m.RateGroupId }).FirstOrDefaultAsync();
        return mapping?.RateGroupId;
    }

    /// <summary>
    /// Apply rate modifiers in order: InstallationMethod → SITier → Partner.
    /// For each type, at most one modifier is applied (highest Priority that matches). Returns adjusted amount, steps, and trace.
    /// </summary>
    private async Task<(decimal AdjustedAmount, List<string> Steps, List<ModifierTraceItemDto> Trace)> ApplyRateModifiersAsync(
        Guid? companyId,
        decimal amount,
        Guid? installationMethodId,
        string? siLevel,
        Guid? partnerGroupId)
    {
        var steps = new List<string>();
        var trace = new List<ModifierTraceItemDto>();
        var current = amount;

        var modifiers = await _dbContext.RateModifiers
            .AsNoTracking()
            .Where(m => m.IsActive && !m.IsDeleted)
            .Where(m => !companyId.HasValue || companyId.Value == Guid.Empty || m.CompanyId == companyId.Value || m.CompanyId == null)
            .ToListAsync();

        if (modifiers.Count == 0)
        {
            steps.Add("No rate modifiers configured or matched.");
            return (current, steps, trace);
        }

        // InstallationMethod: match ModifierValueId == installationMethodId
        var instM = modifiers
            .Where(m => m.ModifierType == RateModifierType.InstallationMethod && installationMethodId.HasValue && m.ModifierValueId == installationMethodId.Value)
            .OrderByDescending(m => m.Priority)
            .FirstOrDefault();
        if (instM != null)
        {
            var before = current;
            current = ApplyOneModifier(current, instM);
            var op = instM.AdjustmentType == RateModifierAdjustmentType.Add ? "Add" : "Multiply";
            steps.Add($"Applied Installation Method modifier: {op} {instM.AdjustmentValue} → {current} {Currency}");
            trace.Add(new ModifierTraceItemDto { ModifierType = "InstallationMethod", Operation = op, Value = instM.AdjustmentValue, AmountBefore = before, AmountAfter = current });
        }

        // SITier: match ModifierValueString == siLevel (e.g. "Junior", "Senior")
        var siTier = modifiers
            .Where(m => m.ModifierType == RateModifierType.SITier && !string.IsNullOrEmpty(siLevel) && m.ModifierValueString == siLevel)
            .OrderByDescending(m => m.Priority)
            .FirstOrDefault();
        if (siTier != null)
        {
            var before = current;
            current = ApplyOneModifier(current, siTier);
            var op = siTier.AdjustmentType == RateModifierAdjustmentType.Add ? "Add" : "Multiply";
            steps.Add($"Applied SI Tier modifier: {op} {siTier.AdjustmentValue} → {current} {Currency}");
            trace.Add(new ModifierTraceItemDto { ModifierType = "SITier", Operation = op, Value = siTier.AdjustmentValue, AmountBefore = before, AmountAfter = current });
        }

        // Partner: match ModifierValueId == partnerGroupId
        var partner = modifiers
            .Where(m => m.ModifierType == RateModifierType.Partner && partnerGroupId.HasValue && m.ModifierValueId == partnerGroupId.Value)
            .OrderByDescending(m => m.Priority)
            .FirstOrDefault();
        if (partner != null)
        {
            var before = current;
            current = ApplyOneModifier(current, partner);
            var op = partner.AdjustmentType == RateModifierAdjustmentType.Add ? "Add" : "Multiply";
            steps.Add($"Applied Partner modifier: {op} {partner.AdjustmentValue} → {current} {Currency}");
            trace.Add(new ModifierTraceItemDto { ModifierType = "Partner", Operation = op, Value = partner.AdjustmentValue, AmountBefore = before, AmountAfter = current });
        }

        if (trace.Count == 0)
            steps.Add("No rate modifiers matched for this context.");
        return (current, steps, trace);
    }

    private const string Currency = "MYR";

    private static decimal ApplyOneModifier(decimal amount, RateModifier m)
    {
        return m.AdjustmentType == RateModifierAdjustmentType.Add
            ? amount + m.AdjustmentValue
            : amount * m.AdjustmentValue;
    }

    private void SetResultFromRateCardLine(UniversalRateResolutionResult result, RateCardLine line, string source)
    {
        result.Success = true;
        result.RateAmount = line.RateAmount;
        result.RateSource = source;
        result.RateCardId = line.RateCardId;
        result.RateCardLineId = line.Id;
        result.RateId = line.Id;
        result.UnitOfMeasure = line.UnitOfMeasure.ToString();
        result.Currency = line.Currency;
        result.ResolutionSteps.Add($"Rate found: {line.RateAmount} {line.Currency} from {source} (ID: {line.Id})");
    }

    #endregion
}

