using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Splitter service implementation
/// </summary>
public class SplitterService : ISplitterService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SplitterService> _logger;

    public SplitterService(
        ApplicationDbContext context,
        ILogger<SplitterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SplitterDto>> GetSplittersAsync(Guid? companyId, Guid? buildingId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Splitters.AsQueryable();
        
        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(s => s.BuildingId == buildingId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var splitters = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var splitterIds = splitters.Select(s => s.Id).ToList();
        var ports = await _context.SplitterPorts
            .Where(p => splitterIds.Contains(p.SplitterId))
            .OrderBy(p => p.SplitterId)
            .ThenBy(p => p.PortNumber)
            .ToListAsync(cancellationToken);

        var buildingIds = splitters.Select(s => s.BuildingId).Distinct().ToList();
        var buildings = await _context.Buildings
            .Where(b => buildingIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var portsBySplitter = ports.GroupBy(p => p.SplitterId).ToDictionary(g => g.Key, g => g.ToList());

        return splitters.Select(s => new SplitterDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            BuildingId = s.BuildingId,
            BuildingName = buildings.TryGetValue(s.BuildingId, out var buildingName) ? buildingName : string.Empty,
            Name = s.Name,
            Code = s.Code,
            SplitterTypeId = s.SplitterTypeId,
            Location = s.Location,
            Block = s.Block,
            Floor = s.Floor,
            DepartmentId = s.DepartmentId,
            IsActive = s.IsActive,
            Ports = portsBySplitter.TryGetValue(s.Id, out var splitterPorts) 
                ? splitterPorts.Select(p => new SplitterPortDto
                {
                    Id = p.Id,
                    SplitterId = p.SplitterId,
                    PortNumber = p.PortNumber,
                    Status = p.Status,
                    OrderId = p.OrderId,
                    AssignedAt = p.AssignedAt,
                    IsStandby = p.IsStandby,
                    StandbyOverrideApproved = p.StandbyOverrideApproved,
                    ApprovalAttachmentId = p.ApprovalAttachmentId
                }).ToList()
                : new List<SplitterPortDto>(),
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<SplitterDto?> GetSplitterByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Splitters.Where(s => s.Id == id);
        
        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }
        
        var splitter = await query.FirstOrDefaultAsync(cancellationToken);

        if (splitter == null)
        {
            return null;
        }

        var ports = await _context.SplitterPorts
            .Where(p => p.SplitterId == splitter.Id)
            .OrderBy(p => p.PortNumber)
            .ToListAsync(cancellationToken);

        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == splitter.BuildingId, cancellationToken);

        return new SplitterDto
        {
            Id = splitter.Id,
            CompanyId = splitter.CompanyId,
            BuildingId = splitter.BuildingId,
            BuildingName = building?.Name ?? string.Empty,
            Name = splitter.Name,
            Code = splitter.Code,
            SplitterTypeId = splitter.SplitterTypeId,
            Location = splitter.Location,
            Block = splitter.Block,
            Floor = splitter.Floor,
            DepartmentId = splitter.DepartmentId,
            IsActive = splitter.IsActive,
            Ports = ports.Select(p => new SplitterPortDto
            {
                Id = p.Id,
                SplitterId = p.SplitterId,
                PortNumber = p.PortNumber,
                Status = p.Status,
                OrderId = p.OrderId,
                AssignedAt = p.AssignedAt,
                IsStandby = p.IsStandby,
                StandbyOverrideApproved = p.StandbyOverrideApproved,
                ApprovalAttachmentId = p.ApprovalAttachmentId
            }).ToList(),
            CreatedAt = splitter.CreatedAt
        };
    }

    public async Task<SplitterDto> CreateSplitterAsync(CreateSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == dto.BuildingId && b.CompanyId == companyId, cancellationToken);

        if (building == null)
        {
            throw new KeyNotFoundException($"Building with ID {dto.BuildingId} not found");
        }

        // Get splitter type to determine port count and standby port
        int totalPorts = 8; // Default
        int? standbyPortNumber = null;
        
        if (dto.SplitterTypeId.HasValue)
        {
            var splitterType = await _context.SplitterTypes
                .FirstOrDefaultAsync(st => st.Id == dto.SplitterTypeId.Value, cancellationToken);
            
            if (splitterType != null)
            {
                totalPorts = splitterType.TotalPorts;
                standbyPortNumber = splitterType.StandbyPortNumber; // e.g., 32 for 1:32 splitters
                
                _logger.LogInformation(
                    "Creating splitter with type {TypeName}: {TotalPorts} ports, standby port: {StandbyPort}",
                    splitterType.Name, totalPorts, standbyPortNumber);
            }
        }

        var splitter = new Splitter
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BuildingId = dto.BuildingId,
            Name = dto.Name,
            Code = dto.Code,
            SplitterTypeId = dto.SplitterTypeId,
            Location = dto.Location,
            Block = dto.Block,
            Floor = dto.Floor,
            DepartmentId = dto.DepartmentId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Splitters.Add(splitter);

        // Create ports based on splitter type
        // Per SPLITTERS_MANAGEMENT_MODULE.md: For 1:32 splitters, port 32 is reserved as standby
        var ports = new List<SplitterPort>();
        
        for (int i = 1; i <= totalPorts; i++)
        {
            // Mark port as standby if it matches the standby port number for this splitter type
            bool isStandby = standbyPortNumber.HasValue && i == standbyPortNumber.Value;
            
            ports.Add(new SplitterPort
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                SplitterId = splitter.Id,
                PortNumber = i,
                Status = isStandby ? "Standby" : "Available",
                IsStandby = isStandby,
                StandbyOverrideApproved = false,
                ApprovalAttachmentId = null,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.SplitterPorts.AddRange(ports);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Splitter created: {SplitterId}, Name: {Name}, Ports: {PortCount}, Standby: {StandbyPort}", 
            splitter.Id, splitter.Name, totalPorts, standbyPortNumber);

        return new SplitterDto
        {
            Id = splitter.Id,
            CompanyId = splitter.CompanyId,
            BuildingId = splitter.BuildingId,
            BuildingName = building.Name,
            Name = splitter.Name,
            Code = splitter.Code,
            SplitterTypeId = splitter.SplitterTypeId,
            Location = splitter.Location,
            Block = splitter.Block,
            Floor = splitter.Floor,
            DepartmentId = splitter.DepartmentId,
            IsActive = splitter.IsActive,
            Ports = ports.Select(p => new SplitterPortDto
            {
                Id = p.Id,
                SplitterId = p.SplitterId,
                PortNumber = p.PortNumber,
                Status = p.Status,
                OrderId = p.OrderId,
                AssignedAt = p.AssignedAt,
                IsStandby = p.IsStandby,
                StandbyOverrideApproved = p.StandbyOverrideApproved,
                ApprovalAttachmentId = p.ApprovalAttachmentId
            }).ToList(),
            CreatedAt = splitter.CreatedAt
        };
    }

    public async Task<SplitterDto> UpdateSplitterAsync(Guid id, UpdateSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Splitters.Where(s => s.Id == id);
        
        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }
        
        var splitter = await query.FirstOrDefaultAsync(cancellationToken);

        if (splitter == null)
        {
            throw new KeyNotFoundException($"Splitter with ID {id} not found");
        }

        if (dto.Name != null) splitter.Name = dto.Name;
        if (dto.Code != null) splitter.Code = dto.Code;
        if (dto.SplitterTypeId.HasValue) splitter.SplitterTypeId = dto.SplitterTypeId;
        if (dto.Location != null) splitter.Location = dto.Location;
        if (dto.Block != null) splitter.Block = dto.Block;
        if (dto.Floor != null) splitter.Floor = dto.Floor;
        if (dto.DepartmentId.HasValue) splitter.DepartmentId = dto.DepartmentId;
        if (dto.IsActive.HasValue) splitter.IsActive = dto.IsActive.Value;

        splitter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Splitter updated: {SplitterId}", id);

        return await GetSplitterByIdAsync(id, companyId, cancellationToken) 
            ?? throw new InvalidOperationException($"Failed to retrieve updated splitter {id}");
    }

    public async Task DeleteSplitterAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Splitters.Where(s => s.Id == id);
        
        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }
        
        var splitter = await query.FirstOrDefaultAsync(cancellationToken);

        if (splitter == null)
        {
            throw new KeyNotFoundException($"Splitter with ID {id} not found");
        }

        var ports = await _context.SplitterPorts
            .Where(p => p.SplitterId == id)
            .ToListAsync(cancellationToken);

        // Check if any ports are in use
        if (ports.Any(p => p.Status == "Used"))
        {
            throw new InvalidOperationException($"Cannot delete splitter {id} because it has ports in use");
        }

        _context.SplitterPorts.RemoveRange(ports);
        _context.Splitters.Remove(splitter);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Splitter deleted: {SplitterId}", id);
    }

    public async Task<SplitterPortDto> UpdateSplitterPortAsync(Guid portId, UpdateSplitterPortDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SplitterPorts.Where(p => p.Id == portId);
        
        if (companyId.HasValue)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }
        
        var port = await query.FirstOrDefaultAsync(cancellationToken);

        if (port == null)
        {
            throw new KeyNotFoundException($"Splitter port with ID {portId} not found");
        }

        // Validate standby port usage per SPLITTERS_MANAGEMENT_MODULE.md section 4
        // If port is marked IsStandby and status is being set to "Used":
        //   - Must have ApprovalAttachmentId
        //   - StandbyOverrideApproved = true must be set
        if (port.IsStandby && dto.Status == "Used")
        {
            if (!dto.OrderId.HasValue)
            {
                throw new InvalidOperationException("Standby port (port 32 on 1:32 splitter) usage requires an order ID.");
            }
            
            if (!dto.ApprovalAttachmentId.HasValue)
            {
                throw new InvalidOperationException(
                    "Standby port (port 32 on 1:32 splitter) requires partner approval. " +
                    "Please upload an approval document and provide the ApprovalAttachmentId.");
            }
            
            // Mark as standby override approved
            port.StandbyOverrideApproved = true;
            port.ApprovalAttachmentId = dto.ApprovalAttachmentId;
            
            _logger.LogInformation(
                "Standby port override approved: Port {PortNumber} on Splitter {SplitterId}, " +
                "Order: {OrderId}, Approval: {ApprovalId}",
                port.PortNumber, port.SplitterId, dto.OrderId, dto.ApprovalAttachmentId);
        }
        else if (dto.Status != "Used")
        {
            // Reset standby override fields when releasing the port
            port.StandbyOverrideApproved = false;
            port.ApprovalAttachmentId = null;
        }

        port.Status = dto.Status;
        port.OrderId = dto.OrderId;
        port.AssignedAt = dto.Status == "Used" ? DateTime.UtcNow : null;
        port.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Splitter port updated: {PortId}, Status: {Status}", portId, dto.Status);

        return new SplitterPortDto
        {
            Id = port.Id,
            SplitterId = port.SplitterId,
            PortNumber = port.PortNumber,
            Status = port.Status,
            OrderId = port.OrderId,
            AssignedAt = port.AssignedAt,
            IsStandby = port.IsStandby,
            StandbyOverrideApproved = port.StandbyOverrideApproved,
            ApprovalAttachmentId = port.ApprovalAttachmentId
        };
    }

    private static int GetPortCountFromType(string splitterType)
    {
        return splitterType switch
        {
            "1:8" => 8,
            "1:12" => 12,
            "1:32" => 32,
            _ => throw new ArgumentException($"Unknown splitter type: {splitterType}")
        };
    }
}

