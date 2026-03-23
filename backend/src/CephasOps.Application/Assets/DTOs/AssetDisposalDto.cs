using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Application.Assets.DTOs;

/// <summary>
/// Asset Disposal DTO
/// </summary>
public class AssetDisposalDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTag { get; set; }
    public DisposalMethod DisposalMethod { get; set; }
    public string DisposalMethodName => DisposalMethod.ToString();
    public DateTime DisposalDate { get; set; }
    public decimal BookValueAtDisposal { get; set; }
    public decimal DisposalProceeds { get; set; }
    public decimal GainLoss { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? BuyerName { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create Asset Disposal request DTO
/// </summary>
public class CreateAssetDisposalDto
{
    public Guid AssetId { get; set; }
    public DisposalMethod DisposalMethod { get; set; }
    public DateTime DisposalDate { get; set; }
    public decimal DisposalProceeds { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? BuyerName { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Approve Asset Disposal request DTO
/// </summary>
public class ApproveAssetDisposalDto
{
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}

