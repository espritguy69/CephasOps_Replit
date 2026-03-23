using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Parser rule entity - email filtering and routing rules
/// </summary>
public class ParserRule : CompanyScopedEntity
{
    public Guid? EmailAccountId { get; set; }
    public string? FromAddressPattern { get; set; }
    public string? DomainPattern { get; set; }
    public string? SubjectContains { get; set; }
    public bool IsVip { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

