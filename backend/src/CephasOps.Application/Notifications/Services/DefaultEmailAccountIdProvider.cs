using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Returns the first active email account id for notification sending (Phase 6).
/// </summary>
public class DefaultEmailAccountIdProvider : IDefaultEmailAccountIdProvider
{
    private readonly ApplicationDbContext _context;

    public DefaultEmailAccountIdProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Guid?> GetDefaultEmailAccountIdAsync(CancellationToken cancellationToken = default)
    {
        var account = await _context.EmailAccounts
            .Where(ea => ea.IsActive)
            .OrderBy(ea => ea.Name)
            .Select(ea => new { ea.Id })
            .FirstOrDefaultAsync(cancellationToken);
        return account?.Id;
    }
}
