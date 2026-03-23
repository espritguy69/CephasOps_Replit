using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Common;

/// <summary>
/// Extension methods for controllers to return standardized API responses
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Requires a valid company context for company-scoped operations.
    /// Uses ITenantProvider (canonical effective company: X-Company-Id, JWT, Department fallback). Do not use CurrentUser.CompanyId directly.
    /// When effective company is null or Guid.Empty, returns 403.
    /// </summary>
    /// <param name="controller">The controller (for building the response).</param>
    /// <param name="tenantProvider">Canonical tenant provider (effective company for this request).</param>
    /// <returns>(companyId, null) when context is valid; (default, ActionResult) when missing (caller should return the ActionResult).</returns>
    public static (Guid companyId, ActionResult? error) RequireCompanyId(this ControllerBase controller, ITenantProvider tenantProvider)
    {
        var id = tenantProvider.CurrentTenantId;
        if (id.HasValue && id.Value != Guid.Empty)
            return (id.Value, null);
        return (default, controller.StatusCode(403, ApiResponse.ErrorResponse("Company context is required for this operation.")));
    }

    /// <summary>
    /// Return a successful response with data
    /// </summary>
    public static ActionResult<ApiResponse<T>> Success<T>(this ControllerBase controller, T data, string? message = null)
    {
        return controller.Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    /// <summary>
    /// Return a successful response without data
    /// </summary>
    public static ActionResult<ApiResponse> Success(this ControllerBase controller, string? message = null)
    {
        return controller.Ok(ApiResponse.SuccessResponse(message));
    }

    /// <summary>
    /// Return an error response
    /// </summary>
    public static ActionResult<ApiResponse<T>> Error<T>(this ControllerBase controller, string error, int statusCode = 400)
    {
        var response = ApiResponse<T>.ErrorResponse(error);
        return controller.StatusCode(statusCode, response);
    }

    /// <summary>
    /// Return a 400 Bad Request response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse<T>> BadRequest<T>(this ControllerBase controller, string message = "Bad Request")
    {
        return controller.Error<T>(message, 400);
    }

    /// <summary>
    /// Return an error response with multiple errors
    /// </summary>
    public static ActionResult<ApiResponse<T>> Error<T>(this ControllerBase controller, List<string> errors, string? message = null, int statusCode = 400)
    {
        var response = ApiResponse<T>.ErrorResponse(errors, message);
        return controller.StatusCode(statusCode, response);
    }

    /// <summary>
    /// Return a not found response
    /// </summary>
    public static ActionResult<ApiResponse<T>> NotFound<T>(this ControllerBase controller, string message = "Resource not found")
    {
        return controller.Error<T>(message, 404);
    }

    /// <summary>
    /// Return a not found response (no data)
    /// </summary>
    public static ActionResult<ApiResponse> NotFound(this ControllerBase controller, string message = "Resource not found")
    {
        return controller.StatusCode(404, ApiResponse.ErrorResponse(message));
    }

    /// <summary>
    /// Return a 400 Bad Request response (no data)
    /// </summary>
    public static ActionResult<ApiResponse> BadRequest(this ControllerBase controller, string message = "Bad Request")
    {
        return controller.StatusCode(400, ApiResponse.ErrorResponse(message));
    }

    /// <summary>
    /// Returns a 401 Unauthorized response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse<T>> Unauthorized<T>(this ControllerBase controller, string message = "Unauthorized")
    {
        return controller.Error<T>(message, 401);
    }

    /// <summary>
    /// Returns a 403 Forbidden response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse<T>> Forbidden<T>(this ControllerBase controller, string message = "Forbidden")
    {
        return controller.Error<T>(message, 403);
    }

    /// <summary>
    /// Returns a 500 Internal Server Error response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse<T>> InternalServerError<T>(this ControllerBase controller, string message = "Internal Server Error", List<string>? errors = null)
    {
        if (errors != null && errors.Count > 0)
        {
            return controller.Error<T>(errors, message, 500);
        }
        return controller.Error<T>(message, 500);
    }

    /// <summary>
    /// Returns a 500 Internal Server Error response without data.
    /// </summary>
    public static ActionResult<ApiResponse> InternalServerError(this ControllerBase controller, string message = "Internal Server Error", List<string>? errors = null)
    {
        var response = errors != null && errors.Count > 0
            ? ApiResponse.ErrorResponse(errors, message)
            : ApiResponse.ErrorResponse(message);
        return controller.StatusCode(500, response);
    }

    /// <summary>
    /// Returns a 204 No Content response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse> NoContent(this ControllerBase controller, string? message = null)
    {
        var response = ApiResponse.SuccessResponse(message ?? "Operation completed successfully.");
        return controller.StatusCode(204, response);
    }

    /// <summary>
    /// Returns a 201 Created response with a standard API envelope.
    /// </summary>
    public static ActionResult<ApiResponse<T>> CreatedAtAction<T>(
        this ControllerBase controller,
        string? actionName,
        object? routeValues,
        T data,
        string? message = null)
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        return controller.CreatedAtAction(actionName, routeValues, response);
    }
}

