using CephasOps.Api.Common;
using CephasOps.Application.Provisioning;
using CephasOps.Application.Provisioning.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Public self-service tenant signup. No auth required.</summary>
[ApiController]
[Route("api/platform")]
public class PlatformSignupController : ControllerBase
{
    private readonly ISignupService _signupService;
    private readonly ILogger<PlatformSignupController> _logger;

    public PlatformSignupController(ISignupService signupService, ILogger<PlatformSignupController> logger)
    {
        _signupService = signupService;
        _logger = logger;
    }

    /// <summary>Self-service signup: create tenant, trial subscription, and admin user. Validates email, company code and slug uniqueness.</summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SignupResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SignupResultDto>>> Signup(
        [FromBody] SignupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return this.Error<SignupResultDto>("Request body required", 400);

        try
        {
            var result = await _signupService.SignupAsync(request, cancellationToken);
            return StatusCode(201, ApiResponse<SignupResultDto>.SuccessResponse(result, result.Message));
        }
        catch (ArgumentException ex)
        {
            return this.Error<SignupResultDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Signup conflict");
            return StatusCode(409, ApiResponse<SignupResultDto>.ErrorResponse(ex.Message));
        }
    }
}
