using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Parser templates management endpoints
/// </summary>
[ApiController]
[Route("api/parser-templates")]
[Authorize]
public class ParserTemplatesController : ControllerBase
{
    private readonly IParserTemplateService _parserTemplateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ParserTemplatesController> _logger;

    public ParserTemplatesController(
        IParserTemplateService parserTemplateService,
        ICurrentUserService currentUserService,
        ILogger<ParserTemplatesController> logger)
    {
        _parserTemplateService = parserTemplateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all parser templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of parser templates sorted by priority</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ParserTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ParserTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ParserTemplateDto>>>> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _parserTemplateService.GetAllAsync(cancellationToken);
            return this.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parser templates");
            return this.InternalServerError<List<ParserTemplateDto>>($"Failed to get parser templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parser template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser template details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _parserTemplateService.GetByIdAsync(id, cancellationToken);
            if (template == null)
            {
                return this.NotFound<ParserTemplateDto>($"Parser template with ID {id} not found");
            }
            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parser template: {TemplateId}", id);
            return this.InternalServerError<ParserTemplateDto>($"Failed to get parser template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get parser template by code
    /// </summary>
    /// <param name="code">Template code (e.g., TIME_FTTH)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser template details</returns>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateDto>>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _parserTemplateService.GetByCodeAsync(code, cancellationToken);
            if (template == null)
            {
                return this.NotFound<ParserTemplateDto>($"Parser template with code '{code}' not found");
            }
            return this.Success(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parser template by code: {Code}", code);
            return this.InternalServerError<ParserTemplateDto>($"Failed to get parser template: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new parser template
    /// </summary>
    /// <param name="dto">Parser template data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created parser template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateDto>>> Create(
        [FromBody] CreateParserTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<ParserTemplateDto>("User context required");
        }

        try
        {
            var template = await _parserTemplateService.CreateAsync(dto, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetById), new { id = template.Id }, template, "Parser template created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<ParserTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parser template");
            return this.InternalServerError<ParserTemplateDto>($"Failed to create parser template: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing parser template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="dto">Updated parser template data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parser template</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateDto>>> Update(
        Guid id,
        [FromBody] UpdateParserTemplateDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<ParserTemplateDto>("User context required");
        }

        try
        {
            var template = await _parserTemplateService.UpdateAsync(id, dto, userId.Value, cancellationToken);
            return this.Success(template, "Parser template updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParserTemplateDto>($"Parser template with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<ParserTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parser template: {TemplateId}", id);
            return this.InternalServerError<ParserTemplateDto>($"Failed to update parser template: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a parser template
    /// </summary>
    /// <param name="id">Template ID</param>
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
            await _parserTemplateService.DeleteAsync(id, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Parser template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parser template: {TemplateId}", id);
            return this.InternalServerError($"Failed to delete parser template: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle auto-approve setting for a parser template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Auto-approve setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated parser template</returns>
    [HttpPost("{id}/toggle-auto-approve")]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateDto>>> ToggleAutoApprove(
        Guid id,
        [FromBody] ToggleAutoApproveRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<ParserTemplateDto>("User context required");
        }

        try
        {
            var template = await _parserTemplateService.ToggleAutoApproveAsync(id, request.AutoApprove, userId.Value, cancellationToken);
            return this.Success(template, "Auto-approve setting toggled successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ParserTemplateDto>($"Parser template with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling auto-approve for parser template: {TemplateId}", id);
            return this.InternalServerError<ParserTemplateDto>($"Failed to toggle auto-approve: {ex.Message}");
        }
    }

    /// <summary>
    /// Test a parser template with sample email data
    /// </summary>
    /// <param name="id">Template ID to test</param>
    /// <param name="testData">Sample email data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result with matching status</returns>
    [HttpPost("{id}/test")]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateTestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateTestResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ParserTemplateTestResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ParserTemplateTestResultDto>>> TestTemplate(
        Guid id,
        [FromBody] ParserTemplateTestDataDto testData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _parserTemplateService.TestTemplateAsync(id, testData, cancellationToken);
            return this.Success(result, "Template test completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing parser template: {TemplateId}", id);
            return this.InternalServerError<ParserTemplateTestResultDto>($"Failed to test template: {ex.Message}");
        }
    }
}

/// <summary>
/// Request to toggle auto-approve setting
/// </summary>
public class ToggleAutoApproveRequest
{
    public bool AutoApprove { get; set; }
}
