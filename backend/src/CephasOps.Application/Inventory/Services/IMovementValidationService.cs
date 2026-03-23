using CephasOps.Application.Inventory.DTOs;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Service for validating stock movement rules based on MovementType configuration
/// </summary>
public interface IMovementValidationService
{
    /// <summary>
    /// Validates a stock movement request against the configured MovementType rules
    /// </summary>
    /// <param name="dto">The stock movement DTO to validate</param>
    /// <param name="movementTypeId">The MovementType ID (if provided, will validate against it)</param>
    /// <param name="companyId">Company ID (multi-tenant SaaS — required).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors if invalid</returns>
    Task<MovementValidationResult> ValidateMovementAsync(
        CreateStockMovementDto dto,
        Guid? movementTypeId,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the MovementType by code
    /// </summary>
    Task<Domain.Inventory.Entities.MovementType?> GetMovementTypeByCodeAsync(
        string code,
        Guid? companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of movement validation
/// </summary>
public class MovementValidationResult
{
    public bool IsValid { get; private set; }
    public List<string> Errors { get; private set; } = new();
    public Domain.Inventory.Entities.MovementType? MovementType { get; private set; }

    public static MovementValidationResult Success(Domain.Inventory.Entities.MovementType? movementType = null)
    {
        return new MovementValidationResult
        {
            IsValid = true,
            MovementType = movementType
        };
    }

    public static MovementValidationResult Failure(string error, Domain.Inventory.Entities.MovementType? movementType = null)
    {
        return new MovementValidationResult
        {
            IsValid = false,
            Errors = new List<string> { error },
            MovementType = movementType
        };
    }

    public static MovementValidationResult Failure(List<string> errors, Domain.Inventory.Entities.MovementType? movementType = null)
    {
        return new MovementValidationResult
        {
            IsValid = false,
            Errors = errors ?? new List<string>(),
            MovementType = movementType
        };
    }
}

