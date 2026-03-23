using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingNameAndStatusToOrderDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ParseSessions",
                type: "bytea",
                rowVersion: true,
                nullable: true,
                defaultValueSql: "gen_random_bytes(8)",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ParsedOrderDrafts",
                type: "bytea",
                rowVersion: true,
                nullable: true,
                defaultValueSql: "gen_random_bytes(8)",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingName",
                table: "ParsedOrderDrafts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingStatus",
                table: "ParsedOrderDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "EmailMessages",
                type: "bytea",
                rowVersion: true,
                nullable: true,
                defaultValueSql: "gen_random_bytes(8)",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingName",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "BuildingStatus",
                table: "ParsedOrderDrafts");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ParseSessions",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true,
                oldNullable: true,
                oldDefaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "ParsedOrderDrafts",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true,
                oldNullable: true,
                oldDefaultValueSql: "gen_random_bytes(8)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "EmailMessages",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true,
                oldNullable: true,
                oldDefaultValueSql: "gen_random_bytes(8)");
        }
    }
}
