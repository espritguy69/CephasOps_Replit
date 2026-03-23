namespace CephasOps.Api.Common;

/// <summary>
/// Standard API response envelope following CephasOps_API_Overview.md specification.
/// All API responses should use this format for consistency.
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Response data payload
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// List of error messages (if any)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = new List<string>()
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string error, List<string>? additionalErrors = null)
    {
        var errors = new List<string> { error };
        if (additionalErrors != null)
        {
            errors.AddRange(additionalErrors);
        }

        return new ApiResponse<T>
        {
            Success = false,
            Message = error,
            Data = default,
            Errors = errors
        };
    }

    /// <summary>
    /// Create an error response with multiple errors
    /// </summary>
    public static ApiResponse<T> ErrorResponse(List<string> errors, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message ?? "One or more errors occurred",
            Data = default,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic API response for responses without data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = null,
            Errors = new List<string>()
        };
    }

    public static new ApiResponse ErrorResponse(string error, List<string>? additionalErrors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = error,
            Data = null,
            Errors = additionalErrors != null ? new List<string> { error }.Concat(additionalErrors).ToList() : new List<string> { error }
        };
    }
}

