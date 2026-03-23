using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Email rules management endpoints
/// </summary>
[ApiController]
[Route("api/email-rules")]
[Authorize]
public class EmailRulesController : ControllerBase
{
    private readonly IEmailRuleService _emailRuleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<EmailRulesController> _logger;

    public EmailRulesController(
        IEmailRuleService emailRuleService,
        ICurrentUserService currentUserService,
        ILogger<EmailRulesController> logger)
    {
        _emailRuleService = emailRuleService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all email rules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email rules sorted by priority</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailRuleDto>>>> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _emailRuleService.GetAllAsync(cancellationToken);
            return this.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email rules");
            return this.InternalServerError<List<EmailRuleDto>>($"Failed to get email rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Get email rule by ID
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email rule details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailRuleDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _emailRuleService.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                return this.NotFound<EmailRuleDto>($"Email rule with ID {id} not found");
            }
            return this.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email rule: {RuleId}", id);
            return this.InternalServerError<EmailRuleDto>($"Failed to get email rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new email rule
    /// </summary>
    /// <param name="dto">Email rule data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created email rule</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailRuleDto>>> Create(
        [FromBody] CreateEmailRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<EmailRuleDto>("User context required");
        }

        try
        {
            var rule = await _emailRuleService.CreateAsync(dto, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule, "Email rule created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<EmailRuleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email rule");
            return this.InternalServerError<EmailRuleDto>($"Failed to create email rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing email rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="dto">Updated email rule data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated email rule</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EmailRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailRuleDto>>> Update(
        Guid id,
        [FromBody] UpdateEmailRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<EmailRuleDto>("User context required");
        }

        try
        {
            var rule = await _emailRuleService.UpdateAsync(id, dto, userId.Value, cancellationToken);
            return this.Success(rule, "Email rule updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<EmailRuleDto>($"Email rule with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<EmailRuleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email rule: {RuleId}", id);
            return this.InternalServerError<EmailRuleDto>($"Failed to update email rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete an email rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _emailRuleService.DeleteAsync(id, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Email rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email rule: {RuleId}", id);
            return this.InternalServerError($"Failed to delete email rule: {ex.Message}");
        }
    }
}
