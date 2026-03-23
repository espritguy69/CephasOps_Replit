using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CephasOps.Api;

/// <summary>
/// Middleware to handle Swagger generation errors gracefully
/// Catches errors during swagger.json generation for file upload endpoints
/// </summary>
public class SwaggerErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SwaggerErrorHandlingMiddleware> _logger;

    public SwaggerErrorHandlingMiddleware(RequestDelegate next, ILogger<SwaggerErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only handle Swagger JSON requests
        if (context.Request.Path.StartsWithSegments("/swagger") && 
            context.Request.Path.Value?.EndsWith(".json") == true)
        {
            try
            {
                await _next(context);
            }
            catch (SwaggerGeneratorException ex) when (ex.Message.Contains("IFormFile") || ex.Message.Contains("FromForm"))
            {
                _logger.LogWarning(ex, "Swagger generation error for file upload endpoint. Operation filters should handle this, but error occurred during parameter generation.");
                
                // Log the error but don't return a response - let it bubble up
                // The operation filter should prevent this, but if it doesn't, we need to fix the filter
                throw; // Re-throw to see the actual error
            }
        }
        else
        {
            await _next(context);
        }
    }
}

