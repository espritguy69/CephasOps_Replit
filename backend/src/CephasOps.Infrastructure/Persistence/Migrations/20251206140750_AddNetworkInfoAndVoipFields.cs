using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkInfoAndVoipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwoNumber",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone2",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkBandwidth",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkGateway",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkLanIp",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkLoginId",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkPackage",
                table: "Orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkPassword",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkSubnetMask",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkWanIp",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceIdType",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SplitterId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitterLocation",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitterNumber",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitterPort",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipGatewayOnu",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipIpAddressOnu",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipIpAddressSrp",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipPassword",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipRemarks",
                table: "Orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoipSubnetMaskOnu",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderMaterialReplacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldSerialisedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NewSerialisedItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacementReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReplacedBySiId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RmaRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMaterialReplacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderMaterialReplacements_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderNonSerialisedReplacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityReplaced = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReplacementReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReplacedBySiId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNonSerialisedReplacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderNonSerialisedReplacements_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_CompanyId_OrderId",
                table: "OrderMaterialReplacements",
                columns: new[] { "CompanyId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_NewMaterialId",
                table: "OrderMaterialReplacements",
                column: "NewMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_NewSerialisedItemId",
                table: "OrderMaterialReplacements",
                column: "NewSerialisedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_OldMaterialId",
                table: "OrderMaterialReplacements",
                column: "OldMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_OldSerialisedItemId",
                table: "OrderMaterialReplacements",
                column: "OldSerialisedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_OrderId",
                table: "OrderMaterialReplacements",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMaterialReplacements_RmaRequestId",
                table: "OrderMaterialReplacements",
                column: "RmaRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNonSerialisedReplacements_CompanyId_OrderId",
                table: "OrderNonSerialisedReplacements",
                columns: new[] { "CompanyId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderNonSerialisedReplacements_MaterialId",
                table: "OrderNonSerialisedReplacements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNonSerialisedReplacements_OrderId",
                table: "OrderNonSerialisedReplacements",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderMaterialReplacements");

            migrationBuilder.DropTable(
                name: "OrderNonSerialisedReplacements");

            migrationBuilder.DropColumn(
                name: "AwoNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkBandwidth",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkGateway",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkLanIp",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkLoginId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkPackage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkPassword",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkSubnetMask",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NetworkWanIp",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceIdType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SplitterId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SplitterLocation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SplitterNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SplitterPort",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipGatewayOnu",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipIpAddressOnu",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipIpAddressSrp",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipPassword",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipRemarks",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoipSubnetMaskOnu",
                table: "Orders");
        }
    }
}
