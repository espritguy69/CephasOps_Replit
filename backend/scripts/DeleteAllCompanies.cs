using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CephasOps.Scripts;

/// <summary>
/// Simple console application to delete all companies
/// Usage: dotnet run --project DeleteAllCompanies.cs
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("============================================");
        Console.WriteLine("  Delete All Companies");
        Console.WriteLine("============================================");
        Console.WriteLine();

        // Confirm deletion
        Console.WriteLine("WARNING: This will delete ALL companies from the database!");
        Console.WriteLine();
        Console.Write("Type 'YES' to confirm: ");
        var confirmation = Console.ReadLine();
        
        if (confirmation?.Trim() != "YES")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        Console.WriteLine();

        // Get connection string from appsettings
        var apiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "CephasOps.Api");
        var appsettingsPath = Path.Combine(apiPath, "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            Console.WriteLine($"ERROR: appsettings.json not found at {appsettingsPath}");
            return;
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true);

        var configuration = builder.Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: Connection string not found in appsettings.json");
            return;
        }

        // Create DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        using var context = new ApplicationDbContext(optionsBuilder.Options);

        // Get all companies
        var companies = await context.Companies.ToListAsync();
        var count = companies.Count;

        if (count == 0)
        {
            Console.WriteLine("No companies found to delete.");
            return;
        }

        Console.WriteLine($"Found {count} companies:");
        foreach (var company in companies)
        {
            Console.WriteLine($"  - {company.LegalName} ({company.ShortName}) - ID: {company.Id}");
        }

        Console.WriteLine();
        Console.Write("Deleting all companies... ");

        try
        {
            context.Companies.RemoveRange(companies);
            var deleted = await context.SaveChangesAsync();

            Console.WriteLine("DONE");
            Console.WriteLine();
            Console.WriteLine($"Successfully deleted {deleted} companies.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAILED");
            Console.WriteLine();
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

