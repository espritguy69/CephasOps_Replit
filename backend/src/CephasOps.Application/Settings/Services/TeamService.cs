using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public interface ITeamService
{
    Task<List<TeamDto>> GetAllAsync(Guid companyId, bool? isActive = null);
    Task<TeamDto?> GetByIdAsync(Guid id);
    Task<TeamDto> CreateAsync(Guid companyId, CreateTeamDto dto);
    Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto dto);
    Task DeleteAsync(Guid id);
}

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;

    public TeamService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TeamDto>> GetAllAsync(Guid companyId, bool? isActive = null)
    {
        var query = _context.Set<Team>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        return items.Select(x => new TeamDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            DepartmentId = x.DepartmentId,
            DepartmentName = x.DepartmentName,
            TeamLeaderId = x.TeamLeaderId,
            TeamLeaderName = x.TeamLeaderName,
            MemberCount = x.MemberCount,
            ActiveJobsCount = x.ActiveJobsCount,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<TeamDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.Set<Team>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null) return null;

        return new TeamDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            DepartmentId = item.DepartmentId,
            DepartmentName = item.DepartmentName,
            TeamLeaderId = item.TeamLeaderId,
            TeamLeaderName = item.TeamLeaderName,
            MemberCount = item.MemberCount,
            ActiveJobsCount = item.ActiveJobsCount,
            IsActive = item.IsActive
        };
    }

    public async Task<TeamDto> CreateAsync(Guid companyId, CreateTeamDto dto)
    {
        var item = new Team
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            DepartmentId = dto.DepartmentId,
            TeamLeaderId = dto.TeamLeaderId,
            MemberCount = 0,
            ActiveJobsCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Team>().Add(item);
        await _context.SaveChangesAsync();

        return new TeamDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            DepartmentId = item.DepartmentId,
            DepartmentName = item.DepartmentName,
            TeamLeaderId = item.TeamLeaderId,
            TeamLeaderName = item.TeamLeaderName,
            MemberCount = item.MemberCount,
            ActiveJobsCount = item.ActiveJobsCount,
            IsActive = item.IsActive
        };
    }

    public async Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto dto)
    {
        var item = await _context.Set<Team>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Team not found");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.DepartmentId = dto.DepartmentId;
        item.TeamLeaderId = dto.TeamLeaderId;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new TeamDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            DepartmentId = item.DepartmentId,
            DepartmentName = item.DepartmentName,
            TeamLeaderId = item.TeamLeaderId,
            TeamLeaderName = item.TeamLeaderName,
            MemberCount = item.MemberCount,
            ActiveJobsCount = item.ActiveJobsCount,
            IsActive = item.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<Team>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
            throw new Exception("Team not found");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

