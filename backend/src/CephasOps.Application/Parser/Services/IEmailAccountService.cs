using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

public interface IEmailAccountService
{
    Task<List<EmailAccountDto>> GetEmailAccountsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<EmailAccountDto?> GetEmailAccountByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<EmailAccountDto> CreateEmailAccountAsync(CreateEmailAccountDto dto, Guid companyId, CancellationToken cancellationToken = default);
    Task<EmailAccountDto> UpdateEmailAccountAsync(Guid id, UpdateEmailAccountDto dto, Guid companyId, CancellationToken cancellationToken = default);
    Task DeleteEmailAccountAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<EmailConnectionTestResultDto> TestConnectionAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}


