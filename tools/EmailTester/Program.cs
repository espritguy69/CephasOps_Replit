using EmailTester.Services;

namespace EmailTester;

public static class Program
{
    /// <summary>
    /// Simple SMTP + POP3 connection tester.
    ///
    /// Usage (local):
    ///   SMTP_HOST=smtp.example.com SMTP_PORT=587 SMTP_USER=user SMTP_PASS=secret SMTP_SSL=false ^
    ///   POP3_HOST=pop3.example.com POP3_PORT=995 POP3_USER=user POP3_PASS=secret POP3_SSL=true ^
    ///   dotnet run --project tools/EmailTester
    ///
    /// Example GitHub Actions step (Linux runner):
    ///   - name: Test mail connectivity
    ///     run: dotnet run --project tools/EmailTester
    ///     env:
    ///       SMTP_HOST: ${{ secrets.SMTP_HOST }}
    ///       SMTP_PORT: "587"
    ///       SMTP_USER: ${{ secrets.SMTP_USER }}
    ///       SMTP_PASS: ${{ secrets.SMTP_PASS }}
    ///       SMTP_SSL: "false"
    ///       POP3_HOST: ${{ secrets.POP3_HOST }}
    ///       POP3_PORT: "995"
    ///       POP3_USER: ${{ secrets.POP3_USER }}
    ///       POP3_PASS: ${{ secrets.POP3_PASS }}
    ///       POP3_SSL: "true"
    ///
    /// Exit codes:
    ///   0 - both SMTP and POP3 tests succeed
    ///   1 - either SMTP or POP3 fails
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var smtpTester = new SmtpTester();
        var pop3Tester = new Pop3Tester();

        using var cts = new CancellationTokenSource();

        Console.WriteLine("Running SMTP and POP3 connection tests...\n");

        var smtpResult = await smtpTester.TestAsync(cts.Token);
        var pop3Result = await pop3Tester.TestAsync(cts.Token);

        // Overall status based on SMTP + POP3 tests
        if (smtpResult.Success && pop3Result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connection successful!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Connection failed!");
            Console.ResetColor();
        }

        // SMTP section
        Console.WriteLine();
        if (smtpResult.Success)
        {
            Console.WriteLine("SMTP connection successful");
            Console.WriteLine("Server is reachable and accepts connections");
            Console.WriteLine($"Response time: {smtpResult.ResponseTimeMs}ms");
        }
        else
        {
            Console.WriteLine("SMTP connection failed");
            if (!string.IsNullOrWhiteSpace(smtpResult.Error))
            {
                Console.WriteLine($"Error: {smtpResult.Error}");
            }
        }

        // POP3 section
        Console.WriteLine();
        if (pop3Result.Success)
        {
            Console.WriteLine("POP3 connection successful");
            Console.WriteLine("Authentication passed");
            Console.WriteLine($"Response time: {pop3Result.ResponseTimeMs}ms");
        }
        else
        {
            Console.WriteLine("POP3 connection failed");
            if (!string.IsNullOrWhiteSpace(pop3Result.Error))
            {
                Console.WriteLine($"Error: {pop3Result.Error}");
            }
        }

        Console.WriteLine();

        // Exit code: 0 only if both succeeded
        return smtpResult.Success && pop3Result.Success ? 0 : 1;
    }
}


