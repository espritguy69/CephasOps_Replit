using System.Text.Json;
using CephasOps.Application.Common;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Creates and reads immutable payout snapshots. Snapshot is captured after resolution; never updated.
/// </summary>
public class OrderPayoutSnapshotService : IOrderPayoutSnapshotService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _context;
    private readonly IOrderProfitabilityService _orderProfitabilityService;
    private readonly ILogger<OrderPayoutSnapshotService> _logger;

    public OrderPayoutSnapshotService(
        ApplicationDbContext context,
        IOrderProfitabilityService orderProfitabilityService,
        ILogger<OrderPayoutSnapshotService> logger)
    {
        _context = context;
        _orderProfitabilityService = orderProfitabilityService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task CreateSnapshotForOrderIfEligibleAsync(Guid orderId, CancellationToken cancellationToken = default, string? provenance = null)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreatePayoutSnapshot");
        var exists = await _context.OrderPayoutSnapshots.AnyAsync(s => s.OrderId == orderId, cancellationToken);
        if (exists)
        {
            return;
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.CompanyId, o.AssignedSiId, o.AppointmentDate })
            .FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            _logger.LogDebug("Order {OrderId} not found; skipping payout snapshot", orderId);
            return;
        }

        FinancialIsolationGuard.RequireCompany(order.CompanyId, "CreatePayoutSnapshot");

        var result = await _orderProfitabilityService.GetOrderPayoutBreakdownAsync(
            orderId, order.CompanyId, order.AppointmentDate, cancellationToken);
        if (result == null || !result.Success || !result.PayoutAmount.HasValue)
        {
            _logger.LogDebug("No successful payout resolution for order {OrderId}; skipping snapshot", orderId);
            return;
        }

        var effectiveProvenance = string.IsNullOrEmpty(provenance) ? SnapshotProvenance.NormalFlow : provenance;
        var snapshot = MapToSnapshot(orderId, order.CompanyId, order.AssignedSiId, result, effectiveProvenance);
        _context.OrderPayoutSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Payout snapshot created. tenantId={TenantId}, orderId={OrderId}, snapshotId={SnapshotId}, operation=CreatePayoutSnapshot, success=true", order.CompanyId, orderId, snapshot.Id);
    }

    /// <inheritdoc />
    public async Task<OrderPayoutSnapshotDto?> GetSnapshotByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;
        var entity = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
        if (entity == null) return null;
        if (entity.CompanyId.HasValue && entity.CompanyId.Value != effectiveCompanyId.Value)
            return null;
        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<OrderPayoutSnapshotResponseDto> GetPayoutWithSnapshotOrLiveAsync(
        Guid orderId, Guid? companyId, DateTime? referenceDate, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId.HasValue && companyId.Value != Guid.Empty ? companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new OrderPayoutSnapshotResponseDto { Source = "None", Result = GponRateResolutionResult.Failed("Company context is required for payout resolution.") };

        var snapshot = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);

        if (snapshot != null)
        {
            FinancialIsolationGuard.RequireSameCompany(snapshot.CompanyId, effectiveCompanyId, "Snapshot", "Request", snapshot.Id, null);
            var result = MapSnapshotToResolutionResult(snapshot);
            return new OrderPayoutSnapshotResponseDto { Source = "Snapshot", Result = result };
        }

        var live = await _orderProfitabilityService.GetOrderPayoutBreakdownAsync(orderId, effectiveCompanyId, referenceDate, cancellationToken);
        var liveResult = live ?? GponRateResolutionResult.Failed("Order not found or payout could not be resolved.");
        return new OrderPayoutSnapshotResponseDto { Source = "Live", Result = liveResult };
    }

    private static OrderPayoutSnapshot MapToSnapshot(
        Guid orderId, Guid? companyId, Guid? installerId, GponRateResolutionResult result, string provenance)
    {
        var details = result.MatchedRuleDetails;
        return new OrderPayoutSnapshot
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CompanyId = companyId,
            InstallerId = installerId,
            RateGroupId = details?.RateGroupId,
            BaseWorkRateId = details?.BaseWorkRateId,
            ServiceProfileId = details?.ServiceProfileId,
            CustomRateId = details?.CustomRateId,
            LegacyRateId = details?.LegacyRateId,
            BaseAmount = result.BaseAmountBeforeModifiers,
            ModifierTraceJson = result.ModifierTrace != null && result.ModifierTrace.Count > 0
                ? JsonSerializer.Serialize(result.ModifierTrace, JsonOptions)
                : null,
            FinalPayout = result.PayoutAmount ?? 0,
            Currency = result.Currency ?? "MYR",
            ResolutionMatchLevel = result.ResolutionMatchLevel,
            PayoutPath = result.PayoutPath,
            ResolutionTraceJson = SerializeResolutionTrace(result.ResolutionSteps, result.Warnings),
            CalculatedAt = result.ResolvedAt,
            Provenance = provenance
        };
    }

    private static string? SerializeResolutionTrace(List<string>? steps, List<string>? warnings)
    {
        if ((steps == null || steps.Count == 0) && (warnings == null || warnings.Count == 0))
            return null;
        var obj = new { steps = steps ?? new List<string>(), warnings = warnings ?? new List<string>() };
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    private static OrderPayoutSnapshotDto MapToDto(OrderPayoutSnapshot s)
    {
        return new OrderPayoutSnapshotDto
        {
            Id = s.Id,
            OrderId = s.OrderId,
            CompanyId = s.CompanyId,
            InstallerId = s.InstallerId,
            RateGroupId = s.RateGroupId,
            BaseWorkRateId = s.BaseWorkRateId,
            ServiceProfileId = s.ServiceProfileId,
            CustomRateId = s.CustomRateId,
            LegacyRateId = s.LegacyRateId,
            BaseAmount = s.BaseAmount,
            ModifierTraceJson = s.ModifierTraceJson,
            FinalPayout = s.FinalPayout,
            Currency = s.Currency,
            ResolutionMatchLevel = s.ResolutionMatchLevel,
            PayoutPath = s.PayoutPath,
            ResolutionTraceJson = s.ResolutionTraceJson,
            CalculatedAt = s.CalculatedAt,
            Provenance = s.Provenance
        };
    }

    private static GponRateResolutionResult MapSnapshotToResolutionResult(OrderPayoutSnapshot s)
    {
        var result = new GponRateResolutionResult
        {
            Success = true,
            PayoutAmount = s.FinalPayout,
            PayoutSource = s.PayoutPath == "CustomOverride" ? "GponSiCustomRate" : s.PayoutPath == "Legacy" ? "GponSiJobRate" : "BaseWorkRate",
            PayoutRateId = s.CustomRateId ?? s.BaseWorkRateId ?? s.LegacyRateId,
            Currency = s.Currency,
            ResolvedAt = s.CalculatedAt,
            BaseAmountBeforeModifiers = s.BaseAmount,
            PayoutPath = s.PayoutPath,
            ResolutionMatchLevel = s.ResolutionMatchLevel,
            MatchedRuleDetails = new MatchedRuleDetailsDto
            {
                RateGroupId = s.RateGroupId,
                BaseWorkRateId = s.BaseWorkRateId,
                ServiceProfileId = s.ServiceProfileId,
                CustomRateId = s.CustomRateId,
                LegacyRateId = s.LegacyRateId
            }
        };

        if (!string.IsNullOrEmpty(s.ModifierTraceJson))
        {
            try
            {
                result.ModifierTrace = JsonSerializer.Deserialize<List<ModifierTraceItemDto>>(s.ModifierTraceJson, JsonOptions) ?? new List<ModifierTraceItemDto>();
            }
            catch
            {
                result.ModifierTrace = new List<ModifierTraceItemDto>();
            }
        }
        else
        {
            result.ModifierTrace = new List<ModifierTraceItemDto>();
        }

        if (!string.IsNullOrEmpty(s.ResolutionTraceJson))
        {
            try
            {
                var trace = JsonSerializer.Deserialize<JsonElement>(s.ResolutionTraceJson);
                if (trace.TryGetProperty("steps", out var stepsEl) && stepsEl.ValueKind == JsonValueKind.Array)
                    result.ResolutionSteps = stepsEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                if (trace.TryGetProperty("warnings", out var warnEl) && warnEl.ValueKind == JsonValueKind.Array)
                    result.Warnings = warnEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            }
            catch
            {
                result.ResolutionSteps = new List<string>();
                result.Warnings = new List<string>();
            }
        }

        result.ResolutionSteps ??= new List<string>();
        result.Warnings ??= new List<string>();
        return result;
    }
}
