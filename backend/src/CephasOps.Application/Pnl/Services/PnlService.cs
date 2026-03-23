using CephasOps.Application.Common;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Infrastructure.Persistence;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Pnl.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// P&amp;L service implementation
/// </summary>
public class PnlService : IPnlService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PnlService> _logger;

    public PnlService(ApplicationDbContext context, ILogger<PnlService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PnlSummaryDto> GetPnlSummaryAsync(Guid companyId, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new PnlSummaryDto { TotalRevenue = 0, TotalDirectMaterialCost = 0, TotalDirectLabourCost = 0, TotalIndirectCost = 0, GrossProfit = 0, NetProfit = 0, TotalJobs = 0, TotalOrdersCompleted = 0, Facts = new List<PnlFactDto>() };

        _logger.LogInformation("Getting P&L summary for company {CompanyId}", effectiveCompanyId.Value);

        var query = _context.PnlFacts.Where(f => f.CompanyId == effectiveCompanyId.Value);

        if (periodId.HasValue)
        {
            var period = await _context.PnlPeriods.FirstOrDefaultAsync(p => p.Id == periodId.Value && p.CompanyId == effectiveCompanyId.Value, cancellationToken);
            if (period != null)
            {
                query = query.Where(f => f.Period == period.Period);
            }
        }

        if (startDate.HasValue)
        {
            var startPeriod = startDate.Value.ToString("yyyy-MM");
            query = query.Where(f => string.Compare(f.Period, startPeriod) >= 0);
        }

        if (endDate.HasValue)
        {
            var endPeriod = endDate.Value.ToString("yyyy-MM");
            query = query.Where(f => string.Compare(f.Period, endPeriod) <= 0);
        }

        // Load facts (related entities loaded separately below)
        var facts = await query
            .ToListAsync(cancellationToken);

        // Load partners and cost centres in bulk
        var partnerIds = facts.Where(f => f.PartnerId.HasValue).Select(f => f.PartnerId!.Value).Distinct().ToList();
        var partners = partnerIds.Any()
            ? await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken)
            : new Dictionary<Guid, Partner>();

        var costCentreIds = facts.Where(f => f.CostCentreId.HasValue).Select(f => f.CostCentreId!.Value).Distinct().ToList();
        var costCentres = costCentreIds.Any()
            ? await _context.CostCentres
                .Where(cc => costCentreIds.Contains(cc.Id))
                .ToDictionaryAsync(cc => cc.Id, cancellationToken)
            : new Dictionary<Guid, CostCentre>();

        var summary = new PnlSummaryDto
        {
            TotalRevenue = facts.Sum(f => f.RevenueAmount),
            TotalDirectMaterialCost = facts.Sum(f => f.DirectMaterialCost),
            TotalDirectLabourCost = facts.Sum(f => f.DirectLabourCost),
            TotalIndirectCost = facts.Sum(f => f.IndirectCost),
            GrossProfit = facts.Sum(f => f.GrossProfit),
            NetProfit = facts.Sum(f => f.NetProfit),
            TotalJobs = facts.Sum(f => f.JobsCount),
            TotalOrdersCompleted = facts.Sum(f => f.OrdersCompletedCount),
            Facts = facts.Select(f => new PnlFactDto
            {
                Id = f.Id,
                PartnerId = f.PartnerId,
                PartnerName = f.PartnerId.HasValue && partners.TryGetValue(f.PartnerId.Value, out var partner) ? partner.Name : string.Empty,
                Vertical = f.Vertical,
                CostCentreId = f.CostCentreId,
                CostCentreName = f.CostCentreId.HasValue && costCentres.TryGetValue(f.CostCentreId.Value, out var costCentre) 
                    ? costCentre.Name 
                    : string.Empty,
                Period = f.Period,
                OrderType = f.OrderType,
                RevenueAmount = f.RevenueAmount,
                DirectMaterialCost = f.DirectMaterialCost,
                DirectLabourCost = f.DirectLabourCost,
                IndirectCost = f.IndirectCost,
                GrossProfit = f.GrossProfit,
                NetProfit = f.NetProfit,
                JobsCount = f.JobsCount,
                OrdersCompletedCount = f.OrdersCompletedCount
            }).ToList()
        };

        return summary;
    }

    public async Task<List<PnlOrderDetailDto>> GetPnlOrderDetailsAsync(Guid companyId, Guid? orderId = null, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<PnlOrderDetailDto>();

        _logger.LogInformation("Getting P&L order details for company {CompanyId}", effectiveCompanyId.Value);

        var query = _context.PnlDetailPerOrders.Where(d => d.CompanyId == effectiveCompanyId.Value);

        if (orderId.HasValue)
        {
            query = query.Where(d => d.OrderId == orderId.Value);
        }

        if (periodId.HasValue)
        {
            var period = await _context.PnlPeriods.FirstOrDefaultAsync(p => p.Id == periodId.Value && p.CompanyId == effectiveCompanyId.Value, cancellationToken);
            if (period != null)
            {
                query = query.Where(d => d.Period == period.Period);
            }
        }

        if (startDate.HasValue)
        {
            var startPeriod = startDate.Value.ToString("yyyy-MM");
            query = query.Where(d => string.Compare(d.Period, startPeriod) >= 0);
        }

        if (endDate.HasValue)
        {
            var endPeriod = endDate.Value.ToString("yyyy-MM");
            query = query.Where(d => string.Compare(d.Period, endPeriod) <= 0);
        }

        // Load details with related entities
        var details = await query
            .OrderByDescending(d => d.Period)
            .ToListAsync(cancellationToken);

        // Load orders, partners, and cost centres in bulk
        var orderIds = details.Select(d => d.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var partnerIds = details.Where(d => d.PartnerId != Guid.Empty).Select(d => d.PartnerId).Distinct().ToList();
        var partners = await _context.Partners
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return details.Select(d => new PnlOrderDetailDto
        {
            Id = d.Id,
            OrderId = d.OrderId,
            OrderUniqueId = orders.TryGetValue(d.OrderId, out var order) ? order.ServiceId : string.Empty,
            PartnerId = d.PartnerId,
            PartnerName = partners.TryGetValue(d.PartnerId, out var partner) ? partner.Name : string.Empty,
            Period = d.Period,
            OrderType = d.OrderType,
            RevenueAmount = d.RevenueAmount,
            MaterialCost = d.MaterialCost,
            LabourCost = d.LabourCost,
            OverheadAllocated = d.OverheadAllocated,
            ProfitForOrder = d.ProfitForOrder
        }).ToList();
    }

    public async Task<List<PnlDetailPerOrderDto>> GetPnlDetailPerOrderAsync(Guid companyId, Guid? orderId = null, Guid? partnerId = null, Guid? departmentId = null, Guid? serviceInstallerId = null, string? orderType = null, string? kpiResult = null, string? period = null, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("GetPnlDetailPerOrder");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<PnlDetailPerOrderDto>();

        _logger.LogInformation("Getting P&L detail per order for company {CompanyId}", effectiveCompanyId.Value);

        var query = _context.PnlDetailPerOrders.Where(d => d.CompanyId == effectiveCompanyId.Value);

        if (orderId.HasValue)
        {
            query = query.Where(d => d.OrderId == orderId.Value);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(d => d.PartnerId == partnerId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId == departmentId.Value);
        }

        if (serviceInstallerId.HasValue)
        {
            query = query.Where(d => d.ServiceInstallerId == serviceInstallerId.Value);
        }

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(d => d.OrderType == orderType);
        }

        if (!string.IsNullOrEmpty(kpiResult))
        {
            query = query.Where(d => d.KpiResult == kpiResult);
        }

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(d => d.Period == period);
        }

        var details = await query
            .OrderByDescending(d => d.Period)
            .ThenByDescending(d => d.CompletedAt)
            .ToListAsync(cancellationToken);

        // Load related entities in bulk
        var orderIds = details.Select(d => d.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var partnerIds = details.Where(d => d.PartnerId != Guid.Empty).Select(d => d.PartnerId).Distinct().ToList();
        var partners = partnerIds.Any()
            ? await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken)
            : new Dictionary<Guid, Partner>();

        var departmentIds = details.Where(d => d.DepartmentId.HasValue).Select(d => d.DepartmentId!.Value).Distinct().ToList();
        var departments = departmentIds.Any()
            ? await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, cancellationToken)
            : new Dictionary<Guid, CephasOps.Domain.Departments.Entities.Department>();

        var serviceInstallerIds = details.Where(d => d.ServiceInstallerId.HasValue).Select(d => d.ServiceInstallerId!.Value).Distinct().ToList();
        var serviceInstallers = serviceInstallerIds.Any()
            ? await _context.ServiceInstallers
                .Where(si => serviceInstallerIds.Contains(si.Id))
                .ToDictionaryAsync(si => si.Id, cancellationToken)
            : new Dictionary<Guid, CephasOps.Domain.ServiceInstallers.Entities.ServiceInstaller>();

        var orderTypeIds = orders.Values.Where(o => o.OrderTypeId != Guid.Empty).Select(o => o.OrderTypeId).Distinct().ToList();
        var orderTypes = orderTypeIds.Any()
            ? await _context.Set<CephasOps.Domain.Orders.Entities.OrderType>()
                .Where(ot => orderTypeIds.Contains(ot.Id))
                .ToDictionaryAsync(ot => ot.Id, cancellationToken)
            : new Dictionary<Guid, CephasOps.Domain.Orders.Entities.OrderType>();

        var orderCategoryIds = orders.Values.Where(o => o.OrderCategoryId.HasValue).Select(o => o.OrderCategoryId!.Value).Distinct().ToList();
        var orderCategories = orderCategoryIds.Any()
            ? await _context.Set<CephasOps.Domain.Orders.Entities.OrderCategory>()
                .Where(oc => orderCategoryIds.Contains(oc.Id))
                .ToDictionaryAsync(oc => oc.Id, cancellationToken)
            : new Dictionary<Guid, CephasOps.Domain.Orders.Entities.OrderCategory>();

        return details.Select(d =>
        {
            var order = orders.TryGetValue(d.OrderId, out var o) ? o : null;
            var partner = partners.TryGetValue(d.PartnerId, out var p) ? p : null;
            var orderCategory = order?.OrderCategoryId != null && orderCategories.TryGetValue(order.OrderCategoryId.Value, out var oc) ? oc : null;
            var derivedPartnerCategoryLabel = (partner != null && orderCategory != null && !string.IsNullOrEmpty(orderCategory.Code))
                ? $"{(partner.Code ?? partner.Name)}-{orderCategory.Code}"
                : null;
            var department = d.DepartmentId.HasValue && departments.TryGetValue(d.DepartmentId.Value, out var dept) ? dept : null;
            var serviceInstaller = d.ServiceInstallerId.HasValue && serviceInstallers.TryGetValue(d.ServiceInstallerId.Value, out var si) ? si : null;
            var orderType = order != null && orderTypes.TryGetValue(order.OrderTypeId, out var ot) ? ot : null;

            return new PnlDetailPerOrderDto
            {
                Id = d.Id,
                CompanyId = d.CompanyId,
                OrderId = d.OrderId,
                PartnerId = d.PartnerId,
                PartnerName = partner?.Name,
                DerivedPartnerCategoryLabel = derivedPartnerCategoryLabel,
                DepartmentId = d.DepartmentId,
                DepartmentName = department?.Name,
                Period = d.Period,
                OrderType = d.OrderType,
                OrderTypeName = orderType?.Name,
                OrderCategory = d.OrderCategory,
                InstallationMethod = d.InstallationMethod,
                RevenueAmount = d.RevenueAmount,
                MaterialCost = d.MaterialCost,
                LabourCost = d.LabourCost,
                OverheadAllocated = d.OverheadAllocated,
                GrossProfit = d.GrossProfit,
                ProfitForOrder = d.ProfitForOrder,
                KpiResult = d.KpiResult,
                RescheduleCount = d.RescheduleCount,
                ServiceInstallerId = d.ServiceInstallerId,
                ServiceInstallerName = serviceInstaller?.Name,
                RevenueRateSource = d.RevenueRateSource,
                LabourRateSource = d.LabourRateSource,
                CompletedAt = d.CompletedAt,
                CalculatedAt = d.CalculatedAt,
                DataQualityNotes = d.DataQualityNotes,
                OrderNumber = order?.ServiceId,
                CustomerName = order?.CustomerName,
                BuildingName = order?.BuildingName,
                AddressLine1 = order?.AddressLine1
            };
        }).ToList();
    }

    public async Task RebuildPnlAsync(Guid companyId, string period, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("RebuildPnl");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to rebuild P&L.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "RebuildPnl");

        _logger.LogInformation("Rebuilding P&L for company {CompanyId}, period {Period}", effectiveCompanyId.Value, period);

        // Parse period to get date range
        if (!DateTime.TryParseExact(period + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var periodStart))
        {
            throw new ArgumentException($"Invalid period format: {period}. Expected format: yyyy-MM");
        }
        var periodEnd = periodStart.AddMonths(1);

        // Step 1: Delete existing PnlDetailPerOrder records for this period
        var existingDetailsQuery = _context.PnlDetailPerOrders.Where(d => d.Period == period && d.CompanyId == effectiveCompanyId.Value);
        var existingDetails = await existingDetailsQuery.ToListAsync(cancellationToken);
        _context.PnlDetailPerOrders.RemoveRange(existingDetails);

        // Step 2: Get all completed orders for this period
        var completedOrders = await _context.Orders
            .Include(o => o.Reschedules)
            .Where(o => o.Status == "OrderCompleted" &&
                        o.UpdatedAt >= periodStart &&
                        o.UpdatedAt < periodEnd &&
                        o.CompanyId == effectiveCompanyId.Value)
            .ToListAsync(cancellationToken);

        // Load lookup tables for order type, order category, and installation method
        var orderTypeIds = completedOrders.Select(o => o.OrderTypeId).Distinct().ToList();
        var orderCategoryIds = completedOrders.Where(o => o.OrderCategoryId.HasValue).Select(o => o.OrderCategoryId!.Value).Distinct().ToList();
        var installationMethodIds = completedOrders.Where(o => o.InstallationMethodId.HasValue).Select(o => o.InstallationMethodId!.Value).Distinct().ToList();

        var orderTypes = await _context.OrderTypes
            .Where(ot => orderTypeIds.Contains(ot.Id))
            .ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken);
        var orderCategories = await _context.OrderCategories
            .Where(oc => orderCategoryIds.Contains(oc.Id))
            .ToDictionaryAsync(oc => oc.Id, oc => oc.Name, cancellationToken);
        var installationMethods = await _context.InstallationMethods
            .Where(im => installationMethodIds.Contains(im.Id))
            .ToDictionaryAsync(im => im.Id, im => im.Name, cancellationToken);

        _logger.LogInformation("Found {Count} completed orders for period {Period}", completedOrders.Count, period);

        // Step 3: Get revenue from invoice line items for these orders
        var orderIds = completedOrders.Select(o => o.Id).ToList();
        var invoiceLineItems = await _context.InvoiceLineItems
            .Where(li => li.OrderId.HasValue && orderIds.Contains(li.OrderId.Value) && li.CompanyId == effectiveCompanyId.Value)
            .ToListAsync(cancellationToken);
        var revenueByOrder = invoiceLineItems
            .GroupBy(li => li.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Total));

        // Step 4: Get material costs from stock movements for these orders
        var materialMovements = await _context.StockMovements
            .Include(sm => sm.Material)
            .Where(sm => sm.OrderId.HasValue && 
                         orderIds.Contains(sm.OrderId.Value) &&
                         sm.MovementType == "InstallAtCustomer" &&
                         sm.CompanyId == effectiveCompanyId.Value)
            .ToListAsync(cancellationToken);
        var materialCostByOrder = materialMovements
            .GroupBy(sm => sm.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(sm => sm.Quantity * (sm.Material?.DefaultCost ?? 0)));

        // Step 5: Get labour costs from job earning records for these orders (tenant-scoped: use effectiveCompanyId only)
        var jobEarningsQuery = _context.JobEarningRecords
            .Where(je => orderIds.Contains(je.OrderId) && je.CompanyId == effectiveCompanyId.Value);
        var jobEarnings = await jobEarningsQuery.ToListAsync(cancellationToken);
        var labourCostByOrder = jobEarnings
            .GroupBy(je => je.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(je => je.FinalPay));
        var kpiResultByOrder = jobEarnings
            .GroupBy(je => je.OrderId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.KpiResult);
        var labourRateSourceByOrder = jobEarnings
            .GroupBy(je => je.OrderId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.RateSource);

        // Step 6: Calculate overhead allocation
        var totalOverhead = await _context.OverheadEntries
            .Where(o => o.Period == period && o.CompanyId == effectiveCompanyId.Value)
            .SumAsync(o => o.Amount, cancellationToken);
        var overheadPerOrder = completedOrders.Count > 0 ? totalOverhead / completedOrders.Count : 0;

        // Step 7: Create PnlDetailPerOrder records
        var pnlDetails = new List<PnlDetailPerOrder>();
        foreach (var order in completedOrders)
        {
            var revenue = revenueByOrder.GetValueOrDefault(order.Id, 0);
            var materialCost = materialCostByOrder.GetValueOrDefault(order.Id, 0);
            var labourCost = labourCostByOrder.GetValueOrDefault(order.Id, 0);
            var grossProfit = revenue - materialCost - labourCost;
            var netProfit = grossProfit - overheadPerOrder;

            var dataQualityNotes = new List<string>();
            if (revenue == 0) dataQualityNotes.Add("No invoice found");
            if (materialCost == 0) dataQualityNotes.Add("No material movements found");
            if (labourCost == 0) dataQualityNotes.Add("No job earning record found");

            var orderTypeName = orderTypes.GetValueOrDefault(order.OrderTypeId, "Unknown");
            var orderCategoryName = order.OrderCategoryId.HasValue 
                ? orderCategories.GetValueOrDefault(order.OrderCategoryId.Value) 
                : null;
            var installationMethodName = order.InstallationMethodId.HasValue 
                ? installationMethods.GetValueOrDefault(order.InstallationMethodId.Value) 
                : null;

            var detail = new PnlDetailPerOrder
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId.Value,
                OrderId = order.Id,
                PartnerId = order.PartnerId,
                DepartmentId = order.DepartmentId,
                Period = period,
                OrderType = orderTypeName,
                OrderCategory = orderCategoryName,
                InstallationMethod = installationMethodName,
                RevenueAmount = revenue,
                MaterialCost = materialCost,
                LabourCost = labourCost,
                OverheadAllocated = overheadPerOrder,
                GrossProfit = grossProfit,
                ProfitForOrder = netProfit,
                KpiResult = kpiResultByOrder.GetValueOrDefault(order.Id),
                RescheduleCount = order.Reschedules?.Count ?? 0,
                ServiceInstallerId = order.AssignedSiId,
                RevenueRateSource = revenueByOrder.ContainsKey(order.Id) ? "Invoice" : null,
                LabourRateSource = labourRateSourceByOrder.GetValueOrDefault(order.Id),
                CompletedAt = order.UpdatedAt,
                CalculatedAt = DateTime.UtcNow,
                DataQualityNotes = dataQualityNotes.Count > 0 ? string.Join("; ", dataQualityNotes) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            pnlDetails.Add(detail);
        }

        _context.PnlDetailPerOrders.AddRange(pnlDetails);

        // Step 8: Rebuild PnlFacts (aggregate by Partner, OrderType, etc.)
        var existingFacts = await _context.PnlFacts
            .Where(f => f.Period == period && f.CompanyId == effectiveCompanyId.Value)
            .ToListAsync(cancellationToken);
        _context.PnlFacts.RemoveRange(existingFacts);

        var factGroups = pnlDetails
            .GroupBy(d => new { d.PartnerId, d.OrderType })
            .Select(g => new PnlFact
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId.Value,
                PartnerId = g.Key.PartnerId,
                Vertical = "GPON", // Default vertical
                CostCentreId = null,
                Period = period,
                OrderType = g.Key.OrderType,
                RevenueAmount = g.Sum(d => d.RevenueAmount),
                DirectMaterialCost = g.Sum(d => d.MaterialCost),
                DirectLabourCost = g.Sum(d => d.LabourCost),
                IndirectCost = g.Sum(d => d.OverheadAllocated),
                GrossProfit = g.Sum(d => d.GrossProfit),
                NetProfit = g.Sum(d => d.ProfitForOrder),
                JobsCount = g.Count(),
                OrdersCompletedCount = g.Count(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        _context.PnlFacts.AddRange(factGroups);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("P&L rebuild completed for period {Period}: {DetailCount} order details, {FactCount} fact records", 
            period, pnlDetails.Count, factGroups.Count);
    }

    public async Task<List<PnlPeriodDto>> GetPnlPeriodsAsync(Guid companyId, string? year = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<PnlPeriodDto>();

        _logger.LogInformation("Getting P&L periods for company {CompanyId}", effectiveCompanyId.Value);

        var query = _context.PnlPeriods.Where(p => p.CompanyId == effectiveCompanyId.Value);

        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(p => p.Period.StartsWith(year));
        }

        var periods = await query
            .OrderByDescending(p => p.Period)
            .ToListAsync(cancellationToken);

        return periods.Select(p => new PnlPeriodDto
        {
            Id = p.Id,
            CompanyId = p.CompanyId,
            Period = p.Period,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            IsLocked = p.IsLocked,
            LastRecalculatedAt = p.LastRecalculatedAt,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<PnlPeriodDto?> GetPnlPeriodByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId != Guid.Empty ? companyId : (Guid?)TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        _logger.LogInformation("Getting P&L period {PeriodId} for company {CompanyId}", id, effectiveCompanyId.Value);

        var period = await _context.PnlPeriods
            .Where(p => p.Id == id && p.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (period == null) return null;

        return new PnlPeriodDto
        {
            Id = period.Id,
            CompanyId = period.CompanyId,
            Period = period.Period,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            IsLocked = period.IsLocked,
            LastRecalculatedAt = period.LastRecalculatedAt,
            CreatedAt = period.CreatedAt
        };
    }

    public async Task<List<OverheadEntryDto>> GetOverheadEntriesAsync(Guid companyId, Guid? costCentreId = null, string? period = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<OverheadEntryDto>();

        _logger.LogInformation("Getting overhead entries for company {CompanyId}", effectiveCompanyId.Value);

        var query = _context.OverheadEntries.Where(o => o.CompanyId == effectiveCompanyId.Value);

        if (costCentreId.HasValue)
        {
            query = query.Where(o => o.CostCentreId == costCentreId.Value);
        }

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(o => o.Period == period);
        }

        var entries = await query
            .OrderByDescending(o => o.Period)
            .ToListAsync(cancellationToken);

        // Load cost centres in bulk
        var costCentreIds = entries.Select(o => o.CostCentreId).Distinct().ToList();
        var costCentres = costCentreIds.Any()
            ? await _context.CostCentres
                .Where(cc => costCentreIds.Contains(cc.Id))
                .ToDictionaryAsync(cc => cc.Id, cancellationToken)
            : new Dictionary<Guid, CostCentre>();

        return entries.Select(o => new OverheadEntryDto
        {
            Id = o.Id,
            CompanyId = o.CompanyId,
            CostCentreId = o.CostCentreId,
            CostCentreName = costCentres.TryGetValue(o.CostCentreId, out var costCentre)
                ? costCentre.Name
                : string.Empty,
            Period = o.Period,
            Amount = o.Amount,
            Description = o.Description,
            AllocationBasis = o.AllocationBasis,
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OverheadEntryDto> CreateOverheadEntryAsync(CreateOverheadEntryDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreateOverheadEntry");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create an overhead entry.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "CreateOverheadEntry");

        _logger.LogInformation("Creating overhead entry for company {CompanyId}", effectiveCompanyId.Value);

        var entry = new OverheadEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            CostCentreId = dto.CostCentreId,
            Period = dto.Period,
            Amount = dto.Amount,
            Description = dto.Description,
            AllocationBasis = dto.AllocationBasis,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OverheadEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return new OverheadEntryDto
        {
            Id = entry.Id,
            CompanyId = entry.CompanyId,
            CostCentreId = entry.CostCentreId,
            CostCentreName = string.Empty,
            Period = entry.Period,
            Amount = entry.Amount,
            Description = entry.Description,
            AllocationBasis = entry.AllocationBasis,
            CreatedAt = entry.CreatedAt
        };
    }

    public async Task DeleteOverheadEntryAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("DeleteOverheadEntry");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete an overhead entry.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "DeleteOverheadEntry");

        _logger.LogInformation("Deleting overhead entry {EntryId} for company {CompanyId}", id, effectiveCompanyId.Value);

        var entry = await _context.OverheadEntries
            .Where(o => o.Id == id && o.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
        {
            throw new KeyNotFoundException($"Overhead entry with ID {id} not found");
        }

        _context.OverheadEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

