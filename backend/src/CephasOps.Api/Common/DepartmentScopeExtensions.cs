using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Common;

/// <summary>
/// Extension methods for resolving department scope in controllers with consistent 400/403 behavior.
/// Use when an endpoint is department-scoped: permission + department access are both required.
/// </summary>
public static class DepartmentScopeExtensions
{
    private const string MessageDepartmentRequired = "Department selection is required";

    /// <summary>
    /// Resolves the effective department scope for the current user, or returns an error result.
    /// Use for department-scoped endpoints: pass query/route departmentId or null to use X-Department-Id/query from context.
    /// </summary>
    /// <param name="controller">The controller (for returning error responses).</param>
    /// <param name="departmentAccessService">Department access service.</param>
    /// <param name="departmentRequestContext">Request context (header/query department).</param>
    /// <param name="requestedDepartmentId">Explicit department from route/query/body, or null to use context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success: (resolved scope, null). On failure: (null, ActionResult to return). 400 when department is required but missing; 403 when no access.</returns>
    public static async Task<(Guid? Scope, ActionResult? Error)> ResolveDepartmentScopeOrFailAsync(
        this ControllerBase controller,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        Guid? requestedDepartmentId,
        CancellationToken cancellationToken)
    {
        var fromRequest = requestedDepartmentId ?? departmentRequestContext.DepartmentId;
        try
        {
            var scope = await departmentAccessService.ResolveDepartmentScopeAsync(fromRequest, cancellationToken);
            return (scope, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            var msg = ex.Message ?? string.Empty;
            var isRequired = msg.IndexOf(MessageDepartmentRequired, StringComparison.OrdinalIgnoreCase) >= 0;
            var statusCode = isRequired ? 400 : 403;
            var response = controller.StatusCode(statusCode, ApiResponse.ErrorResponse(msg));
            return (null, response);
        }
    }

    /// <summary>
    /// Ensures the current user has access to the given department, or returns an error result.
    /// Use when the department is already known (e.g. from route or body).
    /// </summary>
    /// <param name="controller">The controller (for returning error responses).</param>
    /// <param name="departmentAccessService">Department access service.</param>
    /// <param name="departmentId">Department to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success: (true, null). On failure: (false, ActionResult to return).</returns>
    public static async Task<(bool Allowed, ActionResult? Error)> EnsureDepartmentAccessOrFailAsync(
        this ControllerBase controller,
        IDepartmentAccessService departmentAccessService,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await departmentAccessService.EnsureAccessAsync(departmentId, cancellationToken);
            return (true, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            var response = controller.StatusCode(403, ApiResponse.ErrorResponse(ex.Message ?? "You do not have access to this department"));
            return (false, response);
        }
    }
}
