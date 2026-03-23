using CephasOps.Domain.Common;
using CephasOps.Domain.Users.Entities;

namespace CephasOps.Domain.Departments.Entities;

/// <summary>
/// Links users to departments and captures their departmental role.
/// </summary>
public class DepartmentMembership : CompanyScopedEntity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Role { get; set; } = "Member";
    public bool IsDefault { get; set; }

    public Department? Department { get; set; }
    public User? User { get; set; }
}


