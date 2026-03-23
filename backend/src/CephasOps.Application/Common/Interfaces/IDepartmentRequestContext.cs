namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Provides access to the department scope supplied with the incoming HTTP request.
/// </summary>
public interface IDepartmentRequestContext
{
    /// <summary>
    /// Department identifier extracted from headers or query string (if present).
    /// </summary>
    Guid? DepartmentId { get; }

    /// <summary>
    /// Whether the request explicitly supplied a department scope.
    /// </summary>
    bool HasDepartmentScope { get; }
}


