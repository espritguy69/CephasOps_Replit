using CephasOps.Application.Scheduler.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Workflow.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Notifications.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CephasOps.Application.Scheduler.Services;

/// <summary>
/// Scheduler service implementation
/// Per SCHEDULER_MODULE.md: Integrates with KPI profiles for dynamic duration calculation
/// </summary>
public class SchedulerService : ISchedulerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulerService> _logger;
    private readonly IKpiProfileService _kpiProfileService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notificationService;

    public SchedulerService(
        ApplicationDbContext context,
        ILogger<SchedulerService> logger,
        IKpiProfileService kpiProfileService,
        IServiceProvider serviceProvider,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _kpiProfileService = kpiProfileService;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
    }

    public async Task<List<CalendarDto>> GetCalendarAsync(Guid? companyId, DateTime fromDate, DateTime toDate, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var slots = await GetScheduleSlotsAsync(companyId, null, null, null, departmentId, cancellationToken);
        var availabilitiesQuery = _context.SiAvailabilities.AsQueryable()
            .Where(a => a.Date >= fromDate.Date && a.Date <= toDate.Date);
        if (companyId.HasValue)
        {
            availabilitiesQuery = availabilitiesQuery.Where(a => a.CompanyId == companyId.Value);
        }
        if (departmentId.HasValue)
        {
            var siIdsInDept = await _context.ServiceInstallers
                .Where(si => si.DepartmentId == departmentId.Value || si.DepartmentId == null)
                .Select(si => si.Id)
                .ToListAsync(cancellationToken);
            availabilitiesQuery = availabilitiesQuery.Where(a => siIdsInDept.Contains(a.ServiceInstallerId));
        }
        var availabilities = await availabilitiesQuery
            .ToListAsync(cancellationToken);

        var calendar = new Dictionary<DateTime, CalendarDto>();

        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            calendar[date] = new CalendarDto
            {
                Date = date,
                Slots = slots.Where(s => s.Date.Date == date).ToList(),
                Availabilities = availabilities
                    .Where(a => a.Date.Date == date)
                    .Select(a => new SiAvailabilityDto
                    {
                        Id = a.Id,
                        ServiceInstallerId = a.ServiceInstallerId,
                        Date = a.Date,
                        IsWorkingDay = a.IsWorkingDay,
                        WorkingFrom = a.WorkingFrom,
                        WorkingTo = a.WorkingTo,
                        MaxJobs = a.MaxJobs,
                        CurrentJobsCount = a.CurrentJobsCount,
                        Notes = a.Notes?.ToString()
                    })
                    .ToList()
            };
        }

        return calendar.Values.OrderBy(c => c.Date).ToList();
    }

    public async Task<List<ScheduleSlotDto>> GetScheduleSlotsAsync(Guid? companyId, Guid? siId = null, DateTime? date = null, Guid? orderId = null, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledSlots.AsQueryable();
        
        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        if (siId.HasValue)
        {
            query = query.Where(s => s.ServiceInstallerId == siId.Value);
        }

        if (date.HasValue)
        {
            var dateOnly = date.Value.Date;
            query = query.Where(s => s.Date.Date == dateOnly);
        }

        if (orderId.HasValue)
        {
            query = query.Where(s => s.OrderId == orderId.Value);
        }

        var slotsData = await (from slot in query
                               join order in _context.Orders on slot.OrderId equals order.Id
                               join si in _context.ServiceInstallers on slot.ServiceInstallerId equals si.Id
                               join partner in _context.Partners on order.PartnerId equals partner.Id into partnerGroup
                               from partner in partnerGroup.DefaultIfEmpty()
                               join orderCategory in _context.OrderCategories on order.OrderCategoryId equals orderCategory.Id into orderCategoryGroup
                               from orderCategory in orderCategoryGroup.DefaultIfEmpty()
                               join orderType in _context.OrderTypes on order.OrderTypeId equals orderType.Id into orderTypeGroup
                               from orderType in orderTypeGroup.DefaultIfEmpty()
                               join building in _context.Buildings on order.BuildingId equals building.Id into buildingGroup
                               from building in buildingGroup.DefaultIfEmpty()
                               where !departmentId.HasValue || (order.DepartmentId == departmentId.Value && (si.DepartmentId == null || si.DepartmentId == departmentId.Value))
                               select new
                               {
                                   slot,
                                   order,
                                   si,
                                   partner,
                                   orderCategory,
                                   orderType,
                                   building
                               })
            .ToListAsync(cancellationToken);

        var slots = new List<ScheduleSlotDto>();
        
        foreach (var x in slotsData)
        {
            // Resolve KPI profile for expected duration per KPI_PROFILE_MODULE.md
            int? expectedDuration = null;
            string? kpiProfileName = null;
            
            if (companyId.HasValue && x.orderType != null)
            {
                try
                {
                    var kpiProfile = await _kpiProfileService.GetEffectiveProfileAsync(
                        companyId.Value,
                        x.order.PartnerId,
                        x.orderType.Name,
                        x.order.InstallationMethodId,
                        null, // BuildingTypeId is deprecated
                        x.slot.Date,
                        cancellationToken);
                    
                    if (kpiProfile != null)
                    {
                        expectedDuration = kpiProfile.MaxJobDurationMinutes;
                        kpiProfileName = kpiProfile.Name;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve KPI profile for order {OrderId}", x.order.Id);
                }
            }
            
            slots.Add(new ScheduleSlotDto
            {
                Id = x.slot.Id,
                OrderId = x.slot.OrderId,
                ServiceInstallerId = x.slot.ServiceInstallerId,
                Date = x.slot.Date,
                WindowFrom = x.slot.WindowFrom,
                WindowTo = x.slot.WindowTo,
                PlannedTravelMin = x.slot.PlannedTravelMin,
                SequenceIndex = x.slot.SequenceIndex,
                Status = x.slot.Status,
                CreatedByUserId = x.slot.CreatedByUserId,
                CreatedAt = x.slot.CreatedAt,
                // Confirmation and posting tracking
                ConfirmedByUserId = x.slot.ConfirmedByUserId,
                ConfirmedAt = x.slot.ConfirmedAt,
                PostedByUserId = x.slot.PostedByUserId,
                PostedAt = x.slot.PostedAt,
                // Reschedule request fields
                RescheduleRequestedDate = x.slot.RescheduleRequestedDate,
                RescheduleRequestedTime = x.slot.RescheduleRequestedTime,
                RescheduleReason = x.slot.RescheduleReason,
                RescheduleNotes = x.slot.RescheduleNotes,
                RescheduleRequestedBySiId = x.slot.RescheduleRequestedBySiId,
                RescheduleRequestedAt = x.slot.RescheduleRequestedAt,
                // Enriched order details
                ServiceId = x.order.ServiceId,
                TicketId = x.order.TicketId,
                ExternalRef = x.order.ExternalRef,
                CustomerName = x.order.CustomerName,
                BuildingName = x.order.BuildingName,
                PartnerName = x.partner?.Name,
                PartnerId = x.order.PartnerId,
                DerivedPartnerCategoryLabel = (x.partner != null && x.orderCategory != null && !string.IsNullOrEmpty(x.orderCategory.Code)) ? $"{(x.partner.Code ?? x.partner.Name)}-{x.orderCategory.Code}" : null,
                OrderStatus = x.order.Status,
                // Enriched SI details
                ServiceInstallerName = x.si.Name,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                ServiceInstallerIsSubcontractor = x.si.IsSubcontractor,
#pragma warning restore CS0618
                ServiceInstallerSiLevel = x.si.SiLevel.ToString(),
                // KPI profile integration per SCHEDULER_MODULE.md
                ExpectedDurationMinutes = expectedDuration,
                KpiProfileName = kpiProfileName
            });
        }

        return slots;
    }

    public async Task<List<SiAvailabilityDto>> GetSiAvailabilityAsync(Guid? companyId, Guid siId, DateTime? fromDate = null, DateTime? toDate = null, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue)
        {
            var si = await _context.ServiceInstallers
                .Where(s => s.Id == siId && (s.DepartmentId == null || s.DepartmentId == departmentId.Value))
                .FirstOrDefaultAsync(cancellationToken);
            if (si == null)
                throw new UnauthorizedAccessException("Service installer not in your department or not found.");
        }

        var query = $"SELECT * FROM \"SiAvailabilities\" WHERE \"ServiceInstallerId\" = '{siId}'";
        if (companyId.HasValue)
        {
            query += $" AND \"CompanyId\" = '{companyId.Value}'";
        }

        if (fromDate.HasValue)
        {
            query += $" AND \"Date\" >= '{fromDate.Value:yyyy-MM-dd}'";
        }

        if (toDate.HasValue)
        {
            query += $" AND \"Date\" <= '{toDate.Value:yyyy-MM-dd}'";
        }

        var availabilities = await _context.Database
            .SqlQueryRaw<dynamic>(query)
            .ToListAsync(cancellationToken);

        return availabilities.Select(a => new SiAvailabilityDto
        {
            Id = Guid.Parse(a.Id.ToString()),
            ServiceInstallerId = Guid.Parse(a.ServiceInstallerId.ToString()),
            Date = (DateTime)a.Date,
            IsWorkingDay = a.IsWorkingDay != null && (bool)a.IsWorkingDay,
            WorkingFrom = a.WorkingFrom != null ? (TimeSpan?)a.WorkingFrom : null,
            WorkingTo = a.WorkingTo != null ? (TimeSpan?)a.WorkingTo : null,
            MaxJobs = a.MaxJobs != null ? (int)a.MaxJobs : 0,
            CurrentJobsCount = a.CurrentJobsCount != null ? (int)a.CurrentJobsCount : 0,
            Notes = a.Notes?.ToString()
        }).ToList();
    }

    public async Task<ScheduleSlotDto> CreateScheduleSlotAsync(CreateScheduleSlotDto dto, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue)
        {
            var order = await _context.Orders.Where(o => o.Id == dto.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (order == null || order.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not found or not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == dto.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not found or not in your department.");
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Get next sequence index for this SI on this date
        var existingSlots = await GetScheduleSlotsAsync(companyId, dto.ServiceInstallerId, dto.Date.Date, null, departmentId, cancellationToken);
        var maxSequenceIndex = existingSlots.Select(s => s.SequenceIndex).DefaultIfEmpty(0).Max();
        var sequenceIndex = maxSequenceIndex + 1;

        object[] slotParams = {
            id, (object?)companyId ?? DBNull.Value, dto.OrderId, dto.ServiceInstallerId, dto.Date.Date,
            dto.WindowFrom, dto.WindowTo, (object?)dto.PlannedTravelMin ?? DBNull.Value,
            sequenceIndex, "Draft", userId, now, now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""ScheduledSlots"" (""Id"", ""CompanyId"", ""OrderId"", ""ServiceInstallerId"", ""Date"", ""WindowFrom"", ""WindowTo"", ""PlannedTravelMin"", ""SequenceIndex"", ""Status"", ""CreatedByUserId"", ""CreatedAt"", ""UpdatedAt"")
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12})",
            slotParams);

        _logger.LogInformation("Schedule slot created: {SlotId}, Order: {OrderId}, SI: {SiId}", id, dto.OrderId, dto.ServiceInstallerId);

        var slots = await GetScheduleSlotsAsync(companyId, dto.ServiceInstallerId, dto.Date.Date, null, departmentId, cancellationToken);
        return slots.FirstOrDefault(s => s.Id == id) 
            ?? throw new InvalidOperationException("Failed to retrieve created schedule slot");
    }

    public async Task<SiAvailabilityDto> CreateSiAvailabilityAsync(CreateSiAvailabilityDto dto, Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue)
        {
            var si = await _context.ServiceInstallers.Where(s => s.Id == dto.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not found or not in your department.");
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] availParams = {
            id, (object?)companyId ?? DBNull.Value, dto.ServiceInstallerId, dto.Date.Date, dto.IsWorkingDay,
            (object?)dto.WorkingFrom ?? DBNull.Value, (object?)dto.WorkingTo ?? DBNull.Value,
            dto.MaxJobs, 0, (object?)dto.Notes ?? DBNull.Value, now, now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""SiAvailabilities"" (""Id"", ""CompanyId"", ""ServiceInstallerId"", ""Date"", ""IsWorkingDay"", ""WorkingFrom"", ""WorkingTo"", ""MaxJobs"", ""CurrentJobsCount"", ""Notes"", ""CreatedAt"", ""UpdatedAt"")
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
            availParams);

        _logger.LogInformation("SI availability created: {AvailabilityId}, SI: {SiId}, Date: {Date}", id, dto.ServiceInstallerId, dto.Date);

        var availabilities = await GetSiAvailabilityAsync(companyId, dto.ServiceInstallerId, dto.Date.Date, dto.Date.Date, departmentId, cancellationToken);
        return availabilities.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException("Failed to retrieve created SI availability");
    }

    public async Task<List<UnassignedOrderDto>> GetUnassignedOrdersAsync(Guid? companyId, Guid? partnerId = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        // Get orders that are pending/assigned but not yet scheduled
        var scheduledOrderIds = await _context.ScheduledSlots
            .Where(s => !companyId.HasValue || s.CompanyId == companyId.Value)
            .Select(s => s.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _context.Orders
            .Where(o => !scheduledOrderIds.Contains(o.Id) && 
                       (o.Status == "Pending" || o.Status == "Assigned"))
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(o => o.PartnerId == partnerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate <= toDate.Value.Date);
        }

        var ordersData = await (from order in query
                               join partner in _context.Partners on order.PartnerId equals partner.Id into partnerGroup
                               from partner in partnerGroup.DefaultIfEmpty()
                               join orderCategory in _context.OrderCategories on order.OrderCategoryId equals orderCategory.Id into orderCategoryGroup
                               from orderCategory in orderCategoryGroup.DefaultIfEmpty()
                               join orderType in _context.OrderTypes on order.OrderTypeId equals orderType.Id into orderTypeGroup
                               from orderType in orderTypeGroup.DefaultIfEmpty()
                               join building in _context.Buildings on order.BuildingId equals building.Id into buildingGroup
                               from building in buildingGroup.DefaultIfEmpty()
                               select new
                               {
                                   order,
                                   partner,
                                   orderCategory,
                                   orderType,
                                   building
                               })
            .OrderBy(x => x.order.AppointmentDate)
            .ThenBy(x => x.order.AppointmentWindowFrom)
            .ToListAsync(cancellationToken);

        var orders = new List<UnassignedOrderDto>();
        
        foreach (var x in ordersData)
        {
            // Resolve KPI profile for expected duration per KPI_PROFILE_MODULE.md
            int? expectedDuration = null;
            string? kpiProfileName = null;
            
            if (companyId.HasValue && x.orderType != null)
            {
                try
                {
                    var kpiProfile = await _kpiProfileService.GetEffectiveProfileAsync(
                        companyId.Value,
                        x.order.PartnerId,
                        x.orderType.Name,
                        x.order.InstallationMethodId,
                        null, // BuildingTypeId is deprecated
                        x.order.AppointmentDate,
                        cancellationToken);
                    
                    if (kpiProfile != null)
                    {
                        expectedDuration = kpiProfile.MaxJobDurationMinutes;
                        kpiProfileName = kpiProfile.Name;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve KPI profile for order {OrderId}", x.order.Id);
                }
            }
            
            orders.Add(new UnassignedOrderDto
            {
                Id = x.order.Id,
                ServiceId = x.order.ServiceId,
                TicketId = x.order.TicketId,
                ExternalRef = x.order.ExternalRef,
                CustomerName = x.order.CustomerName,
                BuildingName = x.order.BuildingName,
                PartnerName = x.partner?.Name,
                PartnerId = x.order.PartnerId,
                DerivedPartnerCategoryLabel = (x.partner != null && x.orderCategory != null && !string.IsNullOrEmpty(x.orderCategory.Code)) ? $"{(x.partner.Code ?? x.partner.Name)}-{x.orderCategory.Code}" : null,
                Status = x.order.Status,
                AppointmentDate = x.order.AppointmentDate,
                AppointmentWindowFrom = x.order.AppointmentWindowFrom,
                AppointmentWindowTo = x.order.AppointmentWindowTo,
                Priority = x.order.Priority,
                City = x.order.City,
                State = x.order.State,
                // KPI profile integration per SCHEDULER_MODULE.md
                ExpectedDurationMinutes = expectedDuration,
                KpiProfileName = kpiProfileName,
                OrderTypeId = x.order.OrderTypeId,
                BuildingTypeId = null // BuildingTypeId is obsolete - use PropertyType enum instead
            });
        }

        return orders;
    }

    public async Task<ScheduleSlotDto> UpdateScheduleSlotAsync(Guid slotId, UpdateScheduleSlotDto dto, Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && (!companyId.HasValue || s.CompanyId == companyId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var order = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (order == null || order.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (dto.ServiceInstallerId.HasValue)
        {
            slot.ServiceInstallerId = dto.ServiceInstallerId.Value;
        }

        if (dto.Date.HasValue)
        {
            slot.Date = dto.Date.Value.Date;
        }

        if (dto.WindowFrom.HasValue)
        {
            slot.WindowFrom = dto.WindowFrom.Value;
        }

        if (dto.WindowTo.HasValue)
        {
            slot.WindowTo = dto.WindowTo.Value;
        }

        if (dto.PlannedTravelMin.HasValue)
        {
            slot.PlannedTravelMin = dto.PlannedTravelMin.Value;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            slot.Status = dto.Status;
        }

        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Return updated slot with enriched data
        var updatedSlots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return updatedSlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve updated schedule slot");
    }

    public async Task BlockOrderAsync(Guid orderId, BlockOrderDto dto, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Blocking order {OrderId}, blocker type: {BlockerType}", orderId, dto.BlockerType);

        // Verify order exists and belongs to company (and department when scoped)
        var orderQuery = _context.Orders.Where(o => o.Id == orderId);
        if (companyId.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.CompanyId == companyId.Value);
        }
        if (departmentId.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.DepartmentId == departmentId.Value);
        }

        var order = await orderQuery.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} not found");
        }

        // Create order blocker
        var blocker = new OrderBlocker
        {
            Id = Guid.NewGuid(),
            CompanyId = order.CompanyId,
            OrderId = orderId,
            BlockerType = dto.BlockerType,
            Description = dto.Description,
            RaisedBySiId = dto.RaisedBySiId,
            RaisedByUserId = userId,
            RaisedAt = DateTime.UtcNow,
            Resolved = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderBlockers.Add(blocker);
        await _context.SaveChangesAsync(cancellationToken);

        // ⚠️ FIXED: Transition order to "Blocker" via workflow engine instead of direct status update
        if (order.Status != "Blocker")
        {
            try
            {
                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = orderId,
                    TargetStatus = "Blocker",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = dto.Description ?? string.Empty,
                        ["blockerType"] = dto.BlockerType,
                        ["blockerId"] = blocker.Id.ToString(),
                        ["userId"] = userId.ToString(),
                        ["source"] = "SchedulerService"
                    }
                };

                var orderCompanyId = order.CompanyId ?? Guid.Empty; // CompanyId is nullable in entity, use fallback
                var workflowEngineService = _serviceProvider.GetRequiredService<IWorkflowEngineService>();
                var workflowJob = await workflowEngineService.ExecuteTransitionAsync(
                    orderCompanyId,
                    executeDto,
                    userId,
                    cancellationToken);

                if (workflowJob.State != "Succeeded")
                {
                    _logger.LogWarning("Failed to transition Order {OrderId} to Blocker via workflow engine: {Error}",
                        orderId, workflowJob.LastError);
                    // Blocker record is already created, so we continue
                }
                else
                {
                    _logger.LogInformation("Order {OrderId} transitioned to Blocker via workflow engine", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning Order {OrderId} to Blocker via workflow engine", orderId);
                // Blocker record is already created, so we continue
            }
        }

        _logger.LogInformation("Order {OrderId} blocked successfully with blocker type {BlockerType}", orderId, dto.BlockerType);
    }

    /// <summary>
    /// Confirm schedule (changes ScheduledSlot status from Draft to Confirmed)
    /// </summary>
    public async Task<ScheduleSlotDto> ConfirmScheduleAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && (!companyId.HasValue || s.CompanyId == companyId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var order = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (order == null || order.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "Draft")
        {
            throw new InvalidOperationException($"Cannot confirm schedule slot with status '{slot.Status}'. Only Draft slots can be confirmed.");
        }

        slot.Status = "Confirmed";
        slot.ConfirmedByUserId = userId;
        slot.ConfirmedAt = DateTime.UtcNow;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Schedule slot {SlotId} confirmed by user {UserId}", slotId, userId);

        var slots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return slots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve confirmed schedule slot");
    }

    /// <summary>
    /// Post schedule to SI (changes ScheduledSlot status from Confirmed to Posted and triggers order status change via workflow)
    /// </summary>
    public async Task<ScheduleSlotDto> PostScheduleToSIAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue)
        {
            throw new InvalidOperationException("Company context required for posting schedule");
        }

        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && s.CompanyId == companyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var slotOrder = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (slotOrder == null || slotOrder.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "Confirmed")
        {
            throw new InvalidOperationException($"Cannot post schedule slot with status '{slot.Status}'. Only Confirmed slots can be posted.");
        }

        // Get the order to check current status and update it via workflow
        var order = await _context.Orders
            .Where(o => o.Id == slot.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {slot.OrderId} not found");
        }

        // Detect conflicts before posting
        var conflicts = await DetectSchedulingConflictsAsync(
            orderId: order.Id,
            slotId: slotId,
            siId: slot.ServiceInstallerId,
            date: slot.Date,
            companyId: companyId,
            departmentId: departmentId,
            cancellationToken);

        // If order is Pending, transition to Assigned via workflow engine
        if (order.Status == "Pending")
        {
            try
            {
                var metadata = new Dictionary<string, object>
                {
                    ["scheduledSlotId"] = slotId.ToString(),
                    ["appointmentDate"] = slot.Date.ToString("yyyy-MM-dd"),
                    ["appointmentTime"] = slot.WindowFrom.ToString(@"hh\:mm")
                };

                // Include conflict information if conflicts exist
                if (conflicts.Count > 0)
                {
                    metadata["conflictCount"] = conflicts.Count;
                    metadata["hasConflicts"] = true;
                    metadata["conflicts"] = conflicts.Select(c => new Dictionary<string, object>
                    {
                        ["slotId"] = c.SlotId.ToString(),
                        ["orderId"] = c.OrderId.ToString(),
                        ["conflictType"] = c.ConflictType,
                        ["conflictDescription"] = c.ConflictDescription,
                        ["overlappingTime"] = $"{c.WindowFrom:hh\\:mm} - {c.WindowTo:hh\\:mm}"
                    }).ToList();
                }
                else
                {
                    metadata["conflictCount"] = 0;
                    metadata["hasConflicts"] = false;
                }

                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = order.Id,
                    TargetStatus = "Assigned",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = conflicts.Count > 0
                            ? $"Schedule posted to SI (with {conflicts.Count} conflict(s))"
                            : "Schedule posted to SI",
                        ["userId"] = userId.ToString(),
                        ["source"] = "Scheduler",
                        ["siId"] = slot.ServiceInstallerId.ToString(),
                        ["metadata"] = metadata
                    }
                };

                var workflowEngineService = _serviceProvider.GetRequiredService<IWorkflowEngineService>();
                await workflowEngineService.ExecuteTransitionAsync(
                    companyId.Value,
                    executeDto,
                    userId,
                    cancellationToken);

                _logger.LogInformation("Order {OrderId} status changed from Pending to Assigned via workflow engine", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition order {OrderId} to Assigned status via workflow engine", order.Id);
                throw new InvalidOperationException($"Failed to post schedule: {ex.Message}", ex);
            }
        }

        // Update slot status to Posted
        slot.Status = "Posted";
        slot.PostedByUserId = userId;
        slot.PostedAt = DateTime.UtcNow;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Schedule slot {SlotId} posted to SI by user {UserId}", slotId, userId);

        var updatedSlots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return updatedSlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve posted schedule slot");
    }

    /// <summary>
    /// Return schedule to Draft (reverts Confirmed back to Draft)
    /// </summary>
    public async Task<ScheduleSlotDto> ReturnScheduleToDraftAsync(Guid slotId, Guid? companyId, Guid userId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && (!companyId.HasValue || s.CompanyId == companyId.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var order = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (order == null || order.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "Confirmed")
        {
            throw new InvalidOperationException($"Cannot return schedule slot with status '{slot.Status}' to Draft. Only Confirmed slots can be returned to Draft.");
        }

        slot.Status = "Draft";
        slot.ConfirmedByUserId = null;
        slot.ConfirmedAt = null;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Schedule slot {SlotId} returned to Draft by user {UserId}", slotId, userId);

        var slots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return slots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve schedule slot");
    }

    /// <summary>
    /// SI requests reschedule (different day) - updates ScheduledSlot and transitions order to ReschedulePendingApproval via workflow
    /// </summary>
    public async Task<ScheduleSlotDto> RequestRescheduleAsync(
        Guid slotId,
        DateTime newDate,
        TimeSpan newWindowFrom,
        TimeSpan newWindowTo,
        string reason,
        string? notes,
        Guid? companyId,
        Guid siId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue)
        {
            throw new InvalidOperationException("Company context required for reschedule request");
        }

        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && s.CompanyId == companyId.Value && s.ServiceInstallerId == siId)
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found or SI does not have access");
        }

        if (departmentId.HasValue)
        {
            var order = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (order == null || order.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "Posted")
        {
            throw new InvalidOperationException($"Cannot request reschedule for slot with status '{slot.Status}'. Only Posted slots can be rescheduled.");
        }

        // Check if it's a different day reschedule
        var isDifferentDay = newDate.Date != slot.Date.Date;
        
        if (isDifferentDay)
        {
            // Different day reschedule - transition order to ReschedulePendingApproval via workflow
            var order = await _context.Orders
                .Where(o => o.Id == slot.OrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                throw new InvalidOperationException($"Order {slot.OrderId} not found");
            }

            if (order.Status == "Assigned")
            {
                try
                {
                    var executeDto = new ExecuteTransitionDto
                    {
                        EntityType = "Order",
                        EntityId = order.Id,
                        TargetStatus = "ReschedulePendingApproval",
                        PartnerId = order.PartnerId,
                        DepartmentId = order.DepartmentId,
                        Payload = new Dictionary<string, object>
                        {
                            ["reason"] = $"SI requested reschedule - {reason}",
                            ["source"] = "SIApp",
                            ["siId"] = siId.ToString(),
                            ["metadata"] = new Dictionary<string, object>
                            {
                                ["scheduledSlotId"] = slotId.ToString(),
                                ["oldAppointmentDate"] = slot.Date.ToString("yyyy-MM-dd"),
                                ["oldAppointmentTime"] = slot.WindowFrom.ToString(@"hh\:mm"),
                                ["newAppointmentDate"] = newDate.ToString("yyyy-MM-dd"),
                                ["newAppointmentTime"] = newWindowFrom.ToString(@"hh\:mm"),
                                ["rescheduleReason"] = reason,
                                ["customerContacted"] = true
                            }
                        }
                    };

                    var workflowEngineService = _serviceProvider.GetRequiredService<IWorkflowEngineService>();
                await workflowEngineService.ExecuteTransitionAsync(
                        companyId.Value,
                        executeDto,
                        null, // SI-initiated, no user ID
                        cancellationToken);

                    _logger.LogInformation("Order {OrderId} status changed from Assigned to ReschedulePendingApproval via workflow engine", order.Id);

                    // Send notifications to admin/manager users about the reschedule request
                    await SendRescheduleRequestNotificationsAsync(
                        slot: slot,
                        order: order,
                        siId: siId,
                        newDate: newDate,
                        newWindowFrom: newWindowFrom,
                        newWindowTo: newWindowTo,
                        reason: reason,
                        notes: notes,
                        companyId: companyId.Value,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to transition order {OrderId} to ReschedulePendingApproval status via workflow engine", order.Id);
                    throw new InvalidOperationException($"Failed to request reschedule: {ex.Message}", ex);
                }
            }
        }

        // Update slot with reschedule request
        slot.Status = "RescheduleRequested";
        slot.RescheduleRequestedDate = newDate.Date;
        slot.RescheduleRequestedTime = newWindowFrom;
        slot.RescheduleReason = reason;
        slot.RescheduleNotes = notes;
        slot.RescheduleRequestedBySiId = siId;
        slot.RescheduleRequestedAt = DateTime.UtcNow;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reschedule requested for slot {SlotId} by SI {SiId}", slotId, siId);

        var updatedSlots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return updatedSlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve schedule slot");
    }

    /// <summary>
    /// Admin approves reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
    /// </summary>
    public async Task<ScheduleSlotDto> ApproveRescheduleAsync(
        Guid slotId,
        Guid? companyId,
        Guid userId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue)
        {
            throw new InvalidOperationException("Company context required for reschedule approval");
        }

        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && s.CompanyId == companyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var slotOrder = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (slotOrder == null || slotOrder.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "RescheduleRequested")
        {
            throw new InvalidOperationException($"Cannot approve reschedule for slot with status '{slot.Status}'. Only RescheduleRequested slots can be approved.");
        }

        if (!slot.RescheduleRequestedDate.HasValue || !slot.RescheduleRequestedTime.HasValue)
        {
            throw new InvalidOperationException("Reschedule request details are missing");
        }

        // Get the order
        var order = await _context.Orders
            .Where(o => o.Id == slot.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {slot.OrderId} not found");
        }

        // Update order appointment date/time
        var oldDate = order.AppointmentDate;
        var oldWindowFrom = order.AppointmentWindowFrom;
        var oldWindowTo = order.AppointmentWindowTo;

        if (!slot.RescheduleRequestedDate.HasValue || !slot.RescheduleRequestedTime.HasValue)
        {
            throw new InvalidOperationException("Reschedule request details are missing");
        }

        order.AppointmentDate = slot.RescheduleRequestedDate.Value;
        order.AppointmentWindowFrom = slot.RescheduleRequestedTime.Value;
        // Calculate windowTo based on original duration
        var originalDuration = slot.WindowTo - slot.WindowFrom;
        order.AppointmentWindowTo = slot.RescheduleRequestedTime.Value.Add(originalDuration);
        order.UpdatedAt = DateTime.UtcNow;

        // If order is ReschedulePendingApproval, transition back to Assigned via workflow
        if (order.Status == "ReschedulePendingApproval")
        {
            try
            {
                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = order.Id,
                    TargetStatus = "Assigned",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = "Reschedule approved by admin",
                        ["userId"] = userId.ToString(),
                        ["source"] = "Scheduler",
                        ["metadata"] = new Dictionary<string, object>
                        {
                            ["scheduledSlotId"] = slotId.ToString(),
                            ["oldAppointmentDate"] = oldDate.ToString("yyyy-MM-dd"),
                            ["oldAppointmentTime"] = oldWindowFrom.ToString(@"hh\:mm"),
                            ["newAppointmentDate"] = slot.RescheduleRequestedDate.Value.ToString("yyyy-MM-dd"),
                            ["newAppointmentTime"] = slot.RescheduleRequestedTime.Value.ToString(@"hh\:mm"),
                            ["approvedBy"] = userId.ToString()
                        }
                    }
                };

                var workflowEngineService = _serviceProvider.GetRequiredService<IWorkflowEngineService>();
                await workflowEngineService.ExecuteTransitionAsync(
                    companyId.Value,
                    executeDto,
                    userId,
                    cancellationToken);

                _logger.LogInformation("Order {OrderId} status changed from ReschedulePendingApproval to Assigned via workflow engine", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition order {OrderId} to Assigned status via workflow engine", order.Id);
                throw new InvalidOperationException($"Failed to approve reschedule: {ex.Message}", ex);
            }
        }

        // Update slot with new date/time and status
        slot.Date = slot.RescheduleRequestedDate.Value;
        slot.WindowFrom = slot.RescheduleRequestedTime.Value;
        var newWindowTo = slot.RescheduleRequestedTime.Value.Add(slot.WindowTo - slot.WindowFrom);
        slot.WindowTo = newWindowTo;
        slot.Status = "RescheduleApproved";
        slot.UpdatedAt = DateTime.UtcNow;

        // Clear reschedule request fields
        slot.RescheduleRequestedDate = null;
        slot.RescheduleRequestedTime = null;
        slot.RescheduleReason = null;
        slot.RescheduleNotes = null;
        slot.RescheduleRequestedBySiId = null;
        slot.RescheduleRequestedAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reschedule approved for slot {SlotId} by user {UserId}", slotId, userId);

        var updatedSlots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return updatedSlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve schedule slot");
    }

    /// <summary>
    /// Admin rejects reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
    /// </summary>
    public async Task<ScheduleSlotDto> RejectRescheduleAsync(
        Guid slotId,
        string rejectionReason,
        Guid? companyId,
        Guid userId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue)
        {
            throw new InvalidOperationException("Company context required for reschedule rejection");
        }

        var slot = await _context.ScheduledSlots
            .Where(s => s.Id == slotId && s.CompanyId == companyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            throw new InvalidOperationException("Schedule slot not found");
        }

        if (departmentId.HasValue)
        {
            var slotOrder = await _context.Orders.Where(o => o.Id == slot.OrderId).FirstOrDefaultAsync(cancellationToken);
            if (slotOrder == null || slotOrder.DepartmentId != departmentId.Value)
                throw new UnauthorizedAccessException("Order not in your department.");
            var si = await _context.ServiceInstallers.Where(s => s.Id == slot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
            if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                throw new UnauthorizedAccessException("Service installer not in your department.");
        }

        if (slot.Status != "RescheduleRequested")
        {
            throw new InvalidOperationException($"Cannot reject reschedule for slot with status '{slot.Status}'. Only RescheduleRequested slots can be rejected.");
        }

        // Get the order
        var order = await _context.Orders
            .Where(o => o.Id == slot.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {slot.OrderId} not found");
        }

        // If order is ReschedulePendingApproval, transition back to Assigned via workflow
        if (order.Status == "ReschedulePendingApproval")
        {
            try
            {
                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = order.Id,
                    TargetStatus = "Assigned",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = $"Reschedule rejected - {rejectionReason}",
                        ["userId"] = userId.ToString(),
                        ["source"] = "Scheduler",
                        ["metadata"] = new Dictionary<string, object>
                        {
                            ["scheduledSlotId"] = slotId.ToString(),
                            ["rejectionReason"] = rejectionReason,
                            ["adminNotes"] = rejectionReason
                        }
                    }
                };

                var workflowEngineService = _serviceProvider.GetRequiredService<IWorkflowEngineService>();
                await workflowEngineService.ExecuteTransitionAsync(
                    companyId.Value,
                    executeDto,
                    userId,
                    cancellationToken);

                _logger.LogInformation("Order {OrderId} status changed from ReschedulePendingApproval to Assigned via workflow engine (rejected)", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition order {OrderId} to Assigned status via workflow engine", order.Id);
                throw new InvalidOperationException($"Failed to reject reschedule: {ex.Message}", ex);
            }
        }

        // Update slot status to Rejected and clear reschedule request fields
        slot.Status = "RescheduleRejected";
        slot.RescheduleNotes = rejectionReason; // Store rejection reason in notes
        slot.UpdatedAt = DateTime.UtcNow;

        // Clear reschedule request fields
        slot.RescheduleRequestedDate = null;
        slot.RescheduleRequestedTime = null;
        slot.RescheduleReason = null;
        slot.RescheduleRequestedBySiId = null;
        slot.RescheduleRequestedAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reschedule rejected for slot {SlotId} by user {UserId}", slotId, userId);

        var updatedSlots = await GetScheduleSlotsAsync(companyId, slot.ServiceInstallerId, slot.Date, null, departmentId, cancellationToken);
        return updatedSlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new InvalidOperationException("Failed to retrieve schedule slot");
    }

    /// <summary>
    /// Detect scheduling conflicts for a given order or slot
    /// Checks for overlapping appointments for the same SI
    /// </summary>
    public async Task<List<ScheduleConflictDto>> DetectSchedulingConflictsAsync(
        Guid? orderId,
        Guid? slotId,
        Guid? siId,
        DateTime? date,
        Guid? companyId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var conflicts = new List<ScheduleConflictDto>();

        // If slotId is provided, get the slot details
        Domain.Scheduler.Entities.ScheduledSlot? targetSlot = null;
        if (slotId.HasValue)
        {
            var slotQuery = _context.ScheduledSlots
                .Where(s => s.Id == slotId.Value && (!companyId.HasValue || s.CompanyId == companyId.Value));
            targetSlot = await slotQuery.FirstOrDefaultAsync(cancellationToken);

            if (targetSlot == null)
            {
                return conflicts; // Slot not found, no conflicts
            }

            if (departmentId.HasValue)
            {
                var order = await _context.Orders.Where(o => o.Id == targetSlot.OrderId).FirstOrDefaultAsync(cancellationToken);
                if (order == null || order.DepartmentId != departmentId.Value)
                    return conflicts;
                var si = await _context.ServiceInstallers.Where(s => s.Id == targetSlot.ServiceInstallerId).FirstOrDefaultAsync(cancellationToken);
                if (si == null || (si.DepartmentId != null && si.DepartmentId != departmentId.Value))
                    return conflicts;
            }

            orderId = targetSlot.OrderId;
            siId = targetSlot.ServiceInstallerId;
            date = targetSlot.Date;
        }

        if (!siId.HasValue || !date.HasValue)
        {
            return conflicts; // Cannot detect conflicts without SI and date
        }

        // Get all slots for this SI on this date (excluding the target slot if provided)
        var query = _context.ScheduledSlots
            .Where(s => s.ServiceInstallerId == siId.Value &&
                       s.Date.Date == date.Value.Date &&
                       s.Status != "Cancelled" &&
                       (!companyId.HasValue || s.CompanyId == companyId.Value));

        if (slotId.HasValue)
        {
            query = query.Where(s => s.Id != slotId.Value);
        }

        // When department-scoped, only consider slots whose order and SI are in that department
        if (departmentId.HasValue)
        {
            var orderIdsInDept = await _context.Orders
                .Where(o => o.DepartmentId == departmentId.Value)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            var siIdsInDept = await _context.ServiceInstallers
                .Where(si => si.DepartmentId == null || si.DepartmentId == departmentId.Value)
                .Select(si => si.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(s => orderIdsInDept.Contains(s.OrderId) && siIdsInDept.Contains(s.ServiceInstallerId));
        }

        var existingSlots = await query.ToListAsync(cancellationToken);

        // Calculate target time window
        TimeSpan targetWindowFrom;
        TimeSpan targetWindowTo;

        if (targetSlot != null)
        {
            targetWindowFrom = targetSlot.WindowFrom;
            targetWindowTo = targetSlot.WindowTo;
        }
        else if (orderId.HasValue)
        {
            // Get order appointment window
            var order = await _context.Orders
                .Where(o => o.Id == orderId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                return conflicts; // Order not found
            }

            targetWindowFrom = order.AppointmentWindowFrom;
            targetWindowTo = order.AppointmentWindowTo;
        }
        else
        {
            return conflicts; // Cannot detect conflicts without slot or order
        }

        // Check for overlaps
        foreach (var existingSlot in existingSlots)
        {
            // Check if time windows overlap
            bool overlaps = (targetWindowFrom < existingSlot.WindowTo && targetWindowTo > existingSlot.WindowFrom);

            if (overlaps)
            {
                // Get order details for conflict info
                var conflictingOrder = await _context.Orders
                    .Where(o => o.Id == existingSlot.OrderId)
                    .FirstOrDefaultAsync(cancellationToken);

                conflicts.Add(new ScheduleConflictDto
                {
                    SlotId = existingSlot.Id,
                    OrderId = existingSlot.OrderId,
                    ServiceInstallerId = existingSlot.ServiceInstallerId,
                    Date = existingSlot.Date,
                    WindowFrom = existingSlot.WindowFrom,
                    WindowTo = existingSlot.WindowTo,
                    Status = existingSlot.Status,
                    OrderServiceId = conflictingOrder?.ServiceId,
                    OrderCustomerName = conflictingOrder?.CustomerName,
                    OrderBuildingName = conflictingOrder?.BuildingName,
                    ConflictType = "TimeOverlap",
                    ConflictDescription = $"Overlaps with existing appointment: {existingSlot.WindowFrom:hh\\:mm} - {existingSlot.WindowTo:hh\\:mm}"
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Send notifications to SI and admin when scheduling conflicts are detected
    /// </summary>
    private async Task SendConflictNotificationsAsync(
        List<ScheduleConflictDto> conflicts,
        Domain.Scheduler.Entities.ScheduledSlot slot,
        Order order,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get SI details to find their UserId
            var si = await _context.ServiceInstallers
                .Where(s => s.Id == slot.ServiceInstallerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (si == null)
            {
                _logger.LogWarning("ServiceInstaller {SiId} not found for conflict notification", slot.ServiceInstallerId);
                return;
            }

            // Build conflict details message
            var conflictDetails = string.Join("\n", conflicts.Select((c, idx) => 
                $"{idx + 1}. {c.ConflictDescription} - Order: {c.OrderServiceId} ({c.OrderCustomerName})"));

            var conflictSummary = conflicts.Count == 1
                ? "1 scheduling conflict detected"
                : $"{conflicts.Count} scheduling conflicts detected";

            // Prepare metadata for notifications
            var metadata = new Dictionary<string, object>
            {
                ["slotId"] = slot.Id.ToString(),
                ["orderId"] = order.Id.ToString(),
                ["serviceInstallerId"] = slot.ServiceInstallerId.ToString(),
                ["date"] = slot.Date.ToString("yyyy-MM-dd"),
                ["conflictCount"] = conflicts.Count,
                ["conflicts"] = conflicts.Select(c => new Dictionary<string, object>
                {
                    ["slotId"] = c.SlotId.ToString(),
                    ["orderId"] = c.OrderId.ToString(),
                    ["conflictType"] = c.ConflictType,
                    ["conflictDescription"] = c.ConflictDescription,
                    ["windowFrom"] = c.WindowFrom.ToString(@"hh\:mm"),
                    ["windowTo"] = c.WindowTo.ToString(@"hh\:mm")
                }).ToList()
            };

            var metadataJson = JsonSerializer.Serialize(metadata);

            // Send notification to SI (if they have a UserId)
            if (si.UserId.HasValue)
            {
                var siNotification = new CreateNotificationDto
                {
                    CompanyId = companyId,
                    UserId = si.UserId.Value,
                    Type = "SchedulingConflict",
                    Priority = "High",
                    Title = $"⚠️ Scheduling Conflict: {conflictSummary}",
                    Message = $"Your appointment for Order {order.ServiceId} on {slot.Date:dd MMM yyyy} at {slot.WindowFrom:hh\\:mm} has conflicts:\n\n{conflictDetails}\n\nPlease coordinate with admin to resolve.",
                    ActionUrl = $"/scheduler/timeline?date={slot.Date:yyyy-MM-dd}&siId={slot.ServiceInstallerId}",
                    ActionText = "View Schedule",
                    RelatedEntityId = slot.Id,
                    RelatedEntityType = "ScheduledSlot",
                    MetadataJson = metadataJson,
                    DeliveryChannels = "InApp"
                };

                await _notificationService.CreateNotificationAsync(siNotification, cancellationToken);
                _logger.LogInformation("Conflict notification sent to SI {SiId} (UserId: {UserId})", slot.ServiceInstallerId, si.UserId.Value);
            }
            else
            {
                _logger.LogInformation("SI {SiId} has no UserId, skipping conflict notification", slot.ServiceInstallerId);
            }

            // Send notifications to admin users
            var adminUserIds = await _notificationService.ResolveUsersByRoleAsync("Admin", companyId, cancellationToken);
            var managerUserIds = await _notificationService.ResolveUsersByRoleAsync("Manager", companyId, cancellationToken);
            var allAdminUserIds = adminUserIds.Union(managerUserIds).Distinct().ToList();

            foreach (var adminUserId in allAdminUserIds)
            {
                var adminNotification = new CreateNotificationDto
                {
                    CompanyId = companyId,
                    UserId = adminUserId,
                    Type = "SchedulingConflict",
                    Priority = "High",
                    Title = $"⚠️ Scheduling Conflict: {conflictSummary}",
                    Message = $"SI {si.Name} has a scheduling conflict for Order {order.ServiceId} on {slot.Date:dd MMM yyyy}:\n\n{conflictDetails}\n\nPlease review and resolve the conflict.",
                    ActionUrl = $"/scheduler/timeline?date={slot.Date:yyyy-MM-dd}&siId={slot.ServiceInstallerId}",
                    ActionText = "View Schedule",
                    RelatedEntityId = slot.Id,
                    RelatedEntityType = "ScheduledSlot",
                    MetadataJson = metadataJson,
                    DeliveryChannels = "InApp"
                };

                await _notificationService.CreateNotificationAsync(adminNotification, cancellationToken);
            }

            _logger.LogInformation("Conflict notifications sent to {AdminCount} admin/manager users", allAdminUserIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending conflict notifications for slot {SlotId}", slot.Id);
            // Don't throw - notification failure shouldn't block the posting operation
        }
    }

    /// <summary>
    /// Send notifications to admin/manager users when SI requests a reschedule
    /// </summary>
    private async Task SendRescheduleRequestNotificationsAsync(
        Domain.Scheduler.Entities.ScheduledSlot slot,
        Order order,
        Guid siId,
        DateTime newDate,
        TimeSpan newWindowFrom,
        TimeSpan newWindowTo,
        string reason,
        string? notes,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get SI details for notification message
            var si = await _context.ServiceInstallers
                .Where(s => s.Id == siId)
                .FirstOrDefaultAsync(cancellationToken);

            var siName = si?.Name ?? "Service Installer";

            // Build notification message
            var message = $"SI {siName} has requested to reschedule Order {order.ServiceId}:\n\n";
            message += $"📅 Current: {slot.Date:dd MMM yyyy} at {slot.WindowFrom:hh\\:mm}\n";
            message += $"📅 Requested: {newDate:dd MMM yyyy} at {newWindowFrom:hh\\:mm}\n";
            message += $"📝 Reason: {reason}\n";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                message += $"💬 Notes: {notes}\n";
            }
            message += $"\nCustomer: {order.CustomerName}\n";
            message += $"Location: {order.BuildingName ?? order.AddressLine1}";

            // Prepare metadata for notifications
            var metadata = new Dictionary<string, object>
            {
                ["slotId"] = slot.Id.ToString(),
                ["orderId"] = order.Id.ToString(),
                ["serviceInstallerId"] = siId.ToString(),
                ["oldDate"] = slot.Date.ToString("yyyy-MM-dd"),
                ["oldWindowFrom"] = slot.WindowFrom.ToString(@"hh\:mm"),
                ["oldWindowTo"] = slot.WindowTo.ToString(@"hh\:mm"),
                ["newDate"] = newDate.ToString("yyyy-MM-dd"),
                ["newWindowFrom"] = newWindowFrom.ToString(@"hh\:mm"),
                ["newWindowTo"] = newWindowTo.ToString(@"hh\:mm"),
                ["reason"] = reason,
                ["notes"] = notes ?? string.Empty
            };

            var metadataJson = JsonSerializer.Serialize(metadata);

            // Send notifications to admin and manager users
            var adminUserIds = await _notificationService.ResolveUsersByRoleAsync("Admin", companyId, cancellationToken);
            var managerUserIds = await _notificationService.ResolveUsersByRoleAsync("Manager", companyId, cancellationToken);
            var allAdminUserIds = adminUserIds.Union(managerUserIds).Distinct().ToList();

            foreach (var adminUserId in allAdminUserIds)
            {
                var notification = new CreateNotificationDto
                {
                    CompanyId = companyId,
                    UserId = adminUserId,
                    Type = "RescheduleRequest",
                    Priority = "Normal",
                    Title = $"🔄 Reschedule Request: Order {order.ServiceId}",
                    Message = message,
                    ActionUrl = $"/scheduler/timeline?date={slot.Date:yyyy-MM-dd}&slotId={slot.Id}",
                    ActionText = "Review Request",
                    RelatedEntityId = slot.Id,
                    RelatedEntityType = "ScheduledSlot",
                    MetadataJson = metadataJson,
                    DeliveryChannels = "InApp"
                };

                await _notificationService.CreateNotificationAsync(notification, cancellationToken);
            }

            _logger.LogInformation("Reschedule request notifications sent to {AdminCount} admin/manager users for slot {SlotId}", 
                allAdminUserIds.Count, slot.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reschedule request notifications for slot {SlotId}", slot.Id);
            // Don't throw - notification failure shouldn't block the reschedule request
        }
    }
}

