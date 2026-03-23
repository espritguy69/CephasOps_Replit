using CephasOps.Application.Common;
using CephasOps.Application.Events;
using CephasOps.Application.Payroll.DTOs;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Payroll.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Payroll.Services;

/// <summary>
/// Payroll service implementation.
/// Per RATE_ENGINE.md: Uses RateEngineService for rate resolution instead of hardcoded logic.
/// Per PAYROLL_MODULE.md: Calculates KPI results and applies OnTimeBonus/LatePenalty adjustments.
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _context;
    private readonly IRateEngineService _rateEngineService;
    private readonly IKpiProfileService _kpiProfileService;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        ApplicationDbContext context,
        IRateEngineService rateEngineService,
        IKpiProfileService kpiProfileService,
        ILogger<PayrollService> logger,
        IEventBus? eventBus = null)
    {
        _context = context;
        _rateEngineService = rateEngineService;
        _kpiProfileService = kpiProfileService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid companyId, string? year = null, string? status = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payroll periods for company {CompanyId}", companyId);

        var query = _context.PayrollPeriods
            .Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(p => p.Period.StartsWith(year));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var periods = await query
            .OrderByDescending(p => p.Period)
            .ToListAsync(cancellationToken);

        return periods.Select(MapToPayrollPeriodDto).ToList();
    }

    public async Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payroll period {PeriodId} for company {CompanyId}", id, companyId);

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        return period != null ? MapToPayrollPeriodDto(period) : null;
    }

    public async Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating payroll period for company {CompanyId}", companyId);

        var exists = await _context.PayrollPeriods
            .AnyAsync(p => p.CompanyId == companyId && p.Period == dto.Period, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Payroll period '{dto.Period}' already exists");
        }

        var period = new PayrollPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Period = dto.Period,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Status = "Draft",
            IsLocked = false,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToPayrollPeriodDto(period);
    }

    public async Task<List<PayrollRunDto>> GetPayrollRunsAsync(Guid companyId, Guid? periodId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payroll runs for company {CompanyId}", companyId);

        var query = _context.PayrollRuns
            .Include(r => r.PayrollLines)
            .Where(r => r.CompanyId == companyId);

        if (periodId.HasValue)
        {
            query = query.Where(r => r.PayrollPeriodId == periodId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.PeriodStart >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.PeriodEnd <= toDate.Value);
        }

        var runs = await query
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(cancellationToken);

        return runs.Select(r => MapToPayrollRunDto(r)).ToList();
    }

    public async Task<PayrollRunDto?> GetPayrollRunByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payroll run {RunId} for company {CompanyId}", id, companyId);

        var run = await _context.PayrollRuns
            .Include(r => r.PayrollLines)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        return run != null ? MapToPayrollRunDto(run) : null;
    }

    public async Task<PayrollRunDto> CreatePayrollRunAsync(CreatePayrollRunDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreatePayrollRun");
        FinancialIsolationGuard.RequireCompany(companyId, "CreatePayrollRun");

        _logger.LogInformation("Creating payroll run for company {CompanyId}", companyId);

        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PayrollPeriodId = dto.PayrollPeriodId!.Value,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Status = "Draft",
            TotalAmount = 0,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PayrollRuns.Add(run);

        // Calculate payroll using RateEngineService
        // Per RATE_ENGINE.md: Rate resolution priority is Custom Rate → Payout Rate → Revenue Rate
        var completedOrders = await _context.Orders
            .Where(o => o.CompanyId == companyId)
            .Where(o => o.Status == "Completed" || o.Status == "DocketsReceived" || o.Status == "DocketsUploaded")
            .Where(o => o.AppointmentDate >= dto.PeriodStart && o.AppointmentDate <= dto.PeriodEnd)
            .Where(o => o.AssignedSiId.HasValue)
            .Where(o => o.PayrollPeriodId == null) // Not yet processed
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {OrderCount} completed orders for payroll processing", completedOrders.Count);

        // Group by SI
        var ordersBySi = completedOrders.GroupBy(o => o.AssignedSiId!.Value);
        var siIds = ordersBySi.Select(g => g.Key).ToList();

        // Load SI details for level lookup
        var serviceInstallers = await _context.ServiceInstallers
            .Where(si => siIds.Contains(si.Id))
            .ToDictionaryAsync(si => si.Id, si => si, cancellationToken);

        // Load SI rate plans for bonus/penalty lookup
        var siRatePlans = await _context.SiRatePlans
            .Where(rp => siIds.Contains(rp.ServiceInstallerId) && rp.IsActive)
            .Where(rp => !rp.EffectiveFrom.HasValue || rp.EffectiveFrom <= dto.PeriodEnd)
            .Where(rp => !rp.EffectiveTo.HasValue || rp.EffectiveTo >= dto.PeriodStart)
            .ToDictionaryAsync(rp => rp.ServiceInstallerId, rp => rp, cancellationToken);

        // Load partner info for orders
        var partnerIds = completedOrders.Select(o => o.PartnerId).Distinct().ToList();
        var partners = await _context.Partners
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        // Load order types for name lookup
        var orderTypeIds = completedOrders.Select(o => o.OrderTypeId).Distinct().ToList();
        var orderTypes = await _context.OrderTypes
            .Where(ot => orderTypeIds.Contains(ot.Id))
            .ToDictionaryAsync(ot => ot.Id, ot => ot, cancellationToken);

        decimal totalRunAmount = 0;

        foreach (var siGroup in ordersBySi)
        {
            var siId = siGroup.Key;
            var siOrders = siGroup.ToList();
            var si = serviceInstallers.GetValueOrDefault(siId);
            var siLevel = si?.SiLevel ?? Domain.ServiceInstallers.Enums.InstallerLevel.Junior; // Default to Junior if not specified
            var siRatePlan = siRatePlans.GetValueOrDefault(siId);

            decimal siTotalPay = 0;
            decimal siTotalAdjustments = 0;
            int jobCount = 0;

            foreach (var order in siOrders)
            {
                if (!order.OrderCategoryId.HasValue || order.OrderCategoryId.Value == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Order category must be set before payroll calculation. OrderId={order.Id}, ServiceId={order.ServiceId}.");
                }

                // Get partner group for rate lookup
                var partner = partners.GetValueOrDefault(order.PartnerId);
                var partnerGroupId = partner?.GroupId; // Partner.GroupId is the FK to PartnerGroup

                // Get order type
                var orderType = orderTypes.GetValueOrDefault(order.OrderTypeId);
                var orderTypeName = orderType?.Name ?? order.OrderTypeId.ToString();
                var orderTypeCode = orderType?.Code ?? string.Empty;

                // Use RateEngineService for rate resolution (companyId scopes rate lookup to same company as payroll run)
                var rateResult = await _rateEngineService.ResolveGponRatesAsync(new GponRateResolutionRequest
                {
                    CompanyId = companyId,
                    OrderTypeId = order.OrderTypeId,
                    OrderCategoryId = order.OrderCategoryId!.Value,
                    InstallationMethodId = order.InstallationMethodId,
                    PartnerGroupId = partnerGroupId,
                    PartnerId = order.PartnerId,
                    ServiceInstallerId = siId,
                    SiLevel = siLevel.ToString(),
                    ReferenceDate = order.AppointmentDate
                });

                decimal basePayoutAmount = rateResult.PayoutAmount ?? 0;

                // Calculate KPI result using KpiProfileService
                string kpiResult = "OnTime";
                decimal kpiAdjustment = 0;

                try
                {
                    var kpiEvaluation = await _kpiProfileService.EvaluateOrderAsync(order.Id, companyId, cancellationToken);
                    if (kpiEvaluation != null)
                    {
                        kpiResult = kpiEvaluation.KpiResult; // "OnTime", "Late", or "ExceededSla"
                        
                        // Apply KPI adjustments from SI rate plan
                        if (siRatePlan != null)
                        {
                            switch (kpiResult)
                            {
                                case "OnTime":
                                    kpiAdjustment = siRatePlan.OnTimeBonus ?? 0;
                                    break;
                                case "Late":
                                case "ExceededSla":
                                    kpiAdjustment = -(siRatePlan.LatePenalty ?? 0); // Negative for penalty
                                    break;
                            }
                        }
                        
                        _logger.LogDebug("KPI evaluation for order {OrderId}: {KpiResult}, Adjustment: {Adjustment}",
                            order.Id, kpiResult, kpiAdjustment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to evaluate KPI for order {OrderId}, defaulting to OnTime", order.Id);
                }

                decimal finalPay = basePayoutAmount + kpiAdjustment;

                // Create job earning record
                var earningRecord = new JobEarningRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    OrderId = order.Id,
                    ServiceInstallerId = siId,
                    PayrollRunId = run.Id,
                    OrderTypeId = order.OrderTypeId,
                    OrderTypeCode = orderTypeCode,
                    OrderTypeName = orderTypeName,
                    // JobType is deprecated - no longer populated (use OrderTypeId/OrderTypeCode instead)
                    KpiResult = kpiResult,
                    BaseRate = basePayoutAmount,
                    KpiAdjustment = kpiAdjustment,
                    FinalPay = finalPay,
                    Period = dto.PeriodStart.ToString("yyyy-MM"),
                    Status = "Pending",
                    RateSource = rateResult.PayoutSource,
                    RateId = rateResult.PayoutRateId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobEarningRecords.Add(earningRecord);

                // Mark order as processed
                order.PayrollPeriodId = dto.PayrollPeriodId;

                siTotalPay += finalPay;
                siTotalAdjustments += kpiAdjustment;
                jobCount++;
            }

            // Create payroll line for this SI
            if (jobCount > 0)
            {
                var payrollLine = new PayrollLine
                {
                    Id = Guid.NewGuid(),
                    PayrollRunId = run.Id,
                    ServiceInstallerId = siId,
                    TotalJobs = jobCount,
                    TotalPay = siTotalPay - siTotalAdjustments, // Base pay without adjustments
                    Adjustments = siTotalAdjustments, // KPI adjustments (bonuses - penalties)
                    NetPay = siTotalPay, // Final amount including adjustments
                    CreatedAt = DateTime.UtcNow
                };

                _context.PayrollLines.Add(payrollLine);
                totalRunAmount += siTotalPay;
            }
        }

        run.TotalAmount = totalRunAmount;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payroll run created: {RunId}, Total: {TotalAmount} MYR, Orders: {OrderCount}", 
            run.Id, totalRunAmount, completedOrders.Count);

        if (_eventBus != null)
        {
            var evt = new PayrollCalculatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                CompanyId = companyId,
                TriggeredByUserId = userId,
                PayrollRunId = run.Id,
                PayrollPeriodId = run.PayrollPeriodId,
                PeriodStart = run.PeriodStart,
                PeriodEnd = run.PeriodEnd,
                TotalAmount = totalRunAmount,
                Status = run.Status ?? "Draft"
            };
            await _eventBus.PublishAsync(evt, cancellationToken);
        }

        return MapToPayrollRunDto(run);
    }

    public async Task<PayrollRunDto> FinalizePayrollRunAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finalizing payroll run {RunId} for company {CompanyId}", id, companyId);

        var run = await _context.PayrollRuns
            .Include(r => r.PayrollLines)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

        if (run == null)
        {
            throw new KeyNotFoundException($"Payroll run with ID {id} not found");
        }

        if (run.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft payroll runs can be finalized");
        }

        run.Status = "Final";
        run.FinalizedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToPayrollRunDto(run);
    }

    public async Task<PayrollRunDto> MarkPayrollRunPaidAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking payroll run {RunId} as paid for company {CompanyId}", id, companyId);

        var run = await _context.PayrollRuns
            .Include(r => r.PayrollLines)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

        if (run == null)
        {
            throw new KeyNotFoundException($"Payroll run with ID {id} not found");
        }

        run.Status = "Paid";
        run.PaidAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToPayrollRunDto(run);
    }

    public async Task<List<JobEarningRecordDto>> GetJobEarningRecordsAsync(Guid companyId, Guid? siId = null, string? period = null, Guid? orderId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting job earning records for company {CompanyId}", companyId);

        var query = _context.JobEarningRecords
            .Where(j => j.CompanyId == companyId);

        if (siId.HasValue)
        {
            query = query.Where(j => j.ServiceInstallerId == siId.Value);
        }

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(j => j.Period == period);
        }

        if (orderId.HasValue)
        {
            query = query.Where(j => j.OrderId == orderId.Value);
        }

        var records = await query
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

        // Load related data in batch for performance
        var orderIds = records.Select(r => r.OrderId).Distinct().ToList();
        var siIds = records.Select(r => r.ServiceInstallerId).Distinct().ToList();

        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.ServiceId, o.TicketId, o.AwoNumber, o.ExternalRef })
            .ToListAsync(cancellationToken);

        var serviceInstallers = await _context.ServiceInstallers
            .Where(si => siIds.Contains(si.Id))
            .Select(si => new { si.Id, si.Name })
            .ToListAsync(cancellationToken);

        var orderDict = orders.ToDictionary(o => o.Id);
        var siDict = serviceInstallers.ToDictionary(si => si.Id);

        return records.Select(j =>
        {
            var order = orderDict.GetValueOrDefault(j.OrderId);
            var si = siDict.GetValueOrDefault(j.ServiceInstallerId);

            // Determine OrderUniqueId: prefer ServiceId, then TicketId, then AwoNumber, then ExternalRef
            var orderUniqueId = !string.IsNullOrWhiteSpace(order?.ServiceId) ? order.ServiceId
                : !string.IsNullOrWhiteSpace(order?.TicketId) ? order.TicketId
                : !string.IsNullOrWhiteSpace(order?.AwoNumber) ? order.AwoNumber
                : !string.IsNullOrWhiteSpace(order?.ExternalRef) ? order.ExternalRef
                : string.Empty;

            return new JobEarningRecordDto
            {
                Id = j.Id,
                OrderId = j.OrderId,
                OrderUniqueId = orderUniqueId,
                ServiceInstallerId = j.ServiceInstallerId,
                ServiceInstallerName = si?.Name ?? string.Empty,
                OrderTypeId = j.OrderTypeId,
                OrderTypeCode = j.OrderTypeCode,
                OrderTypeName = j.OrderTypeName,
                // JobType is deprecated - use OrderTypeCode/OrderTypeName instead
#pragma warning disable CS0618 // Type or member is obsolete
                JobType = string.Empty, // Deprecated field - no longer populated
#pragma warning restore CS0618
                KpiResult = j.KpiResult,
                BaseRate = j.BaseRate,
                KpiAdjustment = j.KpiAdjustment,
                FinalPay = j.FinalPay,
                Period = j.Period,
                Status = j.Status,
                ConfirmedAt = j.ConfirmedAt,
                PaidAt = j.PaidAt
            };
        }).ToList();
    }

    public async Task<List<SiRatePlanDto>> GetSiRatePlansAsync(Guid companyId, Guid? siId = null, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SI rate plans for company {CompanyId}", companyId);

        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var query = _context.SiRatePlans.Where(s => s.CompanyId == companyId);

        if (siId.HasValue)
        {
            query = query.Where(s => s.ServiceInstallerId == siId.Value);
        }

        // Include rates with null departmentId when filtering by department
        // (null means department-agnostic rates that apply to all departments)
        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId.Value || s.DepartmentId == null);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var plans = await query
            .OrderBy(s => s.ServiceInstallerId)
            .ToListAsync(cancellationToken);

        // Load related data for display names
        var siIds = plans.Select(p => p.ServiceInstallerId).Distinct().ToList();
        var deptIds = plans.Where(p => p.DepartmentId.HasValue).Select(p => p.DepartmentId!.Value).Distinct().ToList();
        var methodIds = plans.Where(p => p.InstallationMethodId.HasValue).Select(p => p.InstallationMethodId!.Value).Distinct().ToList();

        var installers = await _context.ServiceInstallers.Where(si => siIds.Contains(si.Id)).ToDictionaryAsync(si => si.Id, si => si.Name, cancellationToken);
        var departments = deptIds.Any() ? await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken) : new Dictionary<Guid, string>();
        var methods = methodIds.Any() ? await _context.InstallationMethods.Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken) : new Dictionary<Guid, string>();

        return plans.Select(s => new SiRatePlanDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.DepartmentId.HasValue && departments.ContainsKey(s.DepartmentId.Value) ? departments[s.DepartmentId.Value] : null,
            ServiceInstallerId = s.ServiceInstallerId,
            ServiceInstallerName = installers.ContainsKey(s.ServiceInstallerId) ? installers[s.ServiceInstallerId] : string.Empty,
            InstallationMethodId = s.InstallationMethodId,
            InstallationMethodName = s.InstallationMethodId.HasValue && methods.ContainsKey(s.InstallationMethodId.Value) ? methods[s.InstallationMethodId.Value] : null,
            RateType = s.RateType,
            Level = s.Level,
            PrelaidRate = s.PrelaidRate,
            NonPrelaidRate = s.NonPrelaidRate,
            SduRate = s.SduRate,
            RdfPoleRate = s.RdfPoleRate,
            ActivationRate = s.ActivationRate,
            ModificationRate = s.ModificationRate,
            AssuranceRate = s.AssuranceRate,
            AssuranceRepullRate = s.AssuranceRepullRate,
            FttrRate = s.FttrRate,
            FttcRate = s.FttcRate,
            OnTimeBonus = s.OnTimeBonus,
            LatePenalty = s.LatePenalty,
            ReworkRate = s.ReworkRate,
            IsActive = s.IsActive,
            EffectiveFrom = s.EffectiveFrom,
            EffectiveTo = s.EffectiveTo,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<SiRatePlanDto?> GetSiRatePlanByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SI rate plan {PlanId} for company {CompanyId}", id, companyId);

        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var plan = await _context.SiRatePlans
            .Where(s => s.Id == id && s.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null) return null;

        // Load related data (tenant-safe: scope by plan.CompanyId)
        var cid = plan.CompanyId;
        var installer = cid.HasValue ? await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId && si.CompanyId == cid.Value, cancellationToken) : await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId, cancellationToken);
        var department = plan.DepartmentId.HasValue && cid.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value && d.CompanyId == cid.Value, cancellationToken) : plan.DepartmentId.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value, cancellationToken) : null;
        var method = plan.InstallationMethodId.HasValue && cid.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value && m.CompanyId == cid.Value, cancellationToken) : plan.InstallationMethodId.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value, cancellationToken) : null;

        return new SiRatePlanDto
        {
            Id = plan.Id,
            CompanyId = plan.CompanyId,
            DepartmentId = plan.DepartmentId,
            DepartmentName = department?.Name,
            ServiceInstallerId = plan.ServiceInstallerId,
            ServiceInstallerName = installer?.Name ?? string.Empty,
            InstallationMethodId = plan.InstallationMethodId,
            InstallationMethodName = method?.Name,
            RateType = plan.RateType,
            Level = plan.Level,
            PrelaidRate = plan.PrelaidRate,
            NonPrelaidRate = plan.NonPrelaidRate,
            SduRate = plan.SduRate,
            RdfPoleRate = plan.RdfPoleRate,
            ActivationRate = plan.ActivationRate,
            ModificationRate = plan.ModificationRate,
            AssuranceRate = plan.AssuranceRate,
            AssuranceRepullRate = plan.AssuranceRepullRate,
            FttrRate = plan.FttrRate,
            FttcRate = plan.FttcRate,
            OnTimeBonus = plan.OnTimeBonus,
            LatePenalty = plan.LatePenalty,
            ReworkRate = plan.ReworkRate,
            IsActive = plan.IsActive,
            EffectiveFrom = plan.EffectiveFrom,
            EffectiveTo = plan.EffectiveTo,
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task<SiRatePlanDto> CreateSiRatePlanAsync(CreateSiRatePlanDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating SI rate plan for company {CompanyId}, SI {ServiceInstallerId}", companyId, dto.ServiceInstallerId);

        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var installerQuery = _context.ServiceInstallers.Where(si => si.Id == dto.ServiceInstallerId && si.CompanyId == companyId);
        
        var installerExists = await installerQuery.AnyAsync(cancellationToken);

        if (!installerExists)
        {
            throw new KeyNotFoundException($"Service installer with ID {dto.ServiceInstallerId} not found");
        }

        var plan = new SiRatePlan
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = dto.DepartmentId,
            ServiceInstallerId = dto.ServiceInstallerId,
            InstallationMethodId = dto.InstallationMethodId,
            RateType = dto.RateType,
            Level = dto.Level,
            PrelaidRate = dto.PrelaidRate,
            NonPrelaidRate = dto.NonPrelaidRate,
            SduRate = dto.SduRate,
            RdfPoleRate = dto.RdfPoleRate,
            ActivationRate = dto.ActivationRate,
            ModificationRate = dto.ModificationRate,
            AssuranceRate = dto.AssuranceRate,
            AssuranceRepullRate = dto.AssuranceRepullRate,
            FttrRate = dto.FttrRate,
            FttcRate = dto.FttcRate,
            OnTimeBonus = dto.OnTimeBonus,
            LatePenalty = dto.LatePenalty,
            ReworkRate = dto.ReworkRate,
            IsActive = true,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SiRatePlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        // Load related data for response (tenant-safe: scope by plan.CompanyId)
        var cid = plan.CompanyId;
        var installer = cid.HasValue ? await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId && si.CompanyId == cid.Value, cancellationToken) : await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId, cancellationToken);
        var department = plan.DepartmentId.HasValue && cid.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value && d.CompanyId == cid.Value, cancellationToken) : plan.DepartmentId.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value, cancellationToken) : null;
        var method = plan.InstallationMethodId.HasValue && cid.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value && m.CompanyId == cid.Value, cancellationToken) : plan.InstallationMethodId.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value, cancellationToken) : null;

        return new SiRatePlanDto
        {
            Id = plan.Id,
            CompanyId = plan.CompanyId,
            DepartmentId = plan.DepartmentId,
            DepartmentName = department?.Name,
            ServiceInstallerId = plan.ServiceInstallerId,
            ServiceInstallerName = installer?.Name ?? string.Empty,
            InstallationMethodId = plan.InstallationMethodId,
            InstallationMethodName = method?.Name,
            RateType = plan.RateType,
            Level = plan.Level,
            PrelaidRate = plan.PrelaidRate,
            NonPrelaidRate = plan.NonPrelaidRate,
            SduRate = plan.SduRate,
            RdfPoleRate = plan.RdfPoleRate,
            ActivationRate = plan.ActivationRate,
            ModificationRate = plan.ModificationRate,
            AssuranceRate = plan.AssuranceRate,
            AssuranceRepullRate = plan.AssuranceRepullRate,
            FttrRate = plan.FttrRate,
            FttcRate = plan.FttcRate,
            OnTimeBonus = plan.OnTimeBonus,
            LatePenalty = plan.LatePenalty,
            ReworkRate = plan.ReworkRate,
            IsActive = plan.IsActive,
            EffectiveFrom = plan.EffectiveFrom,
            EffectiveTo = plan.EffectiveTo,
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task<SiRatePlanDto> UpdateSiRatePlanAsync(Guid id, CreateSiRatePlanDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Updating SI rate plan {PlanId} for company {CompanyId}", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var plan = await _context.SiRatePlans
            .Where(s => s.Id == id && s.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
        {
            throw new KeyNotFoundException($"SI rate plan with ID {id} not found");
        }

        plan.DepartmentId = dto.DepartmentId;
        plan.InstallationMethodId = dto.InstallationMethodId;
        plan.RateType = dto.RateType;
        plan.Level = dto.Level;
        plan.PrelaidRate = dto.PrelaidRate;
        plan.NonPrelaidRate = dto.NonPrelaidRate;
        plan.SduRate = dto.SduRate;
        plan.RdfPoleRate = dto.RdfPoleRate;
        plan.ActivationRate = dto.ActivationRate;
        plan.ModificationRate = dto.ModificationRate;
        plan.AssuranceRate = dto.AssuranceRate;
        plan.AssuranceRepullRate = dto.AssuranceRepullRate;
        plan.FttrRate = dto.FttrRate;
        plan.FttcRate = dto.FttcRate;
        plan.OnTimeBonus = dto.OnTimeBonus;
        plan.LatePenalty = dto.LatePenalty;
        plan.ReworkRate = dto.ReworkRate;
        plan.EffectiveFrom = dto.EffectiveFrom;
        plan.EffectiveTo = dto.EffectiveTo;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Load related data (tenant-safe: scope by plan.CompanyId)
        var cidUpdate = plan.CompanyId;
        var installer = cidUpdate.HasValue ? await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId && si.CompanyId == cidUpdate.Value, cancellationToken) : await _context.ServiceInstallers.FirstOrDefaultAsync(si => si.Id == plan.ServiceInstallerId, cancellationToken);
        var department = plan.DepartmentId.HasValue && cidUpdate.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value && d.CompanyId == cidUpdate.Value, cancellationToken) : plan.DepartmentId.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == plan.DepartmentId.Value, cancellationToken) : null;
        var method = plan.InstallationMethodId.HasValue && cidUpdate.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value && m.CompanyId == cidUpdate.Value, cancellationToken) : plan.InstallationMethodId.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == plan.InstallationMethodId.Value, cancellationToken) : null;

        return new SiRatePlanDto
        {
            Id = plan.Id,
            CompanyId = plan.CompanyId,
            DepartmentId = plan.DepartmentId,
            DepartmentName = department?.Name,
            ServiceInstallerId = plan.ServiceInstallerId,
            ServiceInstallerName = installer?.Name ?? string.Empty,
            InstallationMethodId = plan.InstallationMethodId,
            InstallationMethodName = method?.Name,
            RateType = plan.RateType,
            Level = plan.Level,
            PrelaidRate = plan.PrelaidRate,
            NonPrelaidRate = plan.NonPrelaidRate,
            SduRate = plan.SduRate,
            RdfPoleRate = plan.RdfPoleRate,
            ActivationRate = plan.ActivationRate,
            ModificationRate = plan.ModificationRate,
            AssuranceRate = plan.AssuranceRate,
            AssuranceRepullRate = plan.AssuranceRepullRate,
            FttrRate = plan.FttrRate,
            FttcRate = plan.FttcRate,
            OnTimeBonus = plan.OnTimeBonus,
            LatePenalty = plan.LatePenalty,
            ReworkRate = plan.ReworkRate,
            IsActive = plan.IsActive,
            EffectiveFrom = plan.EffectiveFrom,
            EffectiveTo = plan.EffectiveTo,
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task DeleteSiRatePlanAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Deleting SI rate plan {PlanId} for company {CompanyId}", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var plan = await _context.SiRatePlans
            .Where(s => s.Id == id && s.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
        {
            throw new KeyNotFoundException($"SI rate plan with ID {id} not found");
        }

        _context.SiRatePlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SI rate plan {PlanId} deleted successfully", id);
    }

    private static PayrollPeriodDto MapToPayrollPeriodDto(PayrollPeriod period)
    {
        return new PayrollPeriodDto
        {
            Id = period.Id,
            CompanyId = period.CompanyId,
            Period = period.Period,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            Status = period.Status,
            IsLocked = period.IsLocked,
            CreatedAt = period.CreatedAt
        };
    }

    private static PayrollRunDto MapToPayrollRunDto(PayrollRun run)
    {
        return new PayrollRunDto
        {
            Id = run.Id,
            CompanyId = run.CompanyId,
            PayrollPeriodId = run.PayrollPeriodId,
            PeriodStart = run.PeriodStart,
            PeriodEnd = run.PeriodEnd,
            Status = run.Status,
            TotalAmount = run.TotalAmount,
            Notes = run.Notes,
            ExportReference = run.ExportReference,
            FinalizedAt = run.FinalizedAt,
            PaidAt = run.PaidAt,
            Lines = run.PayrollLines.Select(l => new PayrollLineDto
            {
                Id = l.Id,
                ServiceInstallerId = l.ServiceInstallerId,
                ServiceInstallerName = string.Empty, // Will be loaded from related data if needed
                TotalJobs = l.TotalJobs,
                TotalPay = l.TotalPay,
                Adjustments = l.Adjustments,
                NetPay = l.NetPay,
                ExportReference = l.ExportReference
            }).ToList(),
            CreatedAt = run.CreatedAt
        };
    }
}

