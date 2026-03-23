using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notification-templates")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly INotificationTemplateService _service;

    public NotificationTemplatesController(INotificationTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationTemplateDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NotificationTemplateDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<NotificationTemplateDto>>($"Failed to get notification templates: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<NotificationTemplateDto>($"Notification template with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<NotificationTemplateDto>($"Failed to get notification template: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> Create([FromQuery] Guid companyId, [FromBody] NotificationTemplateDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<NotificationTemplateDto>.SuccessResponse(item, "Notification template created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<NotificationTemplateDto>($"Failed to create notification template: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> Update(Guid id, [FromBody] NotificationTemplateDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Notification template updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<NotificationTemplateDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<NotificationTemplateDto>($"Failed to update notification template: {ex.Message}", 500);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Notification template deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete notification template: {ex.Message}"));
        }
    }
}

