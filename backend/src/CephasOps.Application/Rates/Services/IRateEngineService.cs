using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Rate Engine Service interface.
/// Provides centralized rate resolution for all verticals.
/// Per RATE_ENGINE.md specification.
/// 
/// Resolution priority:
/// 1. CustomRate (per-staff/per-SI overrides)
/// 2. RateCardLine with PartnerId match
/// 3. RateCardLine with PartnerGroupId match
/// 4. RateCardLine with no partner filter (system default)
/// </summary>
public interface IRateEngineService
{
    /// <summary>
    /// Resolve GPON job rates (revenue and payout).
    /// Per GPON_RATECARDS.md specification.
    /// </summary>
    /// <param name="request">Rate resolution request with order/installation details</param>
    /// <returns>Rate resolution result with revenue and payout amounts</returns>
    Task<GponRateResolutionResult> ResolveGponRatesAsync(GponRateResolutionRequest request);

    /// <summary>
    /// Resolve GPON revenue rate only (what Cephas earns from partner).
    /// </summary>
    Task<decimal?> GetGponRevenueRateAsync(
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? partnerGroupId,
        Guid? partnerId = null,
        DateTime? referenceDate = null);

    /// <summary>
    /// Resolve GPON payout rate only (what SI earns).
    /// Priority: Custom Rate → Default Payout Rate by SI Level
    /// </summary>
    Task<decimal?> GetGponPayoutRateAsync(
        Guid orderTypeId,
        Guid orderCategoryId,
        Guid? installationMethodId,
        Guid? serviceInstallerId,
        string? siLevel,
        Guid? partnerGroupId = null,
        DateTime? referenceDate = null);

    /// <summary>
    /// Resolve universal rate using flexible dimensions.
    /// For NWO, CWO, Barbershop, Travel, Spa, etc.
    /// </summary>
    Task<UniversalRateResolutionResult> ResolveUniversalRateAsync(UniversalRateResolutionRequest request);

    /// <summary>
    /// Check if a custom rate exists for a specific SI/user.
    /// </summary>
    Task<bool> HasCustomRateAsync(
        Guid userId,
        string? dimension1 = null,
        string? dimension2 = null,
        string? dimension3 = null,
        string? dimension4 = null,
        DateTime? referenceDate = null);
}

