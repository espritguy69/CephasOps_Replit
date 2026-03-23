using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/payment-terms")]
public class PaymentTermsController : ControllerBase
{
    private readonly IPaymentTermService _service;
    private readonly ITenantProvider _tenantProvider;

    public PaymentTermsController(IPaymentTermService service, ITenantProvider tenantProvider)
    {
        _service = service;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentTermDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentTermDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentTermDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PaymentTermDto>>>> GetAll([FromQuery] bool? isActive = null)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null)
            return err;
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<PaymentTermDto>>($"Failed to get payment terms: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentTermDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<PaymentTermDto>($"Payment term with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<PaymentTermDto>($"Failed to get payment term: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentTermDto>>> Create([FromBody] CreatePaymentTermDto dto)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null)
            return err;
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<PaymentTermDto>.SuccessResponse(item, "Payment term created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<PaymentTermDto>($"Failed to create payment term: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTermDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaymentTermDto>>> Update(Guid id, [FromBody] UpdatePaymentTermDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            if (item == null)
                return this.NotFound<PaymentTermDto>($"Payment term with ID {id} not found");
            return this.Success(item, "Payment term updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PaymentTermDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<PaymentTermDto>($"Failed to update payment term: {ex.Message}", 500);
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
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return StatusCode(404, ApiResponse.ErrorResponse($"Payment term with ID {id} not found"));
            return this.StatusCode(204, ApiResponse.SuccessResponse("Payment term deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete payment term: {ex.Message}"));
        }
    }
}
