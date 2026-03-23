using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Common.Utilities;
using CephasOps.Domain.ServiceInstallers.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.ServiceInstallers.Services;

/// <summary>
/// Service Installer service implementation
/// </summary>
public class ServiceInstallerService : IServiceInstallerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceInstallerService> _logger;
    private readonly ILocationAutoCreateService? _locationAutoCreateService;

    public ServiceInstallerService(
        ApplicationDbContext context,
        ILogger<ServiceInstallerService> logger,
        ILocationAutoCreateService? locationAutoCreateService = null)
    {
        _context = context;
        _logger = logger;
        _locationAutoCreateService = locationAutoCreateService;
    }

    public async Task<List<ServiceInstallerDto>> GetServiceInstallersAsync(
        Guid? companyId, 
        Guid? departmentId = null, 
        bool? isActive = null,
        InstallerType? installerType = null,
        InstallerLevel? siLevel = null,
        List<Guid>? skillIds = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<ServiceInstallerDto>();

        var query = _context.ServiceInstallers.Where(si => si.CompanyId == effectiveCompanyId.Value);

        // Filter by department if specified
        if (departmentId.HasValue)
        {
            query = query.Where(si => si.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(si => si.IsActive == isActive.Value);
        }

        // Filter by installer type
        if (installerType.HasValue)
        {
            query = query.Where(si => si.InstallerType == installerType.Value);
        }

        // Filter by installer level
        if (siLevel.HasValue)
        {
            query = query.Where(si => si.SiLevel == siLevel.Value);
        }

        // Filter by skills - installers must have ALL specified skills
        if (skillIds != null && skillIds.Any())
        {
            query = query.Where(si => si.Skills
                .Where(s => s.IsActive && !s.IsDeleted)
                .Select(s => s.SkillId)
                .Count(skillId => skillIds.Contains(skillId)) == skillIds.Count);
        }

        var serviceInstallers = await query
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken);

        // Get department names for service installers that have departments
        var departmentIds = serviceInstallers
            .Where(si => si.DepartmentId.HasValue)
            .Select(si => si.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        return serviceInstallers.Select(si => new ServiceInstallerDto
        {
            Id = si.Id,
            CompanyId = si.CompanyId,
            DepartmentId = si.DepartmentId,
            DepartmentName = si.DepartmentId.HasValue && departments.ContainsKey(si.DepartmentId.Value) 
                ? departments[si.DepartmentId.Value] 
                : null,
            Name = si.Name,
            EmployeeId = si.EmployeeId,
            Phone = si.Phone,
            Email = si.Email,
            SiLevel = si.SiLevel,
            InstallerType = si.InstallerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = si.IsSubcontractor, // Backward compatibility - computed from InstallerType
#pragma warning restore CS0618
            IsActive = si.IsActive,
            UserId = si.UserId,
            AvailabilityStatus = si.AvailabilityStatus,
            HireDate = si.HireDate,
            EmploymentStatus = si.EmploymentStatus,
            ContractorId = si.ContractorId,
            ContractorCompany = si.ContractorCompany,
            ContractStartDate = si.ContractStartDate,
            ContractEndDate = si.ContractEndDate,
            IcNumber = si.IcNumber,
            BankName = si.BankName,
            BankAccountNumber = si.BankAccountNumber,
            Address = si.Address,
            EmergencyContact = si.EmergencyContact,
            CreatedAt = si.CreatedAt
        }).ToList();
    }

    public async Task<ServiceInstallerDto?> GetServiceInstallerByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var serviceInstaller = await _context.ServiceInstallers
            .Where(si => si.Id == id && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceInstaller == null)
        {
            return null;
        }

        string? departmentName = null;
        if (serviceInstaller.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == serviceInstaller.DepartmentId.Value, cancellationToken);
            departmentName = department?.Name;
        }

        // Load skills for this installer
        var skills = await _context.ServiceInstallerSkills
            .Where(sis => sis.ServiceInstallerId == id && sis.IsActive && !sis.IsDeleted)
            .Include(sis => sis.Skill)
            .Select(sis => new ServiceInstallerSkillDto
            {
                Id = sis.Id,
                ServiceInstallerId = sis.ServiceInstallerId,
                SkillId = sis.SkillId,
                Skill = new SkillDto
                {
                    Id = sis.Skill.Id,
                    Name = sis.Skill.Name,
                    Code = sis.Skill.Code,
                    Category = sis.Skill.Category,
                    Description = sis.Skill.Description,
                    IsActive = sis.Skill.IsActive,
                    DisplayOrder = sis.Skill.DisplayOrder
                },
                AcquiredAt = sis.AcquiredAt,
                VerifiedAt = sis.VerifiedAt,
                VerifiedByUserId = sis.VerifiedByUserId,
                Notes = sis.Notes,
                IsActive = sis.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ServiceInstallerDto
        {
            Id = serviceInstaller.Id,
            CompanyId = serviceInstaller.CompanyId,
            DepartmentId = serviceInstaller.DepartmentId,
            DepartmentName = departmentName,
            Name = serviceInstaller.Name,
            EmployeeId = serviceInstaller.EmployeeId,
            Phone = serviceInstaller.Phone,
            Email = serviceInstaller.Email,
            SiLevel = serviceInstaller.SiLevel,
            InstallerType = serviceInstaller.InstallerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = serviceInstaller.IsSubcontractor, // Backward compatibility
#pragma warning restore CS0618
            IsActive = serviceInstaller.IsActive,
            UserId = serviceInstaller.UserId,
            AvailabilityStatus = serviceInstaller.AvailabilityStatus,
            HireDate = serviceInstaller.HireDate,
            EmploymentStatus = serviceInstaller.EmploymentStatus,
            ContractorId = serviceInstaller.ContractorId,
            ContractorCompany = serviceInstaller.ContractorCompany,
            ContractStartDate = serviceInstaller.ContractStartDate,
            ContractEndDate = serviceInstaller.ContractEndDate,
            IcNumber = serviceInstaller.IcNumber,
            BankName = serviceInstaller.BankName,
            BankAccountNumber = serviceInstaller.BankAccountNumber,
            Address = serviceInstaller.Address,
            EmergencyContact = serviceInstaller.EmergencyContact,
            Skills = skills,
            CreatedAt = serviceInstaller.CreatedAt
        };
    }

    public async Task<ServiceInstallerDto> CreateServiceInstallerAsync(CreateServiceInstallerDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a service installer.");

        // Normalize input data for comparison
        var normalizedName = NameNormalizer.Normalize(dto.Name);
        var normalizedPhone = PhoneNumberNormalizer.Normalize(dto.Phone);
        var normalizedEmail = EmailNormalizer.Normalize(dto.Email);
        var normalizedEmployeeId = dto.EmployeeId?.Trim().ToUpperInvariant() ?? string.Empty;

        // Prevent duplicates using enhanced normalization and fuzzy matching
        var existingSIs = await _context.ServiceInstallers
            .Where(si => si.CompanyId == effectiveCompanyId.Value && !si.IsDeleted)
            .ToListAsync(cancellationToken);

        // Check for exact matches first (EmployeeId)
        if (!string.IsNullOrEmpty(normalizedEmployeeId))
        {
            var exactMatch = existingSIs.FirstOrDefault(si =>
                si.EmployeeId != null &&
                si.EmployeeId.Trim().ToUpperInvariant() == normalizedEmployeeId);

            if (exactMatch != null)
            {
                throw new InvalidOperationException(
                    $"A service installer with employee ID '{dto.EmployeeId}' already exists: {exactMatch.Name} (ID: {exactMatch.Id}).");
            }
        }

        // Check for normalized matches (Name + Phone/Email)
        var normalizedMatch = existingSIs.FirstOrDefault(si =>
        {
            var siNormalizedName = NameNormalizer.Normalize(si.Name);
            var siNormalizedPhone = PhoneNumberNormalizer.Normalize(si.Phone);
            var siNormalizedEmail = EmailNormalizer.Normalize(si.Email);

            // Exact normalized name match
            if (siNormalizedName != normalizedName)
                return false;

            // Check phone or email match
            if (!string.IsNullOrEmpty(normalizedPhone) && PhoneNumberNormalizer.AreSame(normalizedPhone, siNormalizedPhone))
                return true;

            if (!string.IsNullOrEmpty(normalizedEmail) && EmailNormalizer.AreSame(normalizedEmail, siNormalizedEmail))
                return true;

            return false;
        });

        if (normalizedMatch != null)
        {
            throw new InvalidOperationException(
                $"A service installer with similar name and contact details already exists: {normalizedMatch.Name} (ID: {normalizedMatch.Id}). " +
                $"Please verify if this is the same person before creating a new record.");
        }

        // Check for fuzzy name matches (similar names with same phone/email)
        if (!string.IsNullOrEmpty(normalizedPhone) || !string.IsNullOrEmpty(normalizedEmail))
        {
            var fuzzyMatch = existingSIs.FirstOrDefault(si =>
            {
                var siNormalizedName = NameNormalizer.Normalize(si.Name);
                var siNormalizedPhone = PhoneNumberNormalizer.Normalize(si.Phone);
                var siNormalizedEmail = EmailNormalizer.Normalize(si.Email);

                // Check if names are similar (85% threshold)
                var nameSimilarity = NameNormalizer.CalculateSimilarity(normalizedName, siNormalizedName);
                if (nameSimilarity < 0.85)
                    return false;

                // Check if phone or email matches
                if (!string.IsNullOrEmpty(normalizedPhone) && PhoneNumberNormalizer.AreSame(normalizedPhone, siNormalizedPhone))
                    return true;

                if (!string.IsNullOrEmpty(normalizedEmail) && EmailNormalizer.AreSame(normalizedEmail, siNormalizedEmail))
                    return true;

                return false;
            });

            if (fuzzyMatch != null)
            {
                var similarity = NameNormalizer.CalculateSimilarity(normalizedName, NameNormalizer.Normalize(fuzzyMatch.Name));
                throw new InvalidOperationException(
                    $"A potentially duplicate service installer found: '{fuzzyMatch.Name}' (ID: {fuzzyMatch.Id}, " +
                    $"Similarity: {similarity:P0}). Please verify if this is the same person before creating a new record.");
            }
        }

        // Determine InstallerType from DTO (prioritize InstallerType, fallback to IsSubcontractor for backward compatibility)
        var installerType = dto.InstallerType;
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        if (installerType == InstallerType.InHouse && dto.IsSubcontractor)
        {
            installerType = InstallerType.Subcontractor;
        }
        else if (installerType == InstallerType.Subcontractor && !dto.IsSubcontractor)
        {
            installerType = InstallerType.Subcontractor; // Use InstallerType if provided
        }
        else if (installerType == InstallerType.InHouse)
        {
            // Already set
        }
        else
        {
            // Fallback: derive from IsSubcontractor if InstallerType not provided
            installerType = dto.IsSubcontractor ? InstallerType.Subcontractor : InstallerType.InHouse;
        }
#pragma warning restore CS0618

        // Validation: Email domain check for In-House installers
        if (installerType == InstallerType.InHouse && !string.IsNullOrWhiteSpace(dto.Email))
        {
            if (!dto.Email.EndsWith("@cephas.com", StringComparison.OrdinalIgnoreCase) &&
                !dto.Email.EndsWith("@cephas.com.my", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "In-House installers must have an email address ending with @cephas.com or @cephas.com.my");
            }
        }

        // Validation: Conditional field requirements
        if (installerType == InstallerType.InHouse && string.IsNullOrWhiteSpace(dto.EmployeeId))
        {
            throw new InvalidOperationException("Employee ID is required for In-House installers");
        }

        if (installerType == InstallerType.Subcontractor && string.IsNullOrWhiteSpace(dto.ContractorId))
        {
            throw new InvalidOperationException("Contractor ID is required for Subcontractor installers");
        }

        var serviceInstaller = new CephasOps.Domain.ServiceInstallers.Entities.ServiceInstaller
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name,
            EmployeeId = dto.EmployeeId,
            Phone = dto.Phone,
            Email = dto.Email,
            SiLevel = dto.SiLevel,
            InstallerType = installerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = installerType == InstallerType.Subcontractor, // Sync for backward compatibility
#pragma warning restore CS0618
            IsActive = dto.IsActive,
            UserId = dto.UserId,
            AvailabilityStatus = dto.AvailabilityStatus ?? "Available",
            HireDate = dto.HireDate,
            EmploymentStatus = dto.EmploymentStatus,
            ContractorId = dto.ContractorId,
            ContractorCompany = dto.ContractorCompany,
            ContractStartDate = dto.ContractStartDate,
            ContractEndDate = dto.ContractEndDate,
            IcNumber = dto.IcNumber,
            BankName = dto.BankName,
            BankAccountNumber = dto.BankAccountNumber,
            Address = dto.Address,
            EmergencyContact = dto.EmergencyContact,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceInstallers.Add(serviceInstaller);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign skills if provided
        if (dto.SkillIds != null && dto.SkillIds.Any())
        {
            await AssignSkillsAsync(serviceInstaller.Id, dto.SkillIds, companyId, cancellationToken);
        }

        _logger.LogInformation("Service Installer created: {SiId}, Name: {Name}", serviceInstaller.Id, serviceInstaller.Name);

        // Auto-create stock location if service is available
        if (_locationAutoCreateService != null && companyId.HasValue)
        {
            try
            {
                await _locationAutoCreateService.CreateLocationForServiceInstallerAsync(
                    companyId.Value,
                    serviceInstaller.Id,
                    serviceInstaller.Name,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-create location for SI {SiId}", serviceInstaller.Id);
                // Don't fail the SI creation if location creation fails
            }
        }

        string? departmentName = null;
        if (serviceInstaller.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == serviceInstaller.DepartmentId.Value, cancellationToken);
            departmentName = department?.Name;
        }

        // Load skills for this installer
        var skills = await _context.ServiceInstallerSkills
            .Where(sis => sis.ServiceInstallerId == serviceInstaller.Id && sis.IsActive && !sis.IsDeleted)
            .Include(sis => sis.Skill)
            .Select(sis => new ServiceInstallerSkillDto
            {
                Id = sis.Id,
                ServiceInstallerId = sis.ServiceInstallerId,
                SkillId = sis.SkillId,
                Skill = new SkillDto
                {
                    Id = sis.Skill.Id,
                    Name = sis.Skill.Name,
                    Code = sis.Skill.Code,
                    Category = sis.Skill.Category,
                    Description = sis.Skill.Description,
                    IsActive = sis.Skill.IsActive,
                    DisplayOrder = sis.Skill.DisplayOrder
                },
                AcquiredAt = sis.AcquiredAt,
                VerifiedAt = sis.VerifiedAt,
                VerifiedByUserId = sis.VerifiedByUserId,
                Notes = sis.Notes,
                IsActive = sis.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ServiceInstallerDto
        {
            Id = serviceInstaller.Id,
            CompanyId = serviceInstaller.CompanyId,
            DepartmentId = serviceInstaller.DepartmentId,
            DepartmentName = departmentName,
            Name = serviceInstaller.Name,
            EmployeeId = serviceInstaller.EmployeeId,
            Phone = serviceInstaller.Phone,
            Email = serviceInstaller.Email,
            SiLevel = serviceInstaller.SiLevel,
            InstallerType = serviceInstaller.InstallerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = serviceInstaller.IsSubcontractor, // Backward compatibility
#pragma warning restore CS0618
            IsActive = serviceInstaller.IsActive,
            UserId = serviceInstaller.UserId,
            AvailabilityStatus = serviceInstaller.AvailabilityStatus,
            HireDate = serviceInstaller.HireDate,
            EmploymentStatus = serviceInstaller.EmploymentStatus,
            ContractorId = serviceInstaller.ContractorId,
            ContractorCompany = serviceInstaller.ContractorCompany,
            ContractStartDate = serviceInstaller.ContractStartDate,
            ContractEndDate = serviceInstaller.ContractEndDate,
            IcNumber = serviceInstaller.IcNumber,
            BankName = serviceInstaller.BankName,
            BankAccountNumber = serviceInstaller.BankAccountNumber,
            Address = serviceInstaller.Address,
            EmergencyContact = serviceInstaller.EmergencyContact,
            Skills = skills,
            CreatedAt = serviceInstaller.CreatedAt
        };
    }

    public async Task<ServiceInstallerDto> UpdateServiceInstallerAsync(Guid id, UpdateServiceInstallerDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a service installer.");

        var serviceInstaller = await _context.ServiceInstallers
            .Where(si => si.Id == id && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceInstaller == null)
        {
            throw new KeyNotFoundException($"Service Installer with ID {id} not found");
        }

        // Compute new values for duplicate check
        var newName = dto.Name ?? serviceInstaller.Name;
        var newEmployeeId = dto.EmployeeId ?? serviceInstaller.EmployeeId;
        var newPhone = dto.Phone ?? serviceInstaller.Phone;
        var newEmail = dto.Email ?? serviceInstaller.Email;

        // Normalize values for comparison
        var normalizedName = NameNormalizer.Normalize(newName);
        var normalizedPhone = PhoneNumberNormalizer.Normalize(newPhone);
        var normalizedEmail = EmailNormalizer.Normalize(newEmail);
        var normalizedEmployeeId = newEmployeeId?.Trim().ToUpperInvariant() ?? string.Empty;

        // Get all existing SIs (excluding current one) for duplicate check
        var existingSIs = await _context.ServiceInstallers
            .Where(si => si.Id != id && 
                        si.CompanyId == serviceInstaller.CompanyId && 
                        !si.IsDeleted)
            .ToListAsync(cancellationToken);

        // Check for exact EmployeeId match
        if (!string.IsNullOrEmpty(normalizedEmployeeId))
        {
            var exactMatch = existingSIs.FirstOrDefault(si =>
                si.EmployeeId != null &&
                si.EmployeeId.Trim().ToUpperInvariant() == normalizedEmployeeId);

            if (exactMatch != null)
            {
                throw new InvalidOperationException(
                    $"Another service installer with employee ID '{newEmployeeId}' already exists: {exactMatch.Name} (ID: {exactMatch.Id}).");
            }
        }

        // Check for normalized matches (Name + Phone/Email)
        var normalizedMatch = existingSIs.FirstOrDefault(si =>
        {
            var siNormalizedName = NameNormalizer.Normalize(si.Name);
            var siNormalizedPhone = PhoneNumberNormalizer.Normalize(si.Phone);
            var siNormalizedEmail = EmailNormalizer.Normalize(si.Email);

            if (siNormalizedName != normalizedName)
                return false;

            if (!string.IsNullOrEmpty(normalizedPhone) && PhoneNumberNormalizer.AreSame(normalizedPhone, siNormalizedPhone))
                return true;

            if (!string.IsNullOrEmpty(normalizedEmail) && EmailNormalizer.AreSame(normalizedEmail, siNormalizedEmail))
                return true;

            return false;
        });

        if (normalizedMatch != null)
        {
            throw new InvalidOperationException(
                $"Another service installer with similar name and contact details already exists: {normalizedMatch.Name} (ID: {normalizedMatch.Id}).");
        }

        // Check for fuzzy name matches
        if (!string.IsNullOrEmpty(normalizedPhone) || !string.IsNullOrEmpty(normalizedEmail))
        {
            var fuzzyMatch = existingSIs.FirstOrDefault(si =>
            {
                var siNormalizedName = NameNormalizer.Normalize(si.Name);
                var siNormalizedPhone = PhoneNumberNormalizer.Normalize(si.Phone);
                var siNormalizedEmail = EmailNormalizer.Normalize(si.Email);

                var nameSimilarity = NameNormalizer.CalculateSimilarity(normalizedName, siNormalizedName);
                if (nameSimilarity < 0.85)
                    return false;

                if (!string.IsNullOrEmpty(normalizedPhone) && PhoneNumberNormalizer.AreSame(normalizedPhone, siNormalizedPhone))
                    return true;

                if (!string.IsNullOrEmpty(normalizedEmail) && EmailNormalizer.AreSame(normalizedEmail, siNormalizedEmail))
                    return true;

                return false;
            });

            if (fuzzyMatch != null)
            {
                var similarity = NameNormalizer.CalculateSimilarity(normalizedName, NameNormalizer.Normalize(fuzzyMatch.Name));
                throw new InvalidOperationException(
                    $"A potentially duplicate service installer found: '{fuzzyMatch.Name}' (ID: {fuzzyMatch.Id}, " +
                    $"Similarity: {similarity:P0}). Please verify if this is the same person.");
            }
        }

        // Update DepartmentId - allow setting to null explicitly
        serviceInstaller.DepartmentId = dto.DepartmentId;
        if (dto.Name != null) serviceInstaller.Name = dto.Name;
        if (dto.EmployeeId != null) serviceInstaller.EmployeeId = dto.EmployeeId;
        if (dto.Phone != null) serviceInstaller.Phone = dto.Phone;
        
        // Email validation for In-House installers
        if (dto.Email != null)
        {
            var installerType = dto.InstallerType ?? serviceInstaller.InstallerType;
            if (installerType == InstallerType.InHouse)
            {
                if (!dto.Email.EndsWith("@cephas.com", StringComparison.OrdinalIgnoreCase) &&
                    !dto.Email.EndsWith("@cephas.com.my", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "In-House installers must have an email address ending with @cephas.com or @cephas.com.my");
                }
            }
            serviceInstaller.Email = dto.Email;
        }
        
        if (dto.SiLevel.HasValue) serviceInstaller.SiLevel = dto.SiLevel.Value;
        
        // Update InstallerType (prioritize InstallerType, fallback to IsSubcontractor for backward compatibility)
        if (dto.InstallerType.HasValue)
        {
            serviceInstaller.InstallerType = dto.InstallerType.Value;
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            serviceInstaller.IsSubcontractor = dto.InstallerType.Value == InstallerType.Subcontractor; // Sync
#pragma warning restore CS0618
            
            // Validation: Conditional field requirements when type changes
            if (dto.InstallerType.Value == InstallerType.InHouse && string.IsNullOrWhiteSpace(serviceInstaller.EmployeeId))
            {
                if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    throw new InvalidOperationException("Employee ID is required for In-House installers");
                }
            }
            
            if (dto.InstallerType.Value == InstallerType.Subcontractor && string.IsNullOrWhiteSpace(serviceInstaller.ContractorId))
            {
                if (string.IsNullOrWhiteSpace(dto.ContractorId))
                {
                    throw new InvalidOperationException("Contractor ID is required for Subcontractor installers");
                }
            }
        }
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        else if (dto.IsSubcontractor.HasValue)
        {
            // Backward compatibility: derive InstallerType from IsSubcontractor
            serviceInstaller.InstallerType = dto.IsSubcontractor.Value ? InstallerType.Subcontractor : InstallerType.InHouse;
            serviceInstaller.IsSubcontractor = dto.IsSubcontractor.Value;
        }
#pragma warning restore CS0618
        
        if (dto.IsActive.HasValue) serviceInstaller.IsActive = dto.IsActive.Value;
        if (dto.UserId.HasValue) serviceInstaller.UserId = dto.UserId;
        if (dto.AvailabilityStatus != null) serviceInstaller.AvailabilityStatus = dto.AvailabilityStatus;
        if (dto.HireDate.HasValue) serviceInstaller.HireDate = dto.HireDate;
        if (dto.EmploymentStatus != null) serviceInstaller.EmploymentStatus = dto.EmploymentStatus;
        if (dto.ContractorId != null) serviceInstaller.ContractorId = dto.ContractorId;
        if (dto.ContractorCompany != null) serviceInstaller.ContractorCompany = dto.ContractorCompany;
        if (dto.ContractStartDate.HasValue) serviceInstaller.ContractStartDate = dto.ContractStartDate;
        if (dto.ContractEndDate.HasValue) serviceInstaller.ContractEndDate = dto.ContractEndDate;
        if (dto.IcNumber != null) serviceInstaller.IcNumber = dto.IcNumber;
        if (dto.BankName != null) serviceInstaller.BankName = dto.BankName;
        if (dto.BankAccountNumber != null) serviceInstaller.BankAccountNumber = dto.BankAccountNumber;
        if (dto.Address != null) serviceInstaller.Address = dto.Address;
        if (dto.EmergencyContact != null) serviceInstaller.EmergencyContact = dto.EmergencyContact;
        
        // Update skills if provided
        if (dto.SkillIds != null)
        {
            // Remove existing skills
            var existingSkills = await _context.ServiceInstallerSkills
                .Where(sis => sis.ServiceInstallerId == id && sis.IsActive && !sis.IsDeleted)
                .ToListAsync(cancellationToken);
            
            foreach (var existingSkill in existingSkills)
            {
                existingSkill.IsActive = false;
                existingSkill.IsDeleted = true;
                existingSkill.DeletedAt = DateTime.UtcNow;
            }
            
            // Add new skills
            if (dto.SkillIds.Any())
            {
                await AssignSkillsAsync(id, dto.SkillIds, companyId, cancellationToken);
            }
        }

        serviceInstaller.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service Installer updated: {SiId}", id);

        string? departmentName = null;
        if (serviceInstaller.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == serviceInstaller.DepartmentId.Value, cancellationToken);
            departmentName = department?.Name;
        }

        // Load skills for this installer
        var skills = await _context.ServiceInstallerSkills
            .Where(sis => sis.ServiceInstallerId == id && sis.IsActive && !sis.IsDeleted)
            .Include(sis => sis.Skill)
            .Select(sis => new ServiceInstallerSkillDto
            {
                Id = sis.Id,
                ServiceInstallerId = sis.ServiceInstallerId,
                SkillId = sis.SkillId,
                Skill = new SkillDto
                {
                    Id = sis.Skill.Id,
                    Name = sis.Skill.Name,
                    Code = sis.Skill.Code,
                    Category = sis.Skill.Category,
                    Description = sis.Skill.Description,
                    IsActive = sis.Skill.IsActive,
                    DisplayOrder = sis.Skill.DisplayOrder
                },
                AcquiredAt = sis.AcquiredAt,
                VerifiedAt = sis.VerifiedAt,
                VerifiedByUserId = sis.VerifiedByUserId,
                Notes = sis.Notes,
                IsActive = sis.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ServiceInstallerDto
        {
            Id = serviceInstaller.Id,
            CompanyId = serviceInstaller.CompanyId,
            DepartmentId = serviceInstaller.DepartmentId,
            DepartmentName = departmentName,
            Name = serviceInstaller.Name,
            EmployeeId = serviceInstaller.EmployeeId,
            Phone = serviceInstaller.Phone,
            Email = serviceInstaller.Email,
            SiLevel = serviceInstaller.SiLevel,
            InstallerType = serviceInstaller.InstallerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = serviceInstaller.IsSubcontractor, // Backward compatibility
#pragma warning restore CS0618
            IsActive = serviceInstaller.IsActive,
            UserId = serviceInstaller.UserId,
            AvailabilityStatus = serviceInstaller.AvailabilityStatus,
            HireDate = serviceInstaller.HireDate,
            EmploymentStatus = serviceInstaller.EmploymentStatus,
            ContractorId = serviceInstaller.ContractorId,
            ContractorCompany = serviceInstaller.ContractorCompany,
            ContractStartDate = serviceInstaller.ContractStartDate,
            ContractEndDate = serviceInstaller.ContractEndDate,
            IcNumber = serviceInstaller.IcNumber,
            BankName = serviceInstaller.BankName,
            BankAccountNumber = serviceInstaller.BankAccountNumber,
            Address = serviceInstaller.Address,
            EmergencyContact = serviceInstaller.EmergencyContact,
            Skills = skills,
            CreatedAt = serviceInstaller.CreatedAt
        };
    }

    public async Task DeleteServiceInstallerAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a service installer.");

        var serviceInstaller = await _context.ServiceInstallers
            .Where(si => si.Id == id && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceInstaller == null)
        {
            throw new KeyNotFoundException($"Service Installer with ID {id} not found");
        }

        _context.ServiceInstallers.Remove(serviceInstaller);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service Installer deleted: {SiId}", id);
    }

    public async Task<List<ServiceInstallerContactDto>> GetContactsAsync(Guid serviceInstallerId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<ServiceInstallerContactDto>();

        var contacts = await _context.ServiceInstallerContacts
            .Where(c => c.ServiceInstallerId == serviceInstallerId && c.CompanyId == effectiveCompanyId.Value)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return contacts.Select(c => new ServiceInstallerContactDto
        {
            Id = c.Id,
            ServiceInstallerId = c.ServiceInstallerId,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            ContactType = c.ContactType,
            IsPrimary = c.IsPrimary
        }).ToList();
    }

    public async Task<ServiceInstallerContactDto> CreateContactAsync(Guid serviceInstallerId, CreateServiceInstallerContactDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a service installer contact.");

        var serviceInstaller = await _context.ServiceInstallers
            .Where(si => si.Id == serviceInstallerId && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (serviceInstaller == null)
        {
            throw new KeyNotFoundException($"Service Installer with ID {serviceInstallerId} not found");
        }

        // Prevent duplicate contacts for this installer (same type + same phone/email)
        var exists = await _context.ServiceInstallerContacts.AnyAsync(c =>
            c.ServiceInstallerId == serviceInstallerId &&
            c.ContactType == dto.ContactType &&
            (
                (!string.IsNullOrEmpty(dto.Phone) && c.Phone == dto.Phone) ||
                (!string.IsNullOrEmpty(dto.Email) && c.Email == dto.Email)
            ),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A contact with the same type and contact details already exists for this service installer.");
        }

        var contact = new CephasOps.Domain.ServiceInstallers.Entities.ServiceInstallerContact
        {
            Id = Guid.NewGuid(),
            CompanyId = serviceInstaller.CompanyId,
            ServiceInstallerId = serviceInstallerId,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            ContactType = dto.ContactType,
            IsPrimary = dto.IsPrimary,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceInstallerContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceInstallerContactDto
        {
            Id = contact.Id,
            ServiceInstallerId = contact.ServiceInstallerId,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email,
            ContactType = contact.ContactType,
            IsPrimary = contact.IsPrimary
        };
    }

    public async Task<ServiceInstallerContactDto> UpdateContactAsync(Guid contactId, UpdateServiceInstallerContactDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a service installer contact.");

        var contact = await _context.ServiceInstallerContacts
            .Where(c => c.Id == contactId && c.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (contact == null)
        {
            throw new KeyNotFoundException($"Service Installer contact with ID {contactId} not found");
        }

        var newName = dto.Name ?? contact.Name;
        var newPhone = dto.Phone ?? contact.Phone;
        var newEmail = dto.Email ?? contact.Email;
        var newType = dto.ContactType ?? contact.ContactType;

        var exists = await _context.ServiceInstallerContacts.AnyAsync(c =>
            c.Id != contactId &&
            c.ServiceInstallerId == contact.ServiceInstallerId &&
            c.ContactType == newType &&
            (
                (!string.IsNullOrEmpty(newPhone) && c.Phone == newPhone) ||
                (!string.IsNullOrEmpty(newEmail) && c.Email == newEmail)
            ),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Another contact with the same type and contact details already exists for this service installer.");
        }

        if (dto.Name != null) contact.Name = dto.Name;
        if (dto.Phone != null) contact.Phone = dto.Phone;
        if (dto.Email != null) contact.Email = dto.Email;
        if (dto.ContactType != null) contact.ContactType = dto.ContactType;
        if (dto.IsPrimary.HasValue) contact.IsPrimary = dto.IsPrimary.Value;

        contact.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceInstallerContactDto
        {
            Id = contact.Id,
            ServiceInstallerId = contact.ServiceInstallerId,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email,
            ContactType = contact.ContactType,
            IsPrimary = contact.IsPrimary
        };
    }

    public async Task DeleteContactAsync(Guid contactId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a service installer contact.");

        var contact = await _context.ServiceInstallerContacts
            .Where(c => c.Id == contactId && c.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (contact == null)
        {
            throw new KeyNotFoundException($"Service Installer contact with ID {contactId} not found");
        }

        _context.ServiceInstallerContacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ServiceInstallerDto>> GetAvailableInstallersAsync(
        Guid? companyId,
        Guid? departmentId = null,
        InstallerType? installerType = null,
        InstallerLevel? siLevel = null,
        List<Guid>? requiredSkillIds = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<ServiceInstallerDto>();

        // Get available installers (Active, Available status, not on leave)
        var query = _context.ServiceInstallers
            .Where(si => si.CompanyId == effectiveCompanyId.Value &&
                        si.IsActive && 
                        (si.AvailabilityStatus == null || 
                         si.AvailabilityStatus == "Available" || 
                         si.AvailabilityStatus == "Busy"));

        if (departmentId.HasValue)
        {
            query = query.Where(si => si.DepartmentId == departmentId.Value);
        }

        if (installerType.HasValue)
        {
            query = query.Where(si => si.InstallerType == installerType.Value);
        }

        if (siLevel.HasValue)
        {
            query = query.Where(si => si.SiLevel == siLevel.Value);
        }

        // Filter by required skills - installers must have ALL specified skills
        if (requiredSkillIds != null && requiredSkillIds.Any())
        {
            query = query.Where(si => si.Skills
                .Where(s => s.IsActive && !s.IsDeleted)
                .Select(s => s.SkillId)
                .Count(skillId => requiredSkillIds.Contains(skillId)) == requiredSkillIds.Count);
        }

        var installers = await query
            .OrderBy(si => si.Name)
            .ToListAsync(cancellationToken);

        // Get department names
        var departmentIds = installers
            .Where(si => si.DepartmentId.HasValue)
            .Select(si => si.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        return installers.Select(si => new ServiceInstallerDto
        {
            Id = si.Id,
            CompanyId = si.CompanyId,
            DepartmentId = si.DepartmentId,
            DepartmentName = si.DepartmentId.HasValue && departments.ContainsKey(si.DepartmentId.Value)
                ? departments[si.DepartmentId.Value]
                : null,
            Name = si.Name,
            EmployeeId = si.EmployeeId,
            Phone = si.Phone,
            Email = si.Email,
            SiLevel = si.SiLevel,
            InstallerType = si.InstallerType,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            IsSubcontractor = si.IsSubcontractor,
#pragma warning restore CS0618
            IsActive = si.IsActive,
            UserId = si.UserId,
            AvailabilityStatus = si.AvailabilityStatus,
            HireDate = si.HireDate,
            EmploymentStatus = si.EmploymentStatus,
            ContractorId = si.ContractorId,
            ContractorCompany = si.ContractorCompany,
            ContractStartDate = si.ContractStartDate,
            ContractEndDate = si.ContractEndDate,
            IcNumber = si.IcNumber,
            BankName = si.BankName,
            BankAccountNumber = si.BankAccountNumber,
            Address = si.Address,
            EmergencyContact = si.EmergencyContact,
            CreatedAt = si.CreatedAt
        }).ToList();
    }

    public async Task<List<ServiceInstallerSkillDto>> GetInstallerSkillsAsync(Guid serviceInstallerId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<ServiceInstallerSkillDto>();

        var installer = await _context.ServiceInstallers
            .Where(si => si.Id == serviceInstallerId && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (installer == null)
            return new List<ServiceInstallerSkillDto>();

        var skills = await _context.ServiceInstallerSkills
            .Where(sis => sis.ServiceInstallerId == serviceInstallerId && sis.IsActive && !sis.IsDeleted)
            .Include(sis => sis.Skill)
            .Select(sis => new ServiceInstallerSkillDto
            {
                Id = sis.Id,
                ServiceInstallerId = sis.ServiceInstallerId,
                SkillId = sis.SkillId,
                Skill = new SkillDto
                {
                    Id = sis.Skill.Id,
                    Name = sis.Skill.Name,
                    Code = sis.Skill.Code,
                    Category = sis.Skill.Category,
                    Description = sis.Skill.Description,
                    IsActive = sis.Skill.IsActive,
                    DisplayOrder = sis.Skill.DisplayOrder
                },
                AcquiredAt = sis.AcquiredAt,
                VerifiedAt = sis.VerifiedAt,
                VerifiedByUserId = sis.VerifiedByUserId,
                Notes = sis.Notes,
                IsActive = sis.IsActive
            })
            .OrderBy(sis => sis.Skill!.Category)
            .ThenBy(sis => sis.Skill!.DisplayOrder)
            .ThenBy(sis => sis.Skill!.Name)
            .ToListAsync(cancellationToken);

        return skills;
    }

    public async Task<List<ServiceInstallerSkillDto>> AssignSkillsAsync(Guid serviceInstallerId, List<Guid> skillIds, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to assign skills to a service installer.");

        var installer = await _context.ServiceInstallers
            .Where(si => si.Id == serviceInstallerId && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (installer == null)
            throw new KeyNotFoundException($"Service Installer with ID {serviceInstallerId} not found");

        // Verify all skills exist
        var existingSkills = await _context.Skills
            .Where(s => skillIds.Contains(s.Id) && s.IsActive && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        if (existingSkills.Count != skillIds.Count)
        {
            var missingSkillIds = skillIds.Except(existingSkills.Select(s => s.Id)).ToList();
            throw new InvalidOperationException($"One or more skills not found: {string.Join(", ", missingSkillIds)}");
        }

        var assignedSkills = new List<ServiceInstallerSkillDto>();

        foreach (var skillId in skillIds)
        {
            // Check if skill is already assigned (active)
            var existingAssignment = await _context.ServiceInstallerSkills
                .Where(sis => sis.ServiceInstallerId == serviceInstallerId &&
                            sis.SkillId == skillId &&
                            sis.IsActive &&
                            !sis.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignment == null)
            {
                // Create new assignment
                var skillAssignment = new CephasOps.Domain.ServiceInstallers.Entities.ServiceInstallerSkill
                {
                    Id = Guid.NewGuid(),
                    CompanyId = effectiveCompanyId.Value,
                    ServiceInstallerId = serviceInstallerId,
                    SkillId = skillId,
                    AcquiredAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ServiceInstallerSkills.Add(skillAssignment);

                var skill = existingSkills.First(s => s.Id == skillId);
                assignedSkills.Add(new ServiceInstallerSkillDto
                {
                    Id = skillAssignment.Id,
                    ServiceInstallerId = skillAssignment.ServiceInstallerId,
                    SkillId = skillAssignment.SkillId,
                    Skill = new SkillDto
                    {
                        Id = skill.Id,
                        Name = skill.Name,
                        Code = skill.Code,
                        Category = skill.Category,
                        Description = skill.Description,
                        IsActive = skill.IsActive,
                        DisplayOrder = skill.DisplayOrder
                    },
                    AcquiredAt = skillAssignment.AcquiredAt,
                    IsActive = skillAssignment.IsActive
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Assigned {Count} skills to Service Installer {SiId}", assignedSkills.Count, serviceInstallerId);

        return assignedSkills;
    }

    public async Task RemoveSkillAsync(Guid serviceInstallerId, Guid skillId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to remove a skill from a service installer.");

        var installer = await _context.ServiceInstallers
            .Where(si => si.Id == serviceInstallerId && si.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (installer == null)
            throw new KeyNotFoundException($"Service Installer with ID {serviceInstallerId} not found");

        // Find and soft-delete the skill assignment
        var skillAssignment = await _context.ServiceInstallerSkills
            .Where(sis => sis.ServiceInstallerId == serviceInstallerId &&
                         sis.SkillId == skillId &&
                         sis.IsActive &&
                         !sis.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (skillAssignment == null)
        {
            throw new KeyNotFoundException($"Skill assignment not found for Service Installer {serviceInstallerId} and Skill {skillId}");
        }

        skillAssignment.IsActive = false;
        skillAssignment.IsDeleted = true;
        skillAssignment.DeletedAt = DateTime.UtcNow;
        skillAssignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Removed skill {SkillId} from Service Installer {SiId}", skillId, serviceInstallerId);
    }

    // CSV Import feature not yet implemented
    // public async Task<CephasOps.Application.Common.DTOs.ImportResult<ServiceInstallerCsvDto>> ImportServiceInstallersAsync(List<ServiceInstallerCsvDto> records, Guid? companyId, CancellationToken cancellationToken = default)
    // {
    //     ...
    // }
}

