using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OnuPassword",
                table: "ParsedOrderDrafts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnuPasswordEncrypted",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "JobEarningRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeCode",
                table: "JobEarningRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderTypeId",
                table: "JobEarningRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "OrderTypeName",
                table: "JobEarningRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_JobEarningRecords_OrderTypeId",
                table: "JobEarningRecords",
                column: "OrderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobEarningRecords_OrderTypes_OrderTypeId",
                table: "JobEarningRecords",
                column: "OrderTypeId",
                principalTable: "OrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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
                name: "OnuPassword",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "OnuPasswordEncrypted",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderTypeCode",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "OrderTypeId",
                table: "JobEarningRecords");

            migrationBuilder.DropColumn(
                name: "OrderTypeName",
                table: "JobEarningRecords");

            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "JobEarningRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
