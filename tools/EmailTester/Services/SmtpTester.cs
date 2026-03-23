using System.Diagnostics;
using EmailTester.Models;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace EmailTester.Services;

public sealed class SmtpTester
{
    /// <summary>
    /// Runs an SMTP connectivity + authentication test using configuration from environment variables.
    ///
    /// Required environment variables:
    /// - SMTP_HOST
    /// - SMTP_PORT
    /// - SMTP_USER
    /// - SMTP_PASS
    /// - SMTP_SSL (true / false)
    ///
    /// Assumptions:
    /// - When SMTP_SSL=true, SSL/TLS is used immediately on connect (e.g. port 465).
    /// - When SMTP_SSL=false, STARTTLS is used if the server supports it (safe default for port 587).
    /// </summary>
    public async Task<TestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST");
        var portRaw = Environment.GetEnvironmentVariable("SMTP_PORT");
        var user = Environment.GetEnvironmentVariable("SMTP_USER");
        var pass = Environment.GetEnvironmentVariable("SMTP_PASS");
        var sslRaw = Environment.GetEnvironmentVariable("SMTP_SSL");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portRaw) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(pass))
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(host)) missing.Add("SMTP_HOST");
            if (string.IsNullOrWhiteSpace(portRaw)) missing.Add("SMTP_PORT");
            if (string.IsNullOrWhiteSpace(user)) missing.Add("SMTP_USER");
            if (string.IsNullOrWhiteSpace(pass)) missing.Add("SMTP_PASS");

            return TestResult.FailureResult(
                "SMTP configuration invalid",
                $"Missing required environment variables: {string.Join(", ", missing)}",
                0);
        }

        if (!int.TryParse(portRaw, out var port))
        {
            return TestResult.FailureResult(
                "SMTP configuration invalid",
                $"SMTP_PORT must be a valid integer but was '{portRaw}'.",
                0);
        }

        var useSsl = bool.TryParse(sslRaw, out var ssl) && ssl;

        // Safe defaults:
        // - SSLOnConnect when SMTP_SSL=true (e.g. port 465)
        // - StartTlsWhenAvailable when SMTP_SSL=false (e.g. port 587)
        var secureSocketOptions = useSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var smtp = new SmtpClient();

            // Note: disable OAuth2 mechanisms for plain user/pass testing.
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");

            await smtp.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
            await smtp.AuthenticateAsync(user, pass, cancellationToken);

            stopwatch.Stop();

            // No email is sent; we only validate connectivity + auth.

            await smtp.DisconnectAsync(true, cancellationToken);

            return TestResult.SuccessResult("SMTP connection successful", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return TestResult.FailureResult(
                "SMTP connection failed",
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }
}


