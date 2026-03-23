using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Email accounts / mailboxes management endpoints.
/// </summary>
[ApiController]
[Route("api/email-accounts")]
[Authorize]
public class EmailAccountsController : ControllerBase
{
    private readonly IEmailAccountService _emailAccountService;
    private readonly IEmailIngestionService _emailIngestionService;
    private readonly IEmailCleanupService _emailCleanupService;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EmailAccountsController> _logger;

    public EmailAccountsController(
        IEmailAccountService emailAccountService,
        IEmailIngestionService emailIngestionService,
        IEmailCleanupService emailCleanupService,
        IGlobalSettingsService globalSettingsService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<EmailAccountsController> logger)
    {
        _emailAccountService = emailAccountService;
        _emailIngestionService = emailIngestionService;
        _emailCleanupService = emailCleanupService;
        _globalSettingsService = globalSettingsService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailAccountDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailAccountDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailAccountDto>>>> GetEmailAccounts(
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var accounts = await _emailAccountService.GetEmailAccountsAsync(companyId, cancellationToken);
            return this.Success(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email accounts");
            return this.Error<List<EmailAccountDto>>($"Failed to get email accounts: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailAccountDto>>> GetEmailAccount(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var account = await _emailAccountService.GetEmailAccountByIdAsync(id, companyId, cancellationToken);
            if (account == null)
            {
                return this.NotFound<EmailAccountDto>($"Email account with ID {id} not found");
            }

            return this.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email account {EmailAccountId}", id);
            return this.Error<EmailAccountDto>($"Failed to get email account: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailAccountDto>>> CreateEmailAccount(
        [FromBody] CreateEmailAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            _logger.LogInformation("Creating email account: Name={Name}, Provider={Provider}, Host={Host}, Port={Port}",
                dto.Name, dto.Provider, dto.Host, dto.Port);
            
            var account = await _emailAccountService.CreateEmailAccountAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<EmailAccountDto>.SuccessResponse(account, "Email account created successfully"));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            _logger.LogError(dbEx, "Database error creating email account: {InnerError}", innerMessage);
            return this.Error<EmailAccountDto>($"Failed to create email account: {innerMessage}", 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email account");
            return this.Error<EmailAccountDto>($"Failed to create email account: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailAccountDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailAccountDto>>> UpdateEmailAccount(
        Guid id,
        [FromBody] UpdateEmailAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var account = await _emailAccountService.UpdateEmailAccountAsync(id, dto, companyId, cancellationToken);
            return this.Success(account, "Email account updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<EmailAccountDto>($"Email account with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email account {EmailAccountId}", id);
            return this.Error<EmailAccountDto>($"Failed to update email account: {ex.Message}", 500);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteEmailAccount(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _emailAccountService.DeleteEmailAccountAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Email account deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Email account with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email account {EmailAccountId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete email account: {ex.Message}"));
        }
    }

    [HttpPost("{id}/test-connection")]
    [ProducesResponseType(typeof(ApiResponse<EmailConnectionTestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailConnectionTestResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailConnectionTestResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailConnectionTestResultDto>>> TestConnection(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var result = await _emailAccountService.TestConnectionAsync(id, companyId, cancellationToken);
            return this.Success(result);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<EmailConnectionTestResultDto>($"Email account with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for email account {EmailAccountId}", id);
            return this.Error<EmailConnectionTestResultDto>($"Failed to test connection: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Manually trigger email polling for a specific mailbox
    /// </summary>
    [HttpPost("{id}/poll")]
    [ProducesResponseType(typeof(ApiResponse<EmailIngestionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailIngestionResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailIngestionResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailIngestionResultDto>>> PollEmails(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            _logger.LogInformation("Manual email poll triggered for account {AccountId} by user {UserId}", 
                id, _currentUserService.UserId);
            
            var result = await _emailIngestionService.TriggerPollAsync(id, companyId, cancellationToken);
            
            if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
            {
                return this.NotFound<EmailIngestionResultDto>(result.ErrorMessage ?? "Email account not found");
            }
            
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling emails for account {EmailAccountId}", id);
            return this.Error<EmailIngestionResultDto>($"Failed to poll emails: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Poll all active email accounts
    /// </summary>
    [HttpPost("poll-all")]
    [ProducesResponseType(typeof(ApiResponse<List<EmailIngestionResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailIngestionResultDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailIngestionResultDto>>>> PollAllEmails(
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — IngestAllEmailsAsync runs per authenticated context.
        try
        {
            _logger.LogInformation("Manual email poll (all accounts) triggered by user {UserId}", 
                _currentUserService.UserId);
            
            var results = await _emailIngestionService.IngestAllEmailsAsync(cancellationToken);
            return this.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling all email accounts");
            return this.Error<List<EmailIngestionResultDto>>($"Failed to poll emails: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Manually trigger email cleanup job
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(ApiResponse<EmailCleanupResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailCleanupResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailCleanupResult>>> TriggerCleanup(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual email cleanup triggered by user {UserId}", 
                _currentUserService.UserId);
            
            var result = await _emailCleanupService.CleanupExpiredEmailsAsync(cancellationToken);
            
            if (!result.Success)
            {
                return this.Error<EmailCleanupResult>(
                    result.ErrorMessage ?? "Email cleanup failed", 500);
            }
            
            return this.Success(result, 
                $"Email cleanup completed: {result.DeletedEmails} email(s), {result.DeletedAttachments} attachment(s), {result.DeletedBlobs} blob(s) deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering email cleanup");
            return this.Error<EmailCleanupResult>($"Failed to run email cleanup: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get email system settings (poll interval and retention period)
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<EmailSystemSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailSystemSettingsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailSystemSettingsDto>>> GetEmailSettings(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get from GlobalSettings with defaults
            var pollIntervalMinutes = await _globalSettingsService.GetValueAsync<int?>("EmailPollingIntervalMinutes", cancellationToken) ?? 15;
            var retentionHours = await _globalSettingsService.GetValueAsync<int?>("EmailRetentionHours", cancellationToken) ?? 48;

            var settings = new EmailSystemSettingsDto
            {
                PollIntervalMinutes = pollIntervalMinutes,
                RetentionHours = retentionHours
            };

            return this.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email system settings");
            return this.Error<EmailSystemSettingsDto>($"Failed to get email settings: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update email system settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<EmailSystemSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailSystemSettingsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailSystemSettingsDto>>> UpdateEmailSettings(
        [FromBody] UpdateEmailSystemSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;

            // Update or create EmailPollingIntervalMinutes
            var pollIntervalSetting = await _globalSettingsService.GetByKeyAsync("EmailPollingIntervalMinutes", cancellationToken);
            if (pollIntervalSetting != null)
            {
                await _globalSettingsService.UpdateAsync("EmailPollingIntervalMinutes", 
                    new CephasOps.Application.Settings.DTOs.UpdateGlobalSettingDto 
                    { 
                        Value = dto.PollIntervalMinutes.ToString() 
                    }, 
                    userId, 
                    cancellationToken);
            }
            else
            {
                await _globalSettingsService.CreateAsync(
                    new CephasOps.Application.Settings.DTOs.CreateGlobalSettingDto
                    {
                        Key = "EmailPollingIntervalMinutes",
                        Value = dto.PollIntervalMinutes.ToString(),
                        ValueType = "Int",
                        Module = "Email",
                        Description = "How often to poll email mailboxes (in minutes)"
                    },
                    userId,
                    cancellationToken);
            }

            // Update or create EmailRetentionHours
            var retentionSetting = await _globalSettingsService.GetByKeyAsync("EmailRetentionHours", cancellationToken);
            if (retentionSetting != null)
            {
                await _globalSettingsService.UpdateAsync("EmailRetentionHours",
                    new CephasOps.Application.Settings.DTOs.UpdateGlobalSettingDto
                    {
                        Value = dto.RetentionHours.ToString()
                    },
                    userId,
                    cancellationToken);
            }
            else
            {
                await _globalSettingsService.CreateAsync(
                    new CephasOps.Application.Settings.DTOs.CreateGlobalSettingDto
                    {
                        Key = "EmailRetentionHours",
                        Value = dto.RetentionHours.ToString(),
                        ValueType = "Int",
                        Module = "Email",
                        Description = "How long emails are kept before automatic cleanup (in hours)"
                    },
                    userId,
                    cancellationToken);
            }

            // Return updated settings
            var updatedSettings = new EmailSystemSettingsDto
            {
                PollIntervalMinutes = dto.PollIntervalMinutes,
                RetentionHours = dto.RetentionHours
            };

            _logger.LogInformation("Email system settings updated by user {UserId}: PollInterval={PollInterval}min, Retention={Retention}hrs",
                userId, dto.PollIntervalMinutes, dto.RetentionHours);

            return this.Success(updatedSettings, "Email system settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email system settings");
            return this.Error<EmailSystemSettingsDto>($"Failed to update email settings: {ex.Message}", 500);
        }
    }
}


