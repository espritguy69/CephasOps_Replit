using System.Diagnostics;
using EmailTester.Models;
using MailKit.Net.Pop3;
using MailKit.Security;

namespace EmailTester.Services;

public sealed class Pop3Tester
{
    /// <summary>
    /// Runs a POP3 connectivity + authentication test using configuration from environment variables.
    ///
    /// Required environment variables:
    /// - POP3_HOST
    /// - POP3_PORT
    /// - POP3_USER
    /// - POP3_PASS
    /// - POP3_SSL (true / false)
    ///
    /// Assumptions:
    /// - When POP3_SSL=true, SSL/TLS is used immediately on connect (e.g. port 995).
    /// - When POP3_SSL=false, STARTTLS is used if the server supports it (plain POP3 otherwise).
    /// </summary>
    public async Task<TestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var host = Environment.GetEnvironmentVariable("POP3_HOST");
        var portRaw = Environment.GetEnvironmentVariable("POP3_PORT");
        var user = Environment.GetEnvironmentVariable("POP3_USER");
        var pass = Environment.GetEnvironmentVariable("POP3_PASS");
        var sslRaw = Environment.GetEnvironmentVariable("POP3_SSL");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portRaw) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(pass))
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(host)) missing.Add("POP3_HOST");
            if (string.IsNullOrWhiteSpace(portRaw)) missing.Add("POP3_PORT");
            if (string.IsNullOrWhiteSpace(user)) missing.Add("POP3_USER");
            if (string.IsNullOrWhiteSpace(pass)) missing.Add("POP3_PASS");

            return TestResult.FailureResult(
                "POP3 configuration invalid",
                $"Missing required environment variables: {string.Join(", ", missing)}",
                0);
        }

        if (!int.TryParse(portRaw, out var port))
        {
            return TestResult.FailureResult(
                "POP3 configuration invalid",
                $"POP3_PORT must be a valid integer but was '{portRaw}'.",
                0);
        }

        var useSsl = bool.TryParse(sslRaw, out var ssl) && ssl;

        // Safe defaults:
        // - SslOnConnect when POP3_SSL=true (e.g. port 995)
        // - StartTlsWhenAvailable when POP3_SSL=false (e.g. port 110 with optional STARTTLS)
        var secureSocketOptions = useSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var pop3 = new Pop3Client();

            await pop3.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
            await pop3.AuthenticateAsync(user, pass, cancellationToken);

            stopwatch.Stop();

            // We only validate connectivity + auth; we don't fetch or delete messages.

            await pop3.DisconnectAsync(true, cancellationToken);

            return TestResult.SuccessResult("POP3 connection successful", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return TestResult.FailureResult(
                "POP3 connection failed",
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }
}


