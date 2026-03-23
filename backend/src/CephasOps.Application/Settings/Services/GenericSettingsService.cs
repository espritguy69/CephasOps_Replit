using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Generic settings service for simple CRUD entities: Bins, Brands, ServicePlans, ProductTypes, Teams, CostCentres
/// </summary>
public class GenericSettingsService<TEntity> where TEntity : class
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;

    public GenericSettingsService(ApplicationDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TEntity>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();
        
        // Apply company filter if entity has CompanyId property
        var companyIdProp = typeof(TEntity).GetProperty("CompanyId");
        if (companyIdProp != null)
        {
            query = query.Where(e => EF.Property<Guid>(e, "CompanyId") == companyId);
        }
        
        // Apply IsDeleted filter
        var isDeletedProp = typeof(TEntity).GetProperty("IsDeleted");
        if (isDeletedProp != null)
        {
            query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
        }
        
        // Apply IsActive filter if specified
        if (isActive.HasValue)
        {
            var isActiveProp = typeof(TEntity).GetProperty("IsActive");
            if (isActiveProp != null)
            {
                query = query.Where(e => EF.Property<bool>(e, "IsActive") == isActive.Value);
            }
        }
        
        return await query.ToListAsync(cancellationToken);
    }
}

