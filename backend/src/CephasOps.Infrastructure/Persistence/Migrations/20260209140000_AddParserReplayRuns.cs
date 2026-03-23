using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParserReplayRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParserReplayRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalParseSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldParseStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldConfidence = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NewParseStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NewConfidence = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OldMissingFields = table.Column<string>(type: "jsonb", nullable: true),
                    NewMissingFields = table.Column<string>(type: "jsonb", nullable: true),
                    OldSheetName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NewSheetName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OldHeaderRow = table.Column<int>(type: "integer", nullable: true),
                    NewHeaderRow = table.Column<int>(type: "integer", nullable: true),
                    RegressionDetected = table.Column<bool>(type: "boolean", nullable: false),
                    ImprovementDetected = table.Column<bool>(type: "boolean", nullable: false),
                    ResultSummary = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParserReplayRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParserReplayRuns_AttachmentId",
                table: "ParserReplayRuns",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParserReplayRuns_CreatedAt",
                table: "ParserReplayRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ParserReplayRuns_OriginalParseSessionId",
                table: "ParserReplayRuns",
                column: "OriginalParseSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParserReplayRuns_RegressionDetected_CreatedAt",
                table: "ParserReplayRuns",
                columns: new[] { "RegressionDetected", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParserReplayRuns");
        }
    }
}
