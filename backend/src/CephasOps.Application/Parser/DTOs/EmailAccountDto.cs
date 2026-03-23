namespace CephasOps.Application.Parser.DTOs;

public class EmailAccountDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public int PollIntervalSec { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastPolledAt { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public string? DefaultDepartmentName { get; set; }
    public Guid? DefaultParserTemplateId { get; set; }
    public string? DefaultParserTemplateName { get; set; }

    // SMTP
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public bool SmtpUseSsl { get; set; }
    public bool SmtpUseTls { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
}

/// <summary>
/// DTO for email system settings (poll interval and retention period)
/// </summary>
public class EmailSystemSettingsDto
{
    public int PollIntervalMinutes { get; set; }
    public int RetentionHours { get; set; }
}

/// <summary>
/// DTO for updating email system settings
/// </summary>
public class UpdateEmailSystemSettingsDto
{
    public int PollIntervalMinutes { get; set; }
    public int RetentionHours { get; set; }
}

public class CreateEmailAccountDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "POP3";
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int PollIntervalSec { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public Guid? DefaultDepartmentId { get; set; }
    public Guid? DefaultParserTemplateId { get; set; }

    // SMTP
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; }
    public bool SmtpUseTls { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
}

public class UpdateEmailAccountDto
{
    public string? Name { get; set; }
    public string? Provider { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool? UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? PollIntervalSec { get; set; }
    public bool? IsActive { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public Guid? DefaultParserTemplateId { get; set; }

    // SMTP
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool? SmtpUseSsl { get; set; }
    public bool? SmtpUseTls { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
}
