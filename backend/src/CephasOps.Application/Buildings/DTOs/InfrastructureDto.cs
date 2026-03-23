using CephasOps.Domain.Buildings.Entities;

namespace CephasOps.Application.Buildings.DTOs;

#region Building Block DTOs

/// <summary>
/// Building block DTO
/// </summary>
public class BuildingBlockDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Floors { get; set; }
    public int? UnitsPerFloor { get; set; }
    public int? TotalUnits { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Related counts
    public int SplittersCount { get; set; }
}

/// <summary>
/// Create/update building block DTO
/// </summary>
public class SaveBuildingBlockDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Floors { get; set; }
    public int? UnitsPerFloor { get; set; }
    public int? TotalUnits { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

#endregion

#region Building Splitter DTOs

/// <summary>
/// Building splitter DTO
/// </summary>
public class BuildingSplitterDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public Guid? BlockId { get; set; }
    public string? BlockName { get; set; }
    public Guid SplitterTypeId { get; set; }
    public string? SplitterTypeName { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public string? LocationDescription { get; set; }
    public int PortsTotal { get; set; }
    public int PortsUsed { get; set; }
    public int PortsAvailable { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsFull { get; set; }
    public SplitterStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public string? SerialNumber { get; set; }
    public DateTime? InstalledAt { get; set; }
    public DateTime? LastMaintenanceAt { get; set; }
    public string? Remarks { get; set; }
    public bool NeedsAttention { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Create/update building splitter DTO
/// </summary>
public class SaveBuildingSplitterDto
{
    public Guid? BlockId { get; set; }
    public Guid SplitterTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public string? LocationDescription { get; set; }
    public int PortsTotal { get; set; }
    public int PortsUsed { get; set; }
    public SplitterStatus Status { get; set; } = SplitterStatus.Active;
    public string? SerialNumber { get; set; }
    public DateTime? InstalledAt { get; set; }
    public DateTime? LastMaintenanceAt { get; set; }
    public string? Remarks { get; set; }
    public bool NeedsAttention { get; set; }
}

/// <summary>
/// Splitter list filter DTO
/// </summary>
public class SplitterFilterDto
{
    public Guid? BuildingId { get; set; }
    public Guid? BlockId { get; set; }
    public Guid? SplitterTypeId { get; set; }
    public SplitterStatus? Status { get; set; }
    public bool? NeedsAttention { get; set; }
    public bool? IsFull { get; set; }
    public bool OnlyWithAvailability { get; set; }
}

#endregion

#region Street DTOs

/// <summary>
/// Street DTO
/// </summary>
public class StreetDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Section { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Related counts
    public int HubBoxesCount { get; set; }
    public int PolesCount { get; set; }
}

/// <summary>
/// Create/update street DTO
/// </summary>
public class SaveStreetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Section { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

#endregion

#region Hub Box DTOs

/// <summary>
/// Hub box DTO
/// </summary>
public class HubBoxDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? StreetId { get; set; }
    public string? StreetName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int PortsTotal { get; set; }
    public int PortsUsed { get; set; }
    public int PortsAvailable { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsFull { get; set; }
    public HubBoxStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public DateTime? InstalledAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Create/update hub box DTO
/// </summary>
public class SaveHubBoxDto
{
    public Guid? StreetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int PortsTotal { get; set; }
    public int PortsUsed { get; set; }
    public HubBoxStatus Status { get; set; } = HubBoxStatus.Active;
    public DateTime? InstalledAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsActive { get; set; } = true;
}

#endregion

#region Pole DTOs

/// <summary>
/// Pole DTO
/// </summary>
public class PoleDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? StreetId { get; set; }
    public string? StreetName { get; set; }
    public string PoleNumber { get; set; } = string.Empty;
    public string? PoleType { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? HeightMeters { get; set; }
    public bool HasExistingFibre { get; set; }
    public int DropsCount { get; set; }
    public PoleStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastInspectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Create/update pole DTO
/// </summary>
public class SavePoleDto
{
    public Guid? StreetId { get; set; }
    public string PoleNumber { get; set; } = string.Empty;
    public string? PoleType { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? HeightMeters { get; set; }
    public bool HasExistingFibre { get; set; }
    public int DropsCount { get; set; }
    public PoleStatus Status { get; set; } = PoleStatus.Good;
    public string? Remarks { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastInspectedAt { get; set; }
}

#endregion

#region Infrastructure Overview

/// <summary>
/// Building infrastructure overview DTO
/// Aggregates all infrastructure data for a building
/// </summary>
public class BuildingInfrastructureDto
{
    public Guid BuildingId { get; set; }
    public string? PropertyType { get; set; }
    
    // MDU Infrastructure
    public List<BuildingBlockDto> Blocks { get; set; } = new();
    public List<BuildingSplitterDto> Splitters { get; set; } = new();
    
    // Landed/SDU Infrastructure
    public List<StreetDto> Streets { get; set; } = new();
    public List<HubBoxDto> HubBoxes { get; set; } = new();
    public List<PoleDto> Poles { get; set; } = new();
    
    // Summary stats
    public int TotalBlocks { get; set; }
    public int TotalSplitters { get; set; }
    public int TotalSplitterPorts { get; set; }
    public int UsedSplitterPorts { get; set; }
    public int TotalStreets { get; set; }
    public int TotalHubBoxes { get; set; }
    public int TotalHubBoxPorts { get; set; }
    public int UsedHubBoxPorts { get; set; }
    public int TotalPoles { get; set; }
}

#endregion

