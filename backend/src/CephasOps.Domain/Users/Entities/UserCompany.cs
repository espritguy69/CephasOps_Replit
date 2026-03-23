namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// User-Company membership entity
/// </summary>
public class UserCompany
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public CephasOps.Domain.Companies.Entities.Company? Company { get; set; }
}

