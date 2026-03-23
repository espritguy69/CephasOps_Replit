using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTypeToJobEarningRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns (nullable initially for data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "OrderTypeId",
                table: "JobEarningRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeCode",
                table: "JobEarningRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeName",
                table: "JobEarningRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Populate OrderTypeId, OrderTypeCode, and OrderTypeName from existing JobType
            // Match JobType (which contains OrderType.Name) to OrderType table
            migrationBuilder.Sql(@"
                UPDATE ""JobEarningRecords"" j
                SET 
                    ""OrderTypeId"" = ot.""Id"",
                    ""OrderTypeCode"" = ot.""Code"",
                    ""OrderTypeName"" = ot.""Name""
                FROM ""Orders"" o
                INNER JOIN ""OrderTypes"" ot ON o.""OrderTypeId"" = ot.""Id""
                WHERE j.""OrderId"" = o.""Id""
                  AND j.""OrderTypeId"" IS NULL;
            ");

            // For records where OrderId doesn't exist or OrderTypeId is still null,
            // try to match by JobType name (fallback)
            migrationBuilder.Sql(@"
                UPDATE ""JobEarningRecords"" j
                SET 
                    ""OrderTypeId"" = ot.""Id"",
                    ""OrderTypeCode"" = ot.""Code"",
                    ""OrderTypeName"" = ot.""Name""
                FROM ""OrderTypes"" ot
                WHERE j.""OrderTypeId"" IS NULL
                  AND LOWER(TRIM(ot.""Name"")) = LOWER(TRIM(j.""JobType""))
                LIMIT 1;
            ");

            // Make OrderTypeId required (after data migration)
            migrationBuilder.AlterColumn<Guid>(
                name: "OrderTypeId",
                table: "JobEarningRecords",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            // Make OrderTypeCode required
            migrationBuilder.AlterColumn<string>(
                name: "OrderTypeCode",
                table: "JobEarningRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Make OrderTypeName required
            migrationBuilder.AlterColumn<string>(
                name: "OrderTypeName",
                table: "JobEarningRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_JobEarningRecords_OrderTypes_OrderTypeId",
                table: "JobEarningRecords",
                column: "OrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add index
            migrationBuilder.CreateIndex(
                name: "IX_JobEarningRecords_OrderTypeId",
                table: "JobEarningRecords",
                column: "OrderTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobEarningRecords_OrderTypes_OrderTypeId",
                table: "JobEarningRecords");

            migrationBuilder.DropIndex(
                name: "IX_JobEarningRecords_OrderTypeId",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "OrderTypeId",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "OrderTypeCode",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "OrderTypeName",
                table: "JobEarningRecords");
        }
    }
}

