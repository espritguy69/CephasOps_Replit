using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds TermsInDays column to Invoices for automatic DueDate calculation (Net 45 days).
/// </summary>
public partial class AddTermsInDaysToInvoice : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TermsInDays",
            table: "Invoices",
            type: "integer",
            nullable: false,
            defaultValue: 45);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TermsInDays",
            table: "Invoices");
    }
}
