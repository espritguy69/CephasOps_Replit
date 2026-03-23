using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Service for managing integration settings (MyInvois, SMS, WhatsApp)
/// </summary>
public interface IIntegrationSettingsService
{
    /// <summary>
    /// Get all integration settings
    /// </summary>
    Task<IntegrationSettingsDto> GetIntegrationSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update MyInvois settings
    /// </summary>
    Task UpdateMyInvoisSettingsAsync(MyInvoisSettingsDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update SMS settings
    /// </summary>
    Task UpdateSmsSettingsAsync(SmsSettingsDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update WhatsApp settings
    /// </summary>
    Task UpdateWhatsAppSettingsAsync(WhatsAppSettingsDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test MyInvois connection
    /// </summary>
    Task<bool> TestMyInvoisConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test SMS connection
    /// </summary>
    Task<bool> TestSmsConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test WhatsApp connection
    /// </summary>
    Task<bool> TestWhatsAppConnectionAsync(CancellationToken cancellationToken = default);
}

