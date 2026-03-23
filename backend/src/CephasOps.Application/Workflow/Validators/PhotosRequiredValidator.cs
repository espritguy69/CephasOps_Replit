using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if photos are required/uploaded for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class PhotosRequiredValidator : IGuardConditionValidator
{
    public string Key => "photosRequired";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PhotosRequiredValidator> _logger;

    public PhotosRequiredValidator(
        ApplicationDbContext context,
        ILogger<PhotosRequiredValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for photos validation", entityId);
            return false;
        }

        // Check flag if config says to (default: true)
        bool checkFlag = config?.GetValueOrDefault("checkFlag")?.ToString() != "false";
        if (checkFlag && order.PhotosUploaded)
        {
            _logger.LogDebug("Order {OrderId} has PhotosUploaded flag set", entityId);
            return true;
        }

        // Check files if config says to (default: true)
        bool checkFiles = config?.GetValueOrDefault("checkFiles")?.ToString() != "false";
        if (checkFiles)
        {
            var hasPhotos = await _context.Files
                .Where(f => f.EntityType == "Order" 
                    && f.EntityId == entityId 
                    && f.ContentType.StartsWith("image/"))
                .AnyAsync(cancellationToken);
            
            if (hasPhotos)
            {
                _logger.LogDebug("Order {OrderId} has photo files uploaded", entityId);
                return true;
            }
        }

        _logger.LogDebug("Order {OrderId} does not meet photos required condition", entityId);
        return false;
    }
}

