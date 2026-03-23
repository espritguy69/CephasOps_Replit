using CephasOps.Application.Parser.DTOs;
using System.Diagnostics;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CephasOps.Application.Parser.Services;

public class EmailAccountService : IEmailAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailAccountService> _logger;
    private readonly IEncryptionService _encryptionService;

    public EmailAccountService(
        ApplicationDbContext context, 
        ILogger<EmailAccountService> logger,
        IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<List<EmailAccountDto>> GetEmailAccountsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Getting email accounts for company {CompanyId}", companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var accounts = await _context.EmailAccounts
            .Where(ea => ea.CompanyId == companyId)
            .OrderBy(ea => ea.Name)
            .ToListAsync(cancellationToken);

        // Get department names for accounts that have DefaultDepartmentId
        var departmentIds = accounts
            .Where(a => a.DefaultDepartmentId.HasValue)
            .Select(a => a.DefaultDepartmentId!.Value)
            .Distinct()
            .ToList();
        var templateIds = accounts
            .Where(a => a.DefaultParserTemplateId.HasValue)
            .Select(a => a.DefaultParserTemplateId!.Value)
            .Distinct()
            .ToList();

        var departments = departmentIds.Count > 0
            ? await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken)
            : new Dictionary<Guid, string>();
        var templates = templateIds.Count > 0
            ? await _context.ParserTemplates
                .Where(t => templateIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return accounts.Select(a => MapToDto(a, 
            a.DefaultDepartmentId.HasValue && departments.ContainsKey(a.DefaultDepartmentId.Value) 
                ? departments[a.DefaultDepartmentId.Value] 
                : null,
            a.DefaultParserTemplateId.HasValue && templates.ContainsKey(a.DefaultParserTemplateId.Value)
                ? templates[a.DefaultParserTemplateId.Value]
                : null)).ToList();
    }

    public async Task<EmailAccountDto?> GetEmailAccountByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Getting email account {EmailAccountId} for company {CompanyId}", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var account = await _context.EmailAccounts
            .FirstOrDefaultAsync(ea => ea.Id == id && ea.CompanyId == companyId, cancellationToken);

        if (account == null) return null;

        string? departmentName = null;
        if (account.DefaultDepartmentId.HasValue)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == account.DefaultDepartmentId.Value, cancellationToken);
            departmentName = dept?.Name;
        }

        string? parserTemplateName = null;
        if (account.DefaultParserTemplateId.HasValue)
        {
            var template = await _context.ParserTemplates
                .FirstOrDefaultAsync(t => t.Id == account.DefaultParserTemplateId.Value, cancellationToken);
            parserTemplateName = template?.Name;
        }

        return MapToDto(account, departmentName, parserTemplateName);
    }

    public async Task<EmailAccountDto> CreateEmailAccountAsync(CreateEmailAccountDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating email account for company {CompanyId}: Name={Name}, Provider={Provider}, Host={Host}",
            companyId, dto.Name, dto.Provider, dto.Host);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Email account name is required");
        }
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            throw new ArgumentException("Username is required");
        }
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required");
        }

        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var account = new EmailAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Provider = dto.Provider ?? "POP3",
            Host = dto.Host?.Trim(),
            Port = dto.Port,
            UseSsl = dto.UseSsl,
            Username = dto.Username.Trim(),
            Password = EncryptPassword(dto.Password),
            PollIntervalSec = dto.PollIntervalSec > 0 ? dto.PollIntervalSec : 60,
            IsActive = dto.IsActive,
            LastPolledAt = null,
            DefaultDepartmentId = NormalizeOptionalGuid(dto.DefaultDepartmentId),
            DefaultParserTemplateId = NormalizeOptionalGuid(dto.DefaultParserTemplateId),
            SmtpHost = dto.SmtpHost?.Trim(),
            SmtpPort = dto.SmtpPort,
            SmtpUsername = dto.SmtpUsername?.Trim(),
            SmtpPassword = !string.IsNullOrWhiteSpace(dto.SmtpPassword) ? EncryptPassword(dto.SmtpPassword) : null,
            SmtpUseSsl = dto.SmtpUseSsl,
            SmtpUseTls = dto.SmtpUseTls,
            SmtpFromAddress = dto.SmtpFromAddress?.Trim(),
            SmtpFromName = dto.SmtpFromName?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogDebug("Email account entity created: Id={Id}, DefaultDeptId={DeptId}, DefaultTemplateId={TemplateId}",
            account.Id, account.DefaultDepartmentId, account.DefaultParserTemplateId);

        _context.EmailAccounts.Add(account);
        
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Email account created successfully: Id={Id}, Name={Name}", account.Id, account.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email account: {Error}", ex.InnerException?.Message ?? ex.Message);
            throw;
        }

        return MapToDto(account);
    }

    public async Task<EmailAccountDto> UpdateEmailAccountAsync(Guid id, UpdateEmailAccountDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Updating email account {EmailAccountId} for company {CompanyId}", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var account = await _context.EmailAccounts
            .FirstOrDefaultAsync(ea => ea.Id == id && ea.CompanyId == companyId, cancellationToken);

        if (account == null)
        {
            throw new KeyNotFoundException($"Email account with ID {id} not found");
        }

        if (dto.Name != null) account.Name = dto.Name;
        if (dto.Provider != null) account.Provider = dto.Provider;
        if (dto.Host != null) account.Host = dto.Host;
        if (dto.Port.HasValue) account.Port = dto.Port;
        if (dto.UseSsl.HasValue) account.UseSsl = dto.UseSsl.Value;
        if (dto.Username != null) account.Username = dto.Username;
        if (dto.Password != null) account.Password = EncryptPassword(dto.Password);
        if (dto.PollIntervalSec.HasValue) account.PollIntervalSec = dto.PollIntervalSec.Value;
        if (dto.IsActive.HasValue) account.IsActive = dto.IsActive.Value;
        if (dto.DefaultDepartmentId.HasValue) account.DefaultDepartmentId = NormalizeOptionalGuid(dto.DefaultDepartmentId);
        if (dto.DefaultParserTemplateId.HasValue) account.DefaultParserTemplateId = NormalizeOptionalGuid(dto.DefaultParserTemplateId);

        if (dto.SmtpHost != null) account.SmtpHost = dto.SmtpHost;
        if (dto.SmtpPort.HasValue) account.SmtpPort = dto.SmtpPort;
        if (dto.SmtpUsername != null) account.SmtpUsername = dto.SmtpUsername;
        if (dto.SmtpPassword != null) account.SmtpPassword = EncryptPassword(dto.SmtpPassword);
        if (dto.SmtpUseSsl.HasValue) account.SmtpUseSsl = dto.SmtpUseSsl.Value;
        if (dto.SmtpUseTls.HasValue) account.SmtpUseTls = dto.SmtpUseTls.Value;
        if (dto.SmtpFromAddress != null) account.SmtpFromAddress = dto.SmtpFromAddress;
        if (dto.SmtpFromName != null) account.SmtpFromName = dto.SmtpFromName;

        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(account);
    }

    public async Task DeleteEmailAccountAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Deleting email account {EmailAccountId} for company {CompanyId}", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var account = await _context.EmailAccounts
            .FirstOrDefaultAsync(ea => ea.Id == id && ea.CompanyId == companyId, cancellationToken);

        if (account == null)
        {
            throw new KeyNotFoundException($"Email account with ID {id} not found");
        }

        _context.EmailAccounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailConnectionTestResultDto> TestConnectionAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Testing connection for email account {EmailAccountId} (company {CompanyId})", id, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var account = await _context.EmailAccounts
            .FirstOrDefaultAsync(ea => ea.Id == id && ea.CompanyId == companyId, cancellationToken);

        if (account == null)
        {
            throw new KeyNotFoundException($"Email account with ID {id} not found");
        }

        var result = new EmailConnectionTestResultDto
        {
            IncomingProtocol = account.Provider?.ToUpperInvariant() ?? "POP3"
        };

        // Validate basic incoming configuration
        if (string.IsNullOrWhiteSpace(account.Host) || !account.Port.HasValue)
        {
            result.Success = false;
            result.Message = "Incoming mail host/port not configured for this email account.";
            result.IncomingSuccess = false;
            result.IncomingError = "Incoming mail host/port not configured for this email account.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.Password))
        {
            result.Success = false;
            result.Message = "Incoming mail credentials (username/password) are not configured for this email account.";
            result.IncomingSuccess = false;
            result.IncomingError = "Incoming mail credentials (username/password) are not configured for this email account.";
            return result;
        }

        // Decrypt password for authentication
        var decryptedPassword = DecryptPassword(account.Password);

        // -----------------------------
        // 1. Test INBOUND (POP3 / IMAP)
        // -----------------------------
        var inboundStopwatch = Stopwatch.StartNew();

        var provider = account.Provider?.ToUpperInvariant() ?? "POP3";

        // For POP3/IMAP we use SSL on connect when UseSsl=true, otherwise STARTTLS when available.
        var inboundSecureSocketOptions = account.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        try
        {
            switch (provider)
            {
                case "IMAP":
                    using (var imapClient = new ImapClient())
                    {
                        await imapClient.ConnectAsync(account.Host, account.Port.Value, inboundSecureSocketOptions, cancellationToken);
                        await imapClient.AuthenticateAsync(account.Username, decryptedPassword, cancellationToken);

                        // Optional sanity check
                        _ = imapClient.PersonalNamespaces;

                        await imapClient.DisconnectAsync(true, cancellationToken);
                    }
                    break;

                case "POP3":
                default:
                    using (var pop3Client = new Pop3Client())
                    {
                        await pop3Client.ConnectAsync(account.Host, account.Port.Value, inboundSecureSocketOptions, cancellationToken);
                        await pop3Client.AuthenticateAsync(account.Username, decryptedPassword, cancellationToken);

                        // Optional: ensure we can list messages
                        _ = pop3Client.Count;

                        await pop3Client.DisconnectAsync(true, cancellationToken);
                    }
                    break;
            }

            inboundStopwatch.Stop();
            result.IncomingSuccess = true;
            result.IncomingResponseTimeMs = inboundStopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            inboundStopwatch.Stop();
            result.IncomingSuccess = false;
            result.IncomingResponseTimeMs = inboundStopwatch.ElapsedMilliseconds;
            result.IncomingError = ex.Message;
        }

        // -----------------------------
        // 2. Test OUTBOUND (SMTP)
        // -----------------------------
        var smtpStopwatch = Stopwatch.StartNew();

        if (!string.IsNullOrWhiteSpace(account.SmtpHost) && account.SmtpPort.HasValue)
        {
            try
            {
                var smtpSecureSocketOptions = account.SmtpUseSsl
                    ? SecureSocketOptions.SslOnConnect // e.g. port 465
                    : account.SmtpUseTls
                        ? SecureSocketOptions.StartTls // e.g. port 587
                        : SecureSocketOptions.Auto;

                using var smtpClient = new SmtpClient();

                await smtpClient.ConnectAsync(account.SmtpHost, account.SmtpPort.Value, smtpSecureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(account.SmtpUsername))
                {
                    var decryptedSmtpPassword = !string.IsNullOrWhiteSpace(account.SmtpPassword) 
                        ? DecryptPassword(account.SmtpPassword) 
                        : string.Empty;
                    await smtpClient.AuthenticateAsync(
                        account.SmtpUsername,
                        decryptedSmtpPassword,
                        cancellationToken);
                }

                // We intentionally DO NOT send a real email here; connectivity + auth only.

                await smtpClient.DisconnectAsync(true, cancellationToken);

                smtpStopwatch.Stop();
                result.SmtpSuccess = true;
                result.SmtpResponseTimeMs = smtpStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                smtpStopwatch.Stop();
                result.SmtpSuccess = false;
                result.SmtpResponseTimeMs = smtpStopwatch.ElapsedMilliseconds;
                result.SmtpError = ex.Message;
            }
        }
        else
        {
            smtpStopwatch.Stop();
            result.SmtpSuccess = false;
            result.SmtpResponseTimeMs = 0;
            result.SmtpError = "SMTP settings are not configured for this email account.";
        }

        result.Success = result.IncomingSuccess && result.SmtpSuccess;
        result.Message = result.Success
            ? "Connection test successful!"
            : "Connection test failed.";

        return result;
    }

    private static EmailAccountDto MapToDto(EmailAccount account, string? departmentName = null, string? parserTemplateName = null)
    {
        return new EmailAccountDto
        {
            Id = account.Id,
            CompanyId = account.CompanyId,
            Name = account.Name,
            Provider = account.Provider,
            Host = account.Host,
            Port = account.Port,
            UseSsl = account.UseSsl,
            Username = account.Username,
            PollIntervalSec = account.PollIntervalSec,
            IsActive = account.IsActive,
            LastPolledAt = account.LastPolledAt,
            DefaultDepartmentId = account.DefaultDepartmentId,
            DefaultDepartmentName = departmentName,
            DefaultParserTemplateId = account.DefaultParserTemplateId,
            DefaultParserTemplateName = parserTemplateName,
            SmtpHost = account.SmtpHost,
            SmtpPort = account.SmtpPort,
            SmtpUsername = account.SmtpUsername,
            SmtpUseSsl = account.SmtpUseSsl,
            SmtpUseTls = account.SmtpUseTls,
            SmtpFromAddress = account.SmtpFromAddress,
            SmtpFromName = account.SmtpFromName
        };
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        if (!value.HasValue || value.Value == Guid.Empty)
        {
            return null;
        }

        return value;
    }

    /// <summary>
    /// Encrypts a password before storing in database
    /// </summary>
    private string EncryptPassword(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            return string.Empty;

        // Check if already encrypted (base64 format check)
        // Encrypted passwords are base64 strings, typically longer and contain specific characters
        if (IsEncrypted(plainTextPassword))
        {
            _logger.LogDebug("Password appears to be already encrypted, skipping encryption");
            return plainTextPassword;
        }

        try
        {
            return _encryptionService.Encrypt(plainTextPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt password, storing as plain text (security risk)");
            return plainTextPassword; // Fallback to plain text if encryption fails
        }
    }

    /// <summary>
    /// Decrypts a password when needed for authentication
    /// Handles backward compatibility with plain text passwords
    /// </summary>
    private string DecryptPassword(string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
            return string.Empty;

        // Check if password is encrypted
        if (!IsEncrypted(storedPassword))
        {
            _logger.LogDebug("Password appears to be plain text, returning as-is (backward compatibility)");
            return storedPassword; // Plain text password (backward compatibility)
        }

        try
        {
            var decrypted = _encryptionService.Decrypt(storedPassword);
            if (string.IsNullOrEmpty(decrypted))
            {
                _logger.LogWarning("Decryption returned empty string, using stored password as fallback");
                return storedPassword; // Fallback if decryption fails
            }
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt password, using stored value as fallback");
            return storedPassword; // Fallback to stored value if decryption fails
        }
    }

    /// <summary>
    /// Checks if a password string appears to be encrypted
    /// Encrypted passwords are base64 encoded and typically longer
    /// </summary>
    private static bool IsEncrypted(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Base64 strings are typically longer and contain only base64 characters
        // Plain text passwords are usually shorter and may contain spaces/special chars
        if (password.Length < 20)
            return false; // Too short to be encrypted

        // Check if it's valid base64 (encrypted passwords are base64)
        try
        {
            Convert.FromBase64String(password);
            // If it's valid base64 and reasonably long, assume it's encrypted
            return password.Length >= 20;
        }
        catch
        {
            // Not valid base64, likely plain text
            return false;
        }
    }
}


