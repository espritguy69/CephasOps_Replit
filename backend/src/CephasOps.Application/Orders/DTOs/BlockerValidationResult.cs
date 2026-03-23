namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// Result of blocker validation
/// </summary>
public class BlockerValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? BlockerContext { get; set; } // "PreCustomer" or "PostCustomer"
    public List<string> AllowedReasons { get; set; } = new();

    public static BlockerValidationResult Success(string blockerContext, List<string> allowedReasons) => new()
    {
        IsValid = true,
        BlockerContext = blockerContext,
        AllowedReasons = allowedReasons
    };

    public static BlockerValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    public static BlockerValidationResult Failure(string blockerContext, List<string> allowedReasons, params string[] errors) => new()
    {
        IsValid = false,
        BlockerContext = blockerContext,
        AllowedReasons = allowedReasons,
        Errors = errors.ToList()
    };
}

