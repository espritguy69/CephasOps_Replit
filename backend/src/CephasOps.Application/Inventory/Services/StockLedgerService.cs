using CephasOps.Application.Events;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.Inventory.Enums;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Ledger-based inventory: immutable entries, balance from SUM(ledger), allocation prevents double-use.
/// Does not update StockBalance.Quantity.
/// </summary>
public class StockLedgerService : IStockLedgerService
{
    /// <summary>Maximum page size for ledger list (guardrail for performance).</summary>
    public const int MaxLedgerPageSize = 200;

    /// <summary>Maximum date range in days for usage-summary export (guardrail for unbounded load).</summary>
    public const int MaxUsageSummaryDateRangeDays = 366;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<StockLedgerService> _logger;
    private readonly IEventBus? _eventBus;
    private readonly bool _isTesting;

    public StockLedgerService(ApplicationDbContext context, ILogger<StockLedgerService> logger, IEventBus? eventBus = null, IHostEnvironment? hostEnvironment = null)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
        _isTesting = hostEnvironment != null && string.Equals(hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LedgerWriteResultDto> ReceiveAsync(LedgerReceiveRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        await ValidateMaterialAndLocationAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, departmentId, cancellationToken);

        var material = await GetMaterialAsync(dto.MaterialId, cancellationToken);
        if (material == null) throw new InvalidOperationException("Material not found.");
        if (material.IsSerialised && string.IsNullOrWhiteSpace(dto.SerialNumber))
            throw new InvalidOperationException("Serial number is required for serialised material.");

        Guid? serialisedItemId = null;
        if (material.IsSerialised && !string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            var serial = dto.SerialNumber!.Trim();
            var serialQuery = _context.SerialisedItems.Where(s => s.CompanyId == effectiveCompanyId && s.SerialNumber == serial && !s.IsDeleted);
            if (_isTesting) serialQuery = serialQuery.IgnoreQueryFilters();
            var existing = await serialQuery.FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"Serial number '{serial}' already exists.");
            var newItem = new SerialisedItem
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                MaterialId = dto.MaterialId,
                SerialNumber = serial,
                CurrentLocationId = dto.LocationId,
                Status = "InWarehouse",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SerialisedItems.Add(newItem);
            await _context.SaveChangesAsync(cancellationToken);
            serialisedItemId = newItem.Id;
        }

        var entry = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            EntryType = StockLedgerEntryType.Receive,
            MaterialId = dto.MaterialId,
            LocationId = dto.LocationId,
            Quantity = dto.Quantity,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            CreatedByUserId = userId,
            Remarks = dto.Remarks,
            SerialisedItemId = serialisedItemId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockLedgerEntries.Add(entry);
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.LocationId, material.DepartmentId, dto.Quantity, 0, entry.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger Receive: EntryId={EntryId}, MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}, DeptId={DeptId}, Ref={RefType}/{RefId}",
            entry.Id, dto.MaterialId, dto.LocationId, dto.Quantity, departmentId, dto.ReferenceType, dto.ReferenceId);

        return new LedgerWriteResultDto
        {
            LedgerEntryId = entry.Id,
            EntryType = nameof(StockLedgerEntryType.Receive),
            Message = "Stock received successfully."
        };
    }

    public async Task<LedgerWriteResultDto> TransferAsync(LedgerTransferRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        await ValidateMaterialAndLocationAsync(dto.MaterialId, dto.FromLocationId, effectiveCompanyId, departmentId, cancellationToken);
        await ValidateLocationAsync(dto.ToLocationId, effectiveCompanyId, cancellationToken);
        if (dto.FromLocationId == dto.ToLocationId)
            throw new InvalidOperationException("From and To location must be different.");

        var available = await GetAvailableQuantityAsync(dto.MaterialId, dto.FromLocationId, effectiveCompanyId, cancellationToken);
        if (available < dto.Quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {available}, requested: {dto.Quantity}.");

        var outEntry = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            EntryType = StockLedgerEntryType.Transfer,
            MaterialId = dto.MaterialId,
            LocationId = dto.FromLocationId,
            Quantity = -dto.Quantity,
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            CreatedByUserId = userId,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inEntry = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            EntryType = StockLedgerEntryType.Transfer,
            MaterialId = dto.MaterialId,
            LocationId = dto.ToLocationId,
            Quantity = dto.Quantity,
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            CreatedByUserId = userId,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockLedgerEntries.Add(outEntry);
        _context.StockLedgerEntries.Add(inEntry);
        var materialTransfer = await GetMaterialAsync(dto.MaterialId, cancellationToken);
        var deptId = materialTransfer?.DepartmentId ?? Guid.Empty;
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.FromLocationId, deptId, -dto.Quantity, 0, outEntry.Id, cancellationToken);
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.ToLocationId, deptId, dto.Quantity, 0, inEntry.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger Transfer: MaterialId={MaterialId}, From={From}, To={To}, Qty={Qty}, DeptId={DeptId}",
            dto.MaterialId, dto.FromLocationId, dto.ToLocationId, dto.Quantity, departmentId);

        return new LedgerWriteResultDto
        {
            LedgerEntryId = inEntry.Id,
            EntryType = nameof(StockLedgerEntryType.Transfer),
            Message = "Stock transferred successfully."
        };
    }

    public async Task<LedgerWriteResultDto> AllocateAsync(LedgerAllocateRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        await ValidateOrderDepartmentAsync(dto.OrderId, departmentId, cancellationToken);
        await ValidateMaterialAndLocationAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, departmentId, cancellationToken);

        var material = await GetMaterialAsync(dto.MaterialId, cancellationToken);
        if (material == null) throw new InvalidOperationException("Material not found.");

        Guid? serialisedItemId = null;
        if (material.IsSerialised && !string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            var serial = dto.SerialNumber!.Trim();
            var siQuery = _context.SerialisedItems.Where(s => s.CompanyId == effectiveCompanyId && s.SerialNumber == serial && !s.IsDeleted);
            if (_isTesting) siQuery = siQuery.IgnoreQueryFilters();
            var si = await siQuery.FirstOrDefaultAsync(cancellationToken);
            if (si == null) throw new InvalidOperationException($"Serial number '{serial}' not found.");
            if (si.MaterialId != dto.MaterialId) throw new InvalidOperationException("Serial does not match material.");
            var alreadyAllocated = await _context.StockAllocations
                .AnyAsync(a => a.SerialisedItemId == si.Id && a.Status != StockAllocationStatus.Cancelled && a.Status != StockAllocationStatus.Returned && !a.IsDeleted, cancellationToken);
            if (alreadyAllocated) throw new InvalidOperationException($"Serial '{serial}' is already allocated or issued.");
            serialisedItemId = si.Id;
        }

        var available = await GetAvailableQuantityAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, cancellationToken);
        var toReserve = dto.Quantity;
        if (available < toReserve)
            throw new InvalidOperationException($"Insufficient available stock. Available: {available}, requested: {toReserve}.");

        var allocation = new StockAllocation
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            MaterialId = dto.MaterialId,
            SerialisedItemId = serialisedItemId,
            LocationId = dto.LocationId,
            Quantity = toReserve,
            OrderId = dto.OrderId,
            Status = StockAllocationStatus.Reserved,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockAllocations.Add(allocation);
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.LocationId, material.DepartmentId, 0, toReserve, null, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger Allocate: AllocationId={AllocationId}, OrderId={OrderId}, MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}, DeptId={DeptId}",
            allocation.Id, dto.OrderId, dto.MaterialId, dto.LocationId, toReserve, departmentId);

        return new LedgerWriteResultDto
        {
            AllocationId = allocation.Id,
            EntryType = "Allocate",
            Message = "Stock allocated (reserved) successfully."
        };
    }

    public async Task<LedgerWriteResultDto> IssueAsync(LedgerIssueRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        await ValidateOrderDepartmentAsync(dto.OrderId, departmentId, cancellationToken);
        await ValidateMaterialAndLocationAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, departmentId, cancellationToken);

        StockAllocation? allocation = null;
        if (dto.AllocationId.HasValue)
        {
            var allocQuery = _context.StockAllocations.Where(a => a.Id == dto.AllocationId.Value && !a.IsDeleted);
            if (_isTesting) allocQuery = allocQuery.IgnoreQueryFilters();
            allocation = await allocQuery.FirstOrDefaultAsync(cancellationToken);
            if (allocation == null) throw new InvalidOperationException("Allocation not found.");
            if (allocation.Status != StockAllocationStatus.Reserved)
                throw new InvalidOperationException("Allocation is not in Reserved status.");
            if (allocation.OrderId != dto.OrderId || allocation.MaterialId != dto.MaterialId || allocation.LocationId != dto.LocationId)
                throw new InvalidOperationException("Allocation does not match request.");
            if (allocation.Quantity < dto.Quantity)
                throw new InvalidOperationException("Requested quantity exceeds allocated quantity.");
        }
        else
        {
            var available = await GetAvailableQuantityAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, cancellationToken);
            if (available < dto.Quantity)
                throw new InvalidOperationException($"Insufficient available stock. Available: {available}, requested: {dto.Quantity}.");
        }

        var entry = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            EntryType = StockLedgerEntryType.Issue,
            MaterialId = dto.MaterialId,
            LocationId = dto.LocationId,
            Quantity = -dto.Quantity,
            OrderId = dto.OrderId,
            AllocationId = dto.AllocationId,
            CreatedByUserId = userId,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockLedgerEntries.Add(entry);
        if (allocation != null)
        {
            allocation.Status = StockAllocationStatus.Issued;
            allocation.LedgerEntryIdIssued = entry.Id;
            allocation.UpdatedAt = DateTime.UtcNow;
        }
        var materialIssue = await GetMaterialAsync(dto.MaterialId, cancellationToken);
        var deptIdIssue = materialIssue?.DepartmentId ?? Guid.Empty;
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.LocationId, deptIdIssue, -dto.Quantity, allocation != null ? -dto.Quantity : 0, entry.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger Issue: EntryId={EntryId}, OrderId={OrderId}, MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}, DeptId={DeptId}",
            entry.Id, dto.OrderId, dto.MaterialId, dto.LocationId, dto.Quantity, departmentId);

        return new LedgerWriteResultDto
        {
            LedgerEntryId = entry.Id,
            AllocationId = dto.AllocationId,
            EntryType = nameof(StockLedgerEntryType.Issue),
            Message = "Stock issued successfully."
        };
    }

    public async Task<LedgerWriteResultDto> ReturnAsync(LedgerReturnRequestDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        await ValidateOrderDepartmentAsync(dto.OrderId, departmentId, cancellationToken);
        await ValidateMaterialAndLocationAsync(dto.MaterialId, dto.LocationId, effectiveCompanyId, departmentId, cancellationToken);

        StockAllocation? allocation = null;
        if (dto.AllocationId.HasValue)
        {
            var returnAllocQuery = _context.StockAllocations.Where(a => a.Id == dto.AllocationId.Value && !a.IsDeleted);
            if (_isTesting) returnAllocQuery = returnAllocQuery.IgnoreQueryFilters();
            allocation = await returnAllocQuery.FirstOrDefaultAsync(cancellationToken);
            if (allocation == null) throw new InvalidOperationException("Allocation not found.");
            if (allocation.Status != StockAllocationStatus.Issued)
                throw new InvalidOperationException("Allocation is not in Issued status; cannot return.");
        }

        var entry = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId,
            EntryType = StockLedgerEntryType.Return,
            MaterialId = dto.MaterialId,
            LocationId = dto.LocationId,
            Quantity = dto.Quantity,
            OrderId = dto.OrderId,
            AllocationId = dto.AllocationId,
            CreatedByUserId = userId,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockLedgerEntries.Add(entry);
        if (allocation != null)
        {
            allocation.Status = StockAllocationStatus.Returned;
            allocation.LedgerEntryIdReturned = entry.Id;
            allocation.UpdatedAt = DateTime.UtcNow;
        }
        var materialReturn = await GetMaterialAsync(dto.MaterialId, cancellationToken);
        var deptIdReturn = materialReturn?.DepartmentId ?? Guid.Empty;
        await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.LocationId, deptIdReturn, dto.Quantity, 0, entry.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ledger Return: EntryId={EntryId}, OrderId={OrderId}, MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}, DeptId={DeptId}",
            entry.Id, dto.OrderId, dto.MaterialId, dto.LocationId, dto.Quantity, departmentId);

        if (_eventBus != null && effectiveCompanyId != Guid.Empty)
        {
            var evt = new MaterialReturnedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                CompanyId = effectiveCompanyId,
                TriggeredByUserId = userId,
                OrderId = dto.OrderId,
                MaterialId = dto.MaterialId,
                Quantity = dto.Quantity,
                SerialNumber = dto.SerialNumber,
                ReturnReason = dto.Remarks
            };
            await _eventBus.PublishAsync(evt, cancellationToken);
        }

        return new LedgerWriteResultDto
        {
            LedgerEntryId = entry.Id,
            AllocationId = dto.AllocationId,
            EntryType = nameof(StockLedgerEntryType.Return),
            Message = "Stock returned successfully."
        };
    }

    /// <inheritdoc />
    /// <remarks>Legacy path only. Do not use for new code. Does not update StockBalance.Quantity; ledger is the single source of truth.</remarks>
    public async Task RecordLegacyMovementAsync(CreateStockMovementDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RecordLegacyMovementAsync invoked (legacy path). MovementType={MovementType}, MaterialId={MaterialId}. Ledger is source of truth.", dto.MovementType, dto.MaterialId);
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var code = (dto.MovementType ?? "").Trim();
        Guid? serialisedItemId = null;
        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            var siLookupQuery = _context.SerialisedItems.Where(s => s.CompanyId == effectiveCompanyId && s.MaterialId == dto.MaterialId && s.SerialNumber == dto.SerialNumber.Trim() && !s.IsDeleted);
            if (_isTesting) siLookupQuery = siLookupQuery.IgnoreQueryFilters();
            var si = await siLookupQuery.FirstOrDefaultAsync(cancellationToken);
            serialisedItemId = si?.Id;
        }

        if (IsTransferType(code) && dto.FromLocationId.HasValue && dto.ToLocationId.HasValue && dto.FromLocationId != dto.ToLocationId)
        {
            var outEntry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = StockLedgerEntryType.Transfer,
                MaterialId = dto.MaterialId,
                LocationId = dto.FromLocationId.Value,
                Quantity = -dto.Quantity,
                FromLocationId = dto.FromLocationId,
                ToLocationId = dto.ToLocationId,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var inEntry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = StockLedgerEntryType.Transfer,
                MaterialId = dto.MaterialId,
                LocationId = dto.ToLocationId.Value,
                Quantity = dto.Quantity,
                FromLocationId = dto.FromLocationId,
                ToLocationId = dto.ToLocationId,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLedgerEntries.Add(outEntry);
            _context.StockLedgerEntries.Add(inEntry);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.FromLocationId.Value, null, -dto.Quantity, 0, null, cancellationToken);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.ToLocationId.Value, null, dto.Quantity, 0, null, cancellationToken);
            _logger.LogInformation("Ledger legacy Transfer: MaterialId={MaterialId}, From={From}, To={To}, Qty={Qty}",
                dto.MaterialId, dto.FromLocationId, dto.ToLocationId, dto.Quantity);
            return;
        }

        if (IsReceiveType(code) && dto.ToLocationId.HasValue)
        {
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = StockLedgerEntryType.Receive,
                MaterialId = dto.MaterialId,
                LocationId = dto.ToLocationId.Value,
                Quantity = dto.Quantity,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLedgerEntries.Add(entry);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.ToLocationId.Value, null, dto.Quantity, 0, null, cancellationToken);
            _logger.LogInformation("Ledger legacy Receive: MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}",
                dto.MaterialId, dto.ToLocationId, dto.Quantity);
            return;
        }

        if (IsIssueType(code) && dto.FromLocationId.HasValue)
        {
            var entryType = string.Equals(code, "WriteOff", StringComparison.OrdinalIgnoreCase) ? StockLedgerEntryType.Scrap : StockLedgerEntryType.Issue;
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = entryType,
                MaterialId = dto.MaterialId,
                LocationId = dto.FromLocationId.Value,
                Quantity = -dto.Quantity,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLedgerEntries.Add(entry);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.FromLocationId.Value, null, -dto.Quantity, 0, null, cancellationToken);
            _logger.LogInformation("Ledger legacy {EntryType}: MaterialId={MaterialId}, LocationId={LocationId}, Qty={Qty}",
                entryType, dto.MaterialId, dto.FromLocationId, dto.Quantity);
            return;
        }

        if (IsAdjustType(code))
        {
            if (dto.ToLocationId.HasValue)
            {
                var entry = new StockLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CompanyId = effectiveCompanyId,
                    EntryType = StockLedgerEntryType.Adjust,
                    MaterialId = dto.MaterialId,
                    LocationId = dto.ToLocationId.Value,
                    Quantity = dto.Quantity,
                    CreatedByUserId = userId,
                    Remarks = dto.Remarks,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StockLedgerEntries.Add(entry);
                await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.ToLocationId.Value, null, dto.Quantity, 0, null, cancellationToken);
            }
            else if (dto.FromLocationId.HasValue)
            {
                var entry = new StockLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CompanyId = effectiveCompanyId,
                    EntryType = StockLedgerEntryType.Adjust,
                    MaterialId = dto.MaterialId,
                    LocationId = dto.FromLocationId.Value,
                    Quantity = -dto.Quantity,
                    CreatedByUserId = userId,
                    Remarks = dto.Remarks,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StockLedgerEntries.Add(entry);
                await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.FromLocationId.Value, null, -dto.Quantity, 0, null, cancellationToken);
            }
            _logger.LogInformation("Ledger legacy Adjust: MaterialId={MaterialId}, Qty={Qty}", dto.MaterialId, dto.Quantity);
            return;
        }

        if (dto.FromLocationId.HasValue)
        {
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = StockLedgerEntryType.Issue,
                MaterialId = dto.MaterialId,
                LocationId = dto.FromLocationId.Value,
                Quantity = -dto.Quantity,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLedgerEntries.Add(entry);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.FromLocationId.Value, null, -dto.Quantity, 0, null, cancellationToken);
        }
        else if (dto.ToLocationId.HasValue)
        {
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = effectiveCompanyId,
                EntryType = StockLedgerEntryType.Receive,
                MaterialId = dto.MaterialId,
                LocationId = dto.ToLocationId.Value,
                Quantity = dto.Quantity,
                OrderId = dto.OrderId,
                SerialisedItemId = serialisedItemId,
                CreatedByUserId = userId,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLedgerEntries.Add(entry);
            await ApplyBalanceCacheDeltaAsync(effectiveCompanyId, dto.MaterialId, dto.ToLocationId.Value, null, dto.Quantity, 0, null, cancellationToken);
        }
        _logger.LogInformation("Ledger legacy fallback: MovementType={Code}, MaterialId={MaterialId}", code, dto.MaterialId);
    }

    private static bool IsTransferType(string code) =>
        string.Equals(code, "Transfer", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "TransferToRMA", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "ReturnFaulty", StringComparison.OrdinalIgnoreCase);

    private static bool IsReceiveType(string code) =>
        string.Equals(code, "GRN", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "ReturnFromSI", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "ReturnFromCustomer", StringComparison.OrdinalIgnoreCase);

    private static bool IsIssueType(string code) =>
        string.Equals(code, "IssueToSI", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "IssueToOrder", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "WriteOff", StringComparison.OrdinalIgnoreCase);

    private static bool IsAdjustType(string code) =>
        string.Equals(code, "Adjustment", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "AdjustmentDown", StringComparison.OrdinalIgnoreCase);

    public async Task<LedgerListResultDto> GetLedgerAsync(LedgerFilterDto filter, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var query = _context.StockLedgerEntries
            .Include(e => e.Material)
            .Include(e => e.Location)
            .Include(e => e.FromLocation)
            .Include(e => e.ToLocation)
            .Where(e => !e.IsDeleted)
            .AsQueryable();
        if (_isTesting) query = query.IgnoreQueryFilters();

        query = query.Where(e => e.CompanyId == effectiveCompanyId);
        if (departmentId.HasValue)
            query = query.Where(e => e.Material != null && e.Material.DepartmentId == departmentId.Value);
        if (filter.MaterialId.HasValue)
            query = query.Where(e => e.MaterialId == filter.MaterialId.Value);
        if (filter.LocationId.HasValue)
            query = query.Where(e => e.LocationId == filter.LocationId.Value || e.FromLocationId == filter.LocationId.Value || e.ToLocationId == filter.LocationId.Value);
        if (filter.OrderId.HasValue)
            query = query.Where(e => e.OrderId == filter.OrderId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EntryType))
        {
            var typeStr = filter.EntryType.Trim();
            if (Enum.TryParse<StockLedgerEntryType>(typeStr, true, out var et))
                query = query.Where(e => e.EntryType == et);
        }
        if (filter.FromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxLedgerPageSize);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new LedgerEntryDto
            {
                Id = e.Id,
                EntryType = e.EntryType.ToString(),
                MaterialId = e.MaterialId,
                MaterialCode = e.Material != null ? e.Material.ItemCode : null,
                LocationId = e.LocationId,
                LocationName = e.Location != null ? e.Location.Name : null,
                Quantity = e.Quantity,
                FromLocationId = e.FromLocationId,
                FromLocationName = e.FromLocation != null ? e.FromLocation.Name : null,
                ToLocationId = e.ToLocationId,
                ToLocationName = e.ToLocation != null ? e.ToLocation.Name : null,
                OrderId = e.OrderId,
                SerialisedItemId = e.SerialisedItemId,
                AllocationId = e.AllocationId,
                ReferenceType = e.ReferenceType,
                ReferenceId = e.ReferenceId,
                Remarks = e.Remarks,
                CreatedAt = e.CreatedAt,
                CreatedByUserId = e.CreatedByUserId
            })
            .ToListAsync(cancellationToken);

        if (items.Any() && filter.MaterialId.HasValue == false)
        {
            var serialIds = items.Where(i => i.SerialisedItemId.HasValue).Select(i => i.SerialisedItemId!.Value).Distinct().ToList();
            if (serialIds.Any())
            {
                var serialsQuery = _context.SerialisedItems.Where(s => serialIds.Contains(s.Id));
                if (_isTesting) serialsQuery = serialsQuery.IgnoreQueryFilters();
                var serials = await serialsQuery.ToDictionaryAsync(s => s.Id, s => s.SerialNumber, cancellationToken);
                foreach (var i in items.Where(i => i.SerialisedItemId.HasValue))
                    i.SerialNumber = serials.GetValueOrDefault(i.SerialisedItemId!.Value);
            }
        }

        return new LedgerListResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockSummaryResultDto> GetStockSummaryAsync(Guid? companyId, Guid? departmentId, Guid? locationId, Guid? materialId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        List<(Guid MaterialId, Guid LocationId, decimal Qty, decimal Reserved)> onHandWithReserved;

        var cacheQuery = _context.LedgerBalanceCaches
            .Where(c => c.CompanyId == effectiveCompanyId && c.OnHand > 0)
            .AsQueryable();
        if (_isTesting) cacheQuery = cacheQuery.IgnoreQueryFilters();
        if (departmentId.HasValue)
            cacheQuery = cacheQuery.Where(c => c.DepartmentId == departmentId.Value);
        if (locationId.HasValue)
            cacheQuery = cacheQuery.Where(c => c.LocationId == locationId.Value);
        if (materialId.HasValue)
            cacheQuery = cacheQuery.Where(c => c.MaterialId == materialId.Value);

        var cacheRows = await cacheQuery
            .Select(c => new { c.MaterialId, c.LocationId, c.OnHand, c.Reserved })
            .ToListAsync(cancellationToken);
        if (cacheRows.Count > 0)
        {
            onHandWithReserved = cacheRows.Select(c => (c.MaterialId, c.LocationId, c.OnHand, c.Reserved)).ToList();
            goto BuildByLocation;
        }

        var ledgerQuery = _context.StockLedgerEntries.Where(e => !e.IsDeleted && e.CompanyId == effectiveCompanyId).AsQueryable();
        if (_isTesting) ledgerQuery = ledgerQuery.IgnoreQueryFilters();
        if (departmentId.HasValue)
            ledgerQuery = ledgerQuery.Where(e => e.Material != null && e.Material.DepartmentId == departmentId.Value);
        if (locationId.HasValue)
            ledgerQuery = ledgerQuery.Where(e => e.LocationId == locationId.Value);
        if (materialId.HasValue)
            ledgerQuery = ledgerQuery.Where(e => e.MaterialId == materialId.Value);

        var onHand = await ledgerQuery
            .GroupBy(e => new { e.MaterialId, e.LocationId })
            .Select(g => new { g.Key.MaterialId, g.Key.LocationId, Qty = g.Sum(e => e.Quantity) })
            .Where(x => x.Qty > 0)
            .ToListAsync(cancellationToken);

        var reservedQuery = _context.StockAllocations
            .Where(a => !a.IsDeleted && a.Status == StockAllocationStatus.Reserved && a.CompanyId == effectiveCompanyId)
            .AsQueryable();
        if (_isTesting) reservedQuery = reservedQuery.IgnoreQueryFilters();
        if (locationId.HasValue)
            reservedQuery = reservedQuery.Where(a => a.LocationId == locationId.Value);
        if (materialId.HasValue)
            reservedQuery = reservedQuery.Where(a => a.MaterialId == materialId.Value);

        var reserved = await reservedQuery
            .GroupBy(a => new { a.MaterialId, a.LocationId })
            .Select(g => new { g.Key.MaterialId, g.Key.LocationId, Qty = g.Sum(a => a.Quantity) })
            .ToListAsync(cancellationToken);
        var resLookup = reserved.ToDictionary(x => (x.MaterialId, x.LocationId), x => x.Qty);
        onHandWithReserved = onHand.Select(x => (x.MaterialId, x.LocationId, x.Qty, resLookup.GetValueOrDefault((x.MaterialId, x.LocationId), 0))).ToList();

        BuildByLocation:
        var matIds = onHandWithReserved.Select(x => x.MaterialId).Distinct().ToList();
        var locIds = onHandWithReserved.Select(x => x.LocationId).Distinct().ToList();
        var materialsQuery = _context.Materials.Where(m => matIds.Contains(m.Id));
        var locationsQuery = _context.StockLocations.Where(l => locIds.Contains(l.Id));
        if (_isTesting) { materialsQuery = materialsQuery.IgnoreQueryFilters(); locationsQuery = locationsQuery.IgnoreQueryFilters(); }
        var materials = await materialsQuery.ToDictionaryAsync(m => m.Id, m => m, cancellationToken);
        var locations = await locationsQuery.ToDictionaryAsync(l => l.Id, l => l, cancellationToken);

        if (departmentId.HasValue)
            materials = materials.Where(kv => kv.Value.DepartmentId == departmentId.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

        var byLocation = onHandWithReserved
            .Where(x => materials.ContainsKey(x.MaterialId) && locations.ContainsKey(x.LocationId))
            .Select(x => new StockByLocationDto
            {
                MaterialId = x.MaterialId,
                MaterialCode = materials[x.MaterialId].ItemCode,
                MaterialDescription = materials[x.MaterialId].Description,
                LocationId = x.LocationId,
                LocationName = locations[x.LocationId].Name,
                QuantityOnHand = x.Qty,
                QuantityReserved = x.Reserved,
                IsSerialised = materials[x.MaterialId].IsSerialised
            })
            .ToList();

        var serialQuery = _context.SerialisedItems
            .Include(s => s.Material)
            .Include(s => s.CurrentLocation)
            .Where(s => !s.IsDeleted && (s.CompanyId == effectiveCompanyId))
            .AsQueryable();
        if (_isTesting) serialQuery = serialQuery.IgnoreQueryFilters();
        if (departmentId.HasValue)
            serialQuery = serialQuery.Where(s => s.Material != null && s.Material.DepartmentId == departmentId.Value);
        if (materialId.HasValue)
            serialQuery = serialQuery.Where(s => s.MaterialId == materialId.Value);
        if (locationId.HasValue)
            serialQuery = serialQuery.Where(s => s.CurrentLocationId == locationId.Value);

        var serialisedItems = await serialQuery
            .Select(s => new SerialisedStatusDto
            {
                SerialisedItemId = s.Id,
                MaterialId = s.MaterialId,
                MaterialCode = s.Material != null ? s.Material.ItemCode : null,
                SerialNumber = s.SerialNumber,
                CurrentLocationId = s.CurrentLocationId,
                CurrentLocationName = s.CurrentLocation != null ? s.CurrentLocation.Name : null,
                Status = s.Status,
                LastOrderId = s.LastOrderId
            })
            .ToListAsync(cancellationToken);

        return new StockSummaryResultDto
        {
            ByLocation = byLocation,
            SerialisedItems = serialisedItems
        };
    }

    /// <inheritdoc />
    public async Task<List<LedgerDerivedBalanceDto>> GetLedgerDerivedBalancesAsync(Guid? companyId, Guid? locationId, Guid? materialId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        List<(Guid MaterialId, Guid LocationId, decimal OnHand, decimal Reserved)> rows;

        var cacheQuery = _context.LedgerBalanceCaches
            .Where(c => c.CompanyId == effectiveCompanyId && (c.OnHand != 0 || c.Reserved != 0))
            .AsQueryable();
        if (locationId.HasValue)
            cacheQuery = cacheQuery.Where(c => c.LocationId == locationId.Value);
        if (materialId.HasValue)
            cacheQuery = cacheQuery.Where(c => c.MaterialId == materialId.Value);

        var cacheRows = await cacheQuery
            .Select(c => new { c.MaterialId, c.LocationId, c.OnHand, c.Reserved })
            .ToListAsync(cancellationToken);
        if (cacheRows.Count > 0)
        {
            return cacheRows.Select(c => new LedgerDerivedBalanceDto
            {
                MaterialId = c.MaterialId,
                LocationId = c.LocationId,
                OnHand = c.OnHand,
                Reserved = c.Reserved
            }).ToList();
        }

        var ledgerQuery = _context.StockLedgerEntries
            .Where(e => !e.IsDeleted && e.CompanyId == effectiveCompanyId)
            .AsQueryable();
        if (locationId.HasValue)
            ledgerQuery = ledgerQuery.Where(e => e.LocationId == locationId.Value);
        if (materialId.HasValue)
            ledgerQuery = ledgerQuery.Where(e => e.MaterialId == materialId.Value);

        var onHand = await ledgerQuery
            .GroupBy(e => new { e.MaterialId, e.LocationId })
            .Select(g => new { g.Key.MaterialId, g.Key.LocationId, Qty = g.Sum(e => e.Quantity) })
            .ToListAsync(cancellationToken);

        var reservedQuery = _context.StockAllocations
            .Where(a => !a.IsDeleted && a.Status == StockAllocationStatus.Reserved && a.CompanyId == effectiveCompanyId)
            .AsQueryable();
        if (locationId.HasValue)
            reservedQuery = reservedQuery.Where(a => a.LocationId == locationId.Value);
        if (materialId.HasValue)
            reservedQuery = reservedQuery.Where(a => a.MaterialId == materialId.Value);

        var reserved = await reservedQuery
            .GroupBy(a => new { a.MaterialId, a.LocationId })
            .Select(g => new { g.Key.MaterialId, g.Key.LocationId, Qty = g.Sum(a => a.Quantity) })
            .ToListAsync(cancellationToken);

        var onHandLookup = onHand.ToDictionary(x => (x.MaterialId, x.LocationId), x => x.Qty);
        var resLookup = reserved.ToDictionary(x => (x.MaterialId, x.LocationId), x => x.Qty);
        var allKeys = onHandLookup.Keys.Union(resLookup.Keys).Distinct().ToList();

        rows = allKeys
            .Where(k => (onHandLookup.GetValueOrDefault(k, 0) != 0 || resLookup.GetValueOrDefault(k, 0) != 0))
            .Select(k => (k.MaterialId, k.LocationId, onHandLookup.GetValueOrDefault(k, 0), resLookup.GetValueOrDefault(k, 0)))
            .ToList();

        return rows.Select(r => new LedgerDerivedBalanceDto
        {
            MaterialId = r.MaterialId,
            LocationId = r.LocationId,
            OnHand = r.OnHand,
            Reserved = r.Reserved
        }).ToList();
    }

    private IQueryable<Material> MaterialsQuery => _isTesting ? _context.Materials.IgnoreQueryFilters() : _context.Materials;
    private IQueryable<StockLocation> StockLocationsQuery => _isTesting ? _context.StockLocations.IgnoreQueryFilters() : _context.StockLocations;
    private IQueryable<Order> OrdersQuery => _isTesting ? _context.Orders.IgnoreQueryFilters() : _context.Orders;

    private async Task<Material?> GetMaterialAsync(Guid id, CancellationToken ct) =>
        await MaterialsQuery.FirstOrDefaultAsync(m => m.Id == id, ct);

    private async Task<StockLocation?> GetLocationAsync(Guid id, CancellationToken ct) =>
        await StockLocationsQuery.FirstOrDefaultAsync(l => l.Id == id, ct);

    private async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct) =>
        await OrdersQuery.FirstOrDefaultAsync(o => o.Id == id, ct);

    private async Task ValidateMaterialAndLocationAsync(Guid materialId, Guid locationId, Guid companyId, Guid? departmentId, CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        var material = await GetMaterialAsync(materialId, ct);
        if (material == null) throw new InvalidOperationException("Material not found.");
        if (departmentId.HasValue && material.DepartmentId != departmentId.Value)
            throw new UnauthorizedAccessException("Material does not belong to the selected department.");
        if (material.CompanyId != companyId)
            throw new InvalidOperationException("Material does not belong to company.");
        await ValidateLocationAsync(locationId, companyId, ct);
    }

    private async Task ValidateLocationAsync(Guid locationId, Guid companyId, CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        var loc = await GetLocationAsync(locationId, ct);
        if (loc == null) throw new InvalidOperationException("Location not found.");
        if (loc.CompanyId != companyId)
            throw new InvalidOperationException("Location does not belong to company.");
    }

    private async Task ValidateOrderDepartmentAsync(Guid orderId, Guid? departmentId, CancellationToken ct)
    {
        var order = await GetOrderAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (departmentId.HasValue && order.DepartmentId != departmentId.Value)
            throw new UnauthorizedAccessException("Order does not belong to the selected department.");
    }

    /// <summary>Apply delta to balance cache in same transaction as ledger write. Caller must call SaveChanges.</summary>
    private async Task ApplyBalanceCacheDeltaAsync(Guid companyId, Guid materialId, Guid locationId, Guid? departmentId, decimal onHandDelta, decimal reservedDelta, Guid? lastLedgerEntryId, CancellationToken ct)
    {
        var deptId = departmentId;
        if (!deptId.HasValue || deptId.Value == Guid.Empty)
        {
            var material = await GetMaterialAsync(materialId, ct);
            if (material == null) return;
            deptId = material.DepartmentId;
        }

        var cache = await _context.LedgerBalanceCaches
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.MaterialId == materialId && c.LocationId == locationId, ct);
        var now = DateTime.UtcNow;
        if (cache != null)
        {
            cache.OnHand += onHandDelta;
            cache.Reserved += reservedDelta;
            cache.UpdatedAt = now;
            if (lastLedgerEntryId.HasValue) cache.LastLedgerEntryId = lastLedgerEntryId;
        }
        else
        {
            _context.LedgerBalanceCaches.Add(new LedgerBalanceCache
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                MaterialId = materialId,
                LocationId = locationId,
                DepartmentId = deptId ?? Guid.Empty,
                OnHand = onHandDelta,
                Reserved = reservedDelta,
                UpdatedAt = now,
                LastLedgerEntryId = lastLedgerEntryId
            });
        }
    }

    /// <summary>On-hand from ledger minus reserved. Uses cache when available; falls back to ledger. Multi-tenant SaaS — companyId required.</summary>
    private async Task<decimal> GetAvailableQuantityAsync(Guid materialId, Guid locationId, Guid companyId, CancellationToken ct)
    {
        var cache = await _context.LedgerBalanceCaches
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.MaterialId == materialId && c.LocationId == locationId, ct);
        if (cache != null)
            return cache.OnHand - cache.Reserved;
        var onHand = await _context.StockLedgerEntries
            .Where(e => !e.IsDeleted && e.MaterialId == materialId && e.LocationId == locationId && (e.CompanyId == companyId))
            .SumAsync(e => e.Quantity, ct);
        var reserved = await _context.StockAllocations
            .Where(a => !a.IsDeleted && a.MaterialId == materialId && a.LocationId == locationId && a.Status == StockAllocationStatus.Reserved && (a.CompanyId == companyId))
            .SumAsync(a => a.Quantity, ct);
        return onHand - reserved;
    }

    public async Task<List<UsageSummaryExportRowDto>> GetUsageSummaryExportRowsAsync(DateTime fromDate, DateTime toDate, string? groupBy, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var rangeDays = (toDate - fromDate).TotalDays;
        if (rangeDays > MaxUsageSummaryDateRangeDays)
            throw new ArgumentOutOfRangeException(nameof(toDate), $"Date range cannot exceed {MaxUsageSummaryDateRangeDays} days.");

        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var query = _context.StockLedgerEntries
            .Include(e => e.Material)
            .Include(e => e.Location)
            .Where(e => !e.IsDeleted && e.CreatedAt >= fromDate && e.CreatedAt <= toDate && e.CompanyId == effectiveCompanyId)
            .AsQueryable();
        if (departmentId.HasValue)
            query = query.Where(e => e.Material != null && e.Material.DepartmentId == departmentId.Value);
        if (materialId.HasValue)
            query = query.Where(e => e.MaterialId == materialId.Value);
        if (locationId.HasValue)
            query = query.Where(e => e.LocationId == locationId.Value || e.FromLocationId == locationId.Value || e.ToLocationId == locationId.Value);

        var entries = await query.ToListAsync(cancellationToken);

        var groupKey = (string.IsNullOrWhiteSpace(groupBy) ? null : groupBy.Trim())?.ToLowerInvariant();
        if (groupKey is "material" or "location" or "department")
        {
            var grouped = entries
                .GroupBy(e => groupKey switch
                {
                    "material" => (Id: e.MaterialId, Name: e.Material?.ItemCode ?? e.MaterialId.ToString()),
                    "location" => (Id: e.LocationId, Name: e.Location?.Name ?? e.LocationId.ToString()),
                    "department" => (Id: e.Material?.DepartmentId ?? Guid.Empty, Name: (string?)null),
                    _ => (Id: Guid.Empty, Name: (string?)null)
                })
                .Where(g => groupKey != "department" || g.Key.Id != Guid.Empty)
                .ToList();

            if (groupKey == "department")
            {
                var deptIds = grouped.Select(g => g.Key.Id).Distinct().ToList();
                var depts = await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name ?? d.Id.ToString(), cancellationToken);
                return grouped.Select(g =>
                {
                    var rec = g.Where(x => x.EntryType == StockLedgerEntryType.Receive).Sum(x => x.Quantity);
                    var trans = g.Where(x => x.EntryType == StockLedgerEntryType.Transfer && x.Quantity > 0).Sum(x => x.Quantity);
                    var iss = g.Where(x => x.EntryType == StockLedgerEntryType.Issue).Sum(x => -x.Quantity);
                    var ret = g.Where(x => x.EntryType == StockLedgerEntryType.Return).Sum(x => x.Quantity);
                    var adj = g.Where(x => x.EntryType == StockLedgerEntryType.Adjust).Sum(x => x.Quantity);
                    var scrap = g.Where(x => x.EntryType == StockLedgerEntryType.Scrap).Sum(x => -x.Quantity);
                    return new UsageSummaryExportRowDto
                    {
                        KeyId = g.Key.Id.ToString(),
                        KeyName = depts.GetValueOrDefault(g.Key.Id),
                        Received = rec,
                        Transferred = trans,
                        Issued = iss,
                        Returned = ret,
                        Adjusted = adj,
                        Scrapped = scrap
                    };
                }).ToList();
            }

            return grouped.Select(g =>
            {
                var rec = g.Where(x => x.EntryType == StockLedgerEntryType.Receive).Sum(x => x.Quantity);
                var trans = g.Where(x => x.EntryType == StockLedgerEntryType.Transfer && x.Quantity > 0).Sum(x => x.Quantity);
                var iss = g.Where(x => x.EntryType == StockLedgerEntryType.Issue).Sum(x => -x.Quantity);
                var ret = g.Where(x => x.EntryType == StockLedgerEntryType.Return).Sum(x => x.Quantity);
                var adj = g.Where(x => x.EntryType == StockLedgerEntryType.Adjust).Sum(x => x.Quantity);
                var scrap = g.Where(x => x.EntryType == StockLedgerEntryType.Scrap).Sum(x => -x.Quantity);
                return new UsageSummaryExportRowDto
                {
                    KeyId = g.Key.Id.ToString(),
                    KeyName = g.Key.Name,
                    Received = rec,
                    Transferred = trans,
                    Issued = iss,
                    Returned = ret,
                    Adjusted = adj,
                    Scrapped = scrap
                };
            }).ToList();
        }

        var received = entries.Where(e => e.EntryType == StockLedgerEntryType.Receive).Sum(e => e.Quantity);
        var transferred = entries.Where(e => e.EntryType == StockLedgerEntryType.Transfer && e.Quantity > 0).Sum(e => e.Quantity);
        var issued = entries.Where(e => e.EntryType == StockLedgerEntryType.Issue).Sum(e => -e.Quantity);
        var returned = entries.Where(e => e.EntryType == StockLedgerEntryType.Return).Sum(e => e.Quantity);
        var adjusted = entries.Where(e => e.EntryType == StockLedgerEntryType.Adjust).Sum(e => e.Quantity);
        var scrapped = entries.Where(e => e.EntryType == StockLedgerEntryType.Scrap).Sum(e => -e.Quantity);
        return new List<UsageSummaryExportRowDto>
        {
            new UsageSummaryExportRowDto
            {
                KeyId = "",
                KeyName = "Total",
                Received = received,
                Transferred = transferred,
                Issued = issued,
                Returned = returned,
                Adjusted = adjusted,
                Scrapped = scrapped
            }
        };
    }

    public async Task<List<SerialLifecycleExportRowDto>> GetSerialLifecycleExportRowsAsync(IReadOnlyList<string> serialNumbers, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        if (serialNumbers == null || serialNumbers.Count == 0)
            return new List<SerialLifecycleExportRowDto>();

        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var trimmed = serialNumbers.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        if (trimmed.Count == 0) return new List<SerialLifecycleExportRowDto>();

        var serialisedItems = await _context.SerialisedItems
            .Include(s => s.Material)
            .Where(s => !s.IsDeleted && trimmed.Contains(s.SerialNumber) && (s.CompanyId == effectiveCompanyId))
            .ToListAsync(cancellationToken);
        if (departmentId.HasValue)
            serialisedItems = serialisedItems.Where(s => s.Material != null && s.Material.DepartmentId == departmentId.Value).ToList();

        var serialIds = serialisedItems.Select(s => s.Id).ToList();
        if (serialIds.Count == 0) return new List<SerialLifecycleExportRowDto>();

        var ledgerEntries = await _context.StockLedgerEntries
            .Include(e => e.Material)
            .Include(e => e.Location)
            .Where(e => !e.IsDeleted && e.SerialisedItemId != null && serialIds.Contains(e.SerialisedItemId.Value))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderIds = ledgerEntries.Where(e => e.OrderId.HasValue).Select(e => e.OrderId!.Value).Distinct().ToList();
        var orders = orderIds.Count > 0
            ? await _context.Orders.Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.ServiceId ?? o.Id.ToString(), cancellationToken)
            : new Dictionary<Guid, string>();

        var serialByItemId = serialisedItems.ToDictionary(s => s.Id, s => s.SerialNumber);
        var materialCodeByMaterialId = serialisedItems.Where(s => s.Material != null).ToDictionary(s => s.MaterialId, s => s.Material!.ItemCode ?? s.MaterialId.ToString());

        var rows = new List<SerialLifecycleExportRowDto>();
        foreach (var e in ledgerEntries)
        {
            var serialNumber = e.SerialisedItemId.HasValue && serialByItemId.TryGetValue(e.SerialisedItemId.Value, out var sn) ? sn : "";
            var materialCode = materialCodeByMaterialId.GetValueOrDefault(e.MaterialId);
            var orderRef = e.OrderId.HasValue && orders.TryGetValue(e.OrderId.Value, out var svc) ? svc : null;
            rows.Add(new SerialLifecycleExportRowDto
            {
                SerialNumber = serialNumber,
                MaterialCode = materialCode,
                EntryType = e.EntryType.ToString(),
                Quantity = e.Quantity,
                LocationName = e.Location?.Name,
                OrderReference = orderRef,
                CreatedAtUtc = e.CreatedAt.ToString("O"),
                Remarks = e.Remarks
            });
        }
        return rows;
    }

    /// <inheritdoc />
    public async Task<UsageSummaryReportResultDto> GetUsageSummaryReportAsync(DateTime fromDate, DateTime toDate, string? groupBy, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var rows = await GetUsageSummaryExportRowsAsync(fromDate, toDate, groupBy, materialId, locationId, companyId, departmentId, cancellationToken);
        var totalCount = rows.Count;
        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new UsageSummaryRowDto
            {
                KeyId = r.KeyId,
                KeyName = r.KeyName,
                Received = r.Received,
                Transferred = r.Transferred,
                Issued = r.Issued,
                Returned = r.Returned,
                Adjusted = r.Adjusted,
                Scrapped = r.Scrapped
            })
            .ToList();
        var totalsRow = rows.FirstOrDefault(r => string.IsNullOrEmpty(r.KeyId) || r.KeyName == "Total");
        var totals = totalsRow != null
            ? new UsageSummaryTotalsDto
            {
                Received = totalsRow.Received,
                Transferred = totalsRow.Transferred,
                Issued = totalsRow.Issued,
                Returned = totalsRow.Returned,
                Adjusted = totalsRow.Adjusted,
                Scrapped = totalsRow.Scrapped
            }
            : (items.Count > 0
                ? new UsageSummaryTotalsDto
                {
                    Received = items.Sum(i => i.Received),
                    Transferred = items.Sum(i => i.Transferred),
                    Issued = items.Sum(i => i.Issued),
                    Returned = items.Sum(i => i.Returned),
                    Adjusted = items.Sum(i => i.Adjusted),
                    Scrapped = items.Sum(i => i.Scrapped)
                }
                : null);
        return new UsageSummaryReportResultDto
        {
            FromDate = fromDate.ToString("O"),
            ToDate = toDate.ToString("O"),
            GroupBy = groupBy,
            Totals = totals,
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<SerialLifecycleReportResultDto> GetSerialLifecycleReportAsync(IReadOnlyList<string> serialNumbers, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (serialNumbers == null || serialNumbers.Count == 0)
            return new SerialLifecycleReportResultDto { SerialsQueried = new List<string>(), SerialLifecycles = new List<SerialLifecycleDto>(), TotalCount = 0, Page = page, PageSize = pageSize };

        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var trimmed = serialNumbers.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        if (trimmed.Count == 0)
            return new SerialLifecycleReportResultDto { SerialsQueried = new List<string>(), SerialLifecycles = new List<SerialLifecycleDto>(), TotalCount = 0, Page = page, PageSize = pageSize };

        var serialisedItems = await _context.SerialisedItems
            .Include(s => s.Material)
            .Where(s => !s.IsDeleted && trimmed.Contains(s.SerialNumber) && (s.CompanyId == effectiveCompanyId))
            .ToListAsync(cancellationToken);
        if (departmentId.HasValue)
            serialisedItems = serialisedItems.Where(s => s.Material != null && s.Material.DepartmentId == departmentId.Value).ToList();

        var serialIds = serialisedItems.Select(s => s.Id).ToList();
        if (serialIds.Count == 0)
            return new SerialLifecycleReportResultDto { SerialsQueried = trimmed, SerialLifecycles = new List<SerialLifecycleDto>(), TotalCount = 0, Page = page, PageSize = pageSize };

        var ledgerEntries = await _context.StockLedgerEntries
            .Include(e => e.Material)
            .Include(e => e.Location)
            .Where(e => !e.IsDeleted && e.SerialisedItemId != null && serialIds.Contains(e.SerialisedItemId.Value))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderIds = ledgerEntries.Where(e => e.OrderId.HasValue).Select(e => e.OrderId!.Value).Distinct().ToList();
        var orders = orderIds.Count > 0
            ? await _context.Orders.Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.ServiceId ?? o.Id.ToString(), cancellationToken)
            : new Dictionary<Guid, string>();

        var serialByItemId = serialisedItems.ToDictionary(s => s.Id, s => s.SerialNumber);
        var materialByItemId = serialisedItems.ToDictionary(s => s.Id, s => (s.MaterialId, s.Material?.ItemCode));

        var bySerial = ledgerEntries
            .Where(e => e.SerialisedItemId.HasValue && serialByItemId.ContainsKey(e.SerialisedItemId.Value))
            .GroupBy(e => e.SerialisedItemId!.Value)
            .ToList();

        var lifecycles = new List<SerialLifecycleDto>();
        foreach (var g in bySerial)
        {
            var serialNumber = serialByItemId.GetValueOrDefault(g.Key, "");
            var (matId, materialCode) = materialByItemId.GetValueOrDefault(g.Key, (Guid.Empty, (string?)null));
            var events = g.OrderBy(e => e.CreatedAt).Select(e =>
            {
                var orderRef = e.OrderId.HasValue && orders.TryGetValue(e.OrderId.Value, out var svc) ? svc : null;
                return new SerialLifecycleEventDto
                {
                    LedgerEntryId = e.Id.ToString(),
                    EntryType = e.EntryType.ToString(),
                    Quantity = e.Quantity,
                    LocationId = e.LocationId.ToString(),
                    LocationName = e.Location?.Name,
                    FromLocationId = e.FromLocationId?.ToString(),
                    ToLocationId = e.ToLocationId?.ToString(),
                    OrderId = e.OrderId?.ToString(),
                    OrderReference = orderRef,
                    CreatedAt = e.CreatedAt.ToString("O"),
                    Remarks = e.Remarks,
                    ReferenceType = e.ReferenceType
                };
            }).ToList();
            lifecycles.Add(new SerialLifecycleDto
            {
                SerialNumber = serialNumber,
                MaterialId = matId.ToString(),
                MaterialCode = materialCode,
                SerialisedItemId = g.Key.ToString(),
                Events = events
            });
        }

        var totalCount = lifecycles.Count;
        var paged = lifecycles.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new SerialLifecycleReportResultDto
        {
            SerialsQueried = trimmed,
            SerialLifecycles = paged,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<StockByLocationHistoryResultDto> GetStockByLocationHistoryReportAsync(DateTime fromDate, DateTime toDate, string snapshotType, Guid? materialId, Guid? locationId, Guid? companyId, Guid? departmentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var type = string.IsNullOrWhiteSpace(snapshotType) ? "Daily" : snapshotType.Trim();
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;
        var from = fromDate.Date;
        var to = toDate.Date;

        if ((type.Equals("Daily", StringComparison.OrdinalIgnoreCase) || type.Equals("Weekly", StringComparison.OrdinalIgnoreCase) || type.Equals("Monthly", StringComparison.OrdinalIgnoreCase)))
        {
            var snapshotQuery = _context.StockByLocationSnapshots
                .Where(s => s.CompanyId == effectiveCompanyId && s.SnapshotType == type && s.PeriodStart >= from && s.PeriodEnd <= to);
            if (departmentId.HasValue)
                snapshotQuery = snapshotQuery.Where(s => s.DepartmentId == departmentId.Value);
            if (materialId.HasValue)
                snapshotQuery = snapshotQuery.Where(s => s.MaterialId == materialId.Value);
            if (locationId.HasValue)
                snapshotQuery = snapshotQuery.Where(s => s.LocationId == locationId.Value);

            var snapshots = await snapshotQuery
                .OrderBy(s => s.PeriodStart).ThenBy(s => s.MaterialId).ThenBy(s => s.LocationId)
                .ToListAsync(cancellationToken);
            if (snapshots.Count > 0)
            {
                var matIds = snapshots.Select(s => s.MaterialId).Distinct().ToList();
                var locIds = snapshots.Select(s => s.LocationId).Distinct().ToList();
                var materials = await _context.Materials.Where(m => matIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.ItemCode ?? m.Id.ToString(), cancellationToken);
                var locations = await _context.StockLocations.Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name ?? l.Id.ToString(), cancellationToken);
                var allItems = snapshots.Select(s => new StockByLocationHistoryRowDto
                {
                    PeriodStart = s.PeriodStart.ToString("O"),
                    PeriodEnd = s.PeriodEnd.ToString("O"),
                    MaterialId = s.MaterialId.ToString(),
                    MaterialCode = materials.GetValueOrDefault(s.MaterialId),
                    LocationId = s.LocationId.ToString(),
                    LocationName = locations.GetValueOrDefault(s.LocationId),
                    QuantityOnHand = s.QuantityOnHand,
                    QuantityReserved = s.QuantityReserved
                }).ToList();
                var totalCount = allItems.Count;
                var pagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                return new StockByLocationHistoryResultDto
                {
                    FromDate = from.ToString("O"),
                    ToDate = to.ToString("O"),
                    SnapshotType = type,
                    Items = pagedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
        }

        var summary = await GetStockSummaryAsync(companyId, departmentId, locationId, materialId, cancellationToken);
        var periodStart = fromDate.ToString("O");
        var periodEnd = toDate.ToString("O");
        var items = summary.ByLocation
            .Select(b => new StockByLocationHistoryRowDto
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                MaterialId = b.MaterialId.ToString(),
                MaterialCode = b.MaterialCode,
                LocationId = b.LocationId.ToString(),
                LocationName = b.LocationName,
                QuantityOnHand = b.QuantityOnHand,
                QuantityReserved = b.QuantityReserved
            })
            .ToList();
        var totalCountFallback = items.Count;
        var pagedItemsFallback = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new StockByLocationHistoryResultDto
        {
            FromDate = periodStart,
            ToDate = periodEnd,
            SnapshotType = string.IsNullOrWhiteSpace(snapshotType) ? "Daily" : snapshotType,
            Items = pagedItemsFallback,
            TotalCount = totalCountFallback,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task EnsureSnapshotsForPeriodAsync(DateTime periodEndDate, string snapshotType, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var type = string.IsNullOrWhiteSpace(snapshotType) ? "Daily" : snapshotType.Trim();
        var date = periodEndDate.Date;
        DateTime periodStart;
        DateTime periodEnd;
        if (type.Equals("Daily", StringComparison.OrdinalIgnoreCase))
        {
            periodStart = date;
            periodEnd = date;
        }
        else if (type.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
        {
            var daysToMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            periodStart = date.AddDays(-daysToMonday);
            periodEnd = periodStart.AddDays(6);
        }
        else if (type.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
        {
            periodStart = new DateTime(date.Year, date.Month, 1);
            periodEnd = periodStart.AddMonths(1).AddDays(-1);
        }
        else
        {
            _logger.LogWarning("EnsureSnapshotsForPeriodAsync: unknown SnapshotType {Type}, using Daily.", type);
            periodStart = date;
            periodEnd = date;
        }

        var periodEndExclusive = periodEnd.Date.AddDays(1);
        if (!companyId.HasValue || companyId.Value == Guid.Empty) throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var effectiveCompanyId = companyId.Value;

        var ledgerKeys = await _context.StockLedgerEntries
            .Where(e => !e.IsDeleted && e.CompanyId == effectiveCompanyId && e.CreatedAt < periodEndExclusive)
            .Select(e => new { e.CompanyId, e.MaterialId, e.LocationId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var allocationKeys = await _context.StockAllocations
            .Where(a => !a.IsDeleted && a.Status == StockAllocationStatus.Reserved && a.CompanyId == effectiveCompanyId && a.CreatedAt < periodEndExclusive)
            .Select(a => new { a.CompanyId, a.MaterialId, a.LocationId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var allKeys = ledgerKeys.Union(allocationKeys).Distinct().ToList();
        if (allKeys.Count == 0)
            return;

        var matIds = allKeys.Select(k => k.MaterialId).Distinct().ToList();
        var materials = await _context.Materials.Where(m => matIds.Contains(m.Id)).Select(m => new { m.Id, m.DepartmentId }).ToDictionaryAsync(m => m.Id, m => m.DepartmentId, cancellationToken);

        foreach (var key in allKeys)
        {
            var onHand = await _context.StockLedgerEntries
                .Where(e => !e.IsDeleted && e.CompanyId == key.CompanyId && e.MaterialId == key.MaterialId && e.LocationId == key.LocationId && e.CreatedAt < periodEndExclusive)
                .SumAsync(e => e.Quantity, cancellationToken);
            var reserved = await _context.StockAllocations
                .Where(a => !a.IsDeleted && a.Status == StockAllocationStatus.Reserved && a.CompanyId == key.CompanyId && a.MaterialId == key.MaterialId && a.LocationId == key.LocationId && a.CreatedAt < periodEndExclusive)
                .SumAsync(a => a.Quantity, cancellationToken);

            var deptId = materials.GetValueOrDefault(key.MaterialId, null);
            var existing = await _context.StockByLocationSnapshots
                .FirstOrDefaultAsync(s => s.CompanyId == key.CompanyId && s.MaterialId == key.MaterialId && s.LocationId == key.LocationId && s.PeriodStart == periodStart && s.SnapshotType == type, cancellationToken);
            if (existing != null)
            {
                existing.QuantityOnHand = onHand;
                existing.QuantityReserved = reserved;
                existing.PeriodEnd = periodEnd;
                existing.DepartmentId = deptId;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.StockByLocationSnapshots.Add(new StockByLocationSnapshot
                {
                    Id = Guid.NewGuid(),
                    CompanyId = key.CompanyId ?? Guid.Empty,
                    DepartmentId = deptId,
                    MaterialId = key.MaterialId,
                    LocationId = key.LocationId,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    SnapshotType = type,
                    QuantityOnHand = onHand,
                    QuantityReserved = reserved,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stock-by-location snapshots populated for period {PeriodStart}–{PeriodEnd}, type {Type}, {Count} row(s).", periodStart, periodEnd, type, allKeys.Count);
    }

    /// <inheritdoc />
    public async Task ReconcileBalanceCacheAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _context.StockLedgerEntries
            .Where(e => !e.IsDeleted)
            .Select(e => new { e.CompanyId, e.MaterialId, e.LocationId })
            .Distinct()
            .ToListAsync(cancellationToken);
        const int batchSize = 300;
        var repaired = 0;
        for (var i = 0; i < keys.Count; i += batchSize)
        {
            var batch = keys.Skip(i).Take(batchSize).ToList();
            var matIds = batch.Select(k => k.MaterialId).Distinct().ToList();
            var materials = await _context.Materials.Where(m => matIds.Contains(m.Id)).Select(m => new { m.Id, m.DepartmentId }).ToDictionaryAsync(m => m.Id, m => m.DepartmentId, cancellationToken);
            foreach (var key in batch)
            {
                var onHand = await _context.StockLedgerEntries
                    .Where(e => !e.IsDeleted && e.CompanyId == key.CompanyId && e.MaterialId == key.MaterialId && e.LocationId == key.LocationId)
                    .SumAsync(e => e.Quantity, cancellationToken);
                var reserved = await _context.StockAllocations
                    .Where(a => !a.IsDeleted && a.Status == StockAllocationStatus.Reserved && a.CompanyId == key.CompanyId && a.MaterialId == key.MaterialId && a.LocationId == key.LocationId)
                    .SumAsync(a => a.Quantity, cancellationToken);
                var departmentId = materials.GetValueOrDefault(key.MaterialId, null) ?? Guid.Empty;
                var cache = await _context.LedgerBalanceCaches
                    .FirstOrDefaultAsync(c => c.CompanyId == key.CompanyId && c.MaterialId == key.MaterialId && c.LocationId == key.LocationId, cancellationToken);
                var now = DateTime.UtcNow;
                if (cache != null)
                {
                    if (cache.OnHand != onHand || cache.Reserved != reserved)
                    {
                        cache.OnHand = onHand;
                        cache.Reserved = reserved;
                        cache.DepartmentId = departmentId;
                        cache.UpdatedAt = now;
                        repaired++;
                    }
                }
                else if (onHand != 0 || reserved != 0)
                {
                    _context.LedgerBalanceCaches.Add(new LedgerBalanceCache
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = key.CompanyId,
                        MaterialId = key.MaterialId,
                        LocationId = key.LocationId,
                        DepartmentId = departmentId,
                        OnHand = onHand,
                        Reserved = reserved,
                        UpdatedAt = now
                    });
                    repaired++;
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
        if (repaired > 0)
            _logger.LogInformation("Ledger balance cache reconciliation: repaired {Count} row(s).", repaired);
    }
}
