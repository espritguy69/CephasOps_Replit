using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerForeignKeyToMaterial_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Materials_PartnerId",
                table: "Materials",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Partners_PartnerId",
                table: "Materials",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Partners_PartnerId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_PartnerId",
                table: "Materials");
        }
    }
}
