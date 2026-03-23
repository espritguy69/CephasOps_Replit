using CephasOps.Application.RMA.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Companies.Services;
using CephasOps.Domain.RMA.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.RMA.Services;

/// <summary>
/// RMA service implementation
/// </summary>
public class RMAService : IRMAService
{
    private readonly ApplicationDbContext _context;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IPartnerService _partnerService;
    private readonly ILogger<RMAService> _logger;

    public RMAService(
        ApplicationDbContext context,
        IApprovalWorkflowService approvalWorkflowService,
        IPartnerService partnerService,
        ILogger<RMAService> logger)
    {
        _context = context;
        _approvalWorkflowService = approvalWorkflowService;
        _partnerService = partnerService;
        _logger = logger;
    }

    public async Task<List<RmaRequestDto>> GetRmaRequestsAsync(Guid? companyId, Guid? partnerId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting RMA requests for company {CompanyId}", companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = companyId.HasValue 
            ? _context.RmaRequests.Include(r => r.Items).Where(r => r.CompanyId == companyId.Value)
            : _context.RmaRequests.Include(r => r.Items).AsQueryable();

        if (partnerId.HasValue)
        {
            query = query.Where(r => r.PartnerId == partnerId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.RequestDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.RequestDate <= toDate.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(cancellationToken);

        var result = new List<RmaRequestDto>();
        foreach (var request in requests)
        {
            result.Add(await MapToRmaRequestDtoAsync(request, cancellationToken));
        }
        return result;
    }

    public async Task<List<RmaRequestDto>> GetRmaRequestsByOrderAsync(Guid orderId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting RMA requests for order {OrderId}", orderId);

        // Get RMA requests that have items linked to this order
        var query = _context.RmaRequests
            .Include(r => r.Items)
            .Where(r => r.Items.Any(item => item.OriginalOrderId == orderId));

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(cancellationToken);

        var result = new List<RmaRequestDto>();
        foreach (var request in requests)
        {
            result.Add(await MapToRmaRequestDtoAsync(request, cancellationToken));
        }
        return result;
    }

    public async Task<RmaRequestDto?> GetRmaRequestByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting RMA request {RmaId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.RmaRequests.Include(r => r.Items).Where(r => r.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        var request = await query.FirstOrDefaultAsync(cancellationToken);

        return request != null ? await MapToRmaRequestDtoAsync(request, cancellationToken) : null;
    }

    public async Task<RmaRequestDto> CreateRmaRequestAsync(CreateRmaRequestDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        _logger.LogInformation("Creating RMA request (company feature disabled)");

        // Check for approval workflow
        Guid? approvalWorkflowId = null;
        string initialStatus = "Requested";
        
        if (companyId.HasValue)
        {
            try
            {
                var approvalWorkflow = await _approvalWorkflowService.GetEffectiveWorkflowAsync(
                    companyId.Value,
                    workflowType: "RMA",
                    entityType: "RmaRequest",
                    partnerId: dto.PartnerId,
                    departmentId: null,
                    orderType: null,
                    value: null,
                    cancellationToken);

                if (approvalWorkflow != null)
                {
                    approvalWorkflowId = approvalWorkflow.Id;
                    initialStatus = "Pending"; // Require approval
                    _logger.LogInformation(
                        "Approval workflow {WorkflowId} found for RMA request. Status set to Pending.",
                        approvalWorkflow.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking approval workflow for RMA request");
            }
        }

        var rmaRequest = new RmaRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? Guid.Empty, // Company feature removed
            PartnerId = dto.PartnerId,
            RequestDate = DateTime.UtcNow,
            Reason = dto.Reason,
            Status = initialStatus,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            var item = new RmaRequestItem
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId ?? Guid.Empty, // Company feature removed
                RmaRequestId = rmaRequest.Id,
                SerialisedItemId = itemDto.SerialisedItemId,
                OriginalOrderId = itemDto.OriginalOrderId,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            rmaRequest.Items.Add(item);
        }

        _context.RmaRequests.Add(rmaRequest);
        await _context.SaveChangesAsync(cancellationToken);

        // If approval workflow exists, the RMA request will remain in "Pending" status
        // until approval steps are completed (to be implemented in approval workflow execution)
        if (approvalWorkflowId.HasValue)
        {
            _logger.LogInformation(
                "RMA request {RmaRequestId} requires approval workflow {WorkflowId}. Status set to Pending.",
                rmaRequest.Id, approvalWorkflowId.Value);
        }

        return await MapToRmaRequestDtoAsync(rmaRequest, cancellationToken);
    }

    public async Task<RmaRequestDto> UpdateRmaRequestAsync(Guid id, UpdateRmaRequestDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating RMA request {RmaId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.RmaRequests.Include(r => r.Items).Where(r => r.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        var request = await query.FirstOrDefaultAsync(cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"RMA request with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            request.Status = dto.Status;
        }

        if (dto.RmaNumber != null)
        {
            request.RmaNumber = dto.RmaNumber;
        }

        if (dto.MraDocumentId.HasValue)
        {
            request.MraDocumentId = dto.MraDocumentId;
        }

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await MapToRmaRequestDtoAsync(request, cancellationToken);
    }

    public async Task DeleteRmaRequestAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting RMA request {RmaId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.RmaRequests.Where(r => r.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        var request = await query.FirstOrDefaultAsync(cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"RMA request with ID {id} not found");
        }

        _context.RmaRequests.Remove(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<RmaRequestDto> MapToRmaRequestDtoAsync(RmaRequest request, CancellationToken cancellationToken)
    {
        // Load partner name
        string partnerName = string.Empty;
        if (request.PartnerId != Guid.Empty && request.CompanyId.HasValue)
        {
            var partner = await _partnerService.GetPartnerByIdAsync(request.PartnerId, request.CompanyId, cancellationToken);
            partnerName = partner?.Name ?? string.Empty;
        }

        // Load serial numbers for items
        var itemDtos = new List<RmaRequestItemDto>();
        foreach (var item in request.Items)
        {
            string serialNumber = string.Empty;
            if (item.SerialisedItemId != Guid.Empty)
            {
                var serialisedItem = await _context.Set<CephasOps.Domain.Inventory.Entities.SerialisedItem>()
                    .FirstOrDefaultAsync(si => si.Id == item.SerialisedItemId, cancellationToken);
                serialNumber = serialisedItem?.SerialNumber ?? string.Empty;
            }

            itemDtos.Add(new RmaRequestItemDto
            {
                Id = item.Id,
                SerialisedItemId = item.SerialisedItemId,
                SerialNumber = serialNumber,
                OriginalOrderId = item.OriginalOrderId,
                Notes = item.Notes,
                Result = item.Result
            });
        }

        return new RmaRequestDto
        {
            Id = request.Id,
            CompanyId = request.CompanyId,
            PartnerId = request.PartnerId,
            PartnerName = partnerName,
            RmaNumber = request.RmaNumber,
            RequestDate = request.RequestDate,
            Reason = request.Reason,
            Status = request.Status,
            MraDocumentId = request.MraDocumentId,
            Items = itemDtos,
            CreatedAt = request.CreatedAt
        };
    }
}

