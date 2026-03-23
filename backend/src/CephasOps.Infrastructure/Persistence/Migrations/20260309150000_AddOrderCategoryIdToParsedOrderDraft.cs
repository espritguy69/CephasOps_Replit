using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20260309150000_AddOrderCategoryIdToParsedOrderDraft")]
    public partial class AddOrderCategoryIdToParsedOrderDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderCategoryId",
                table: "ParsedOrderDrafts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderCategoryId",
                table: "ParsedOrderDrafts");
        }
    }
}
