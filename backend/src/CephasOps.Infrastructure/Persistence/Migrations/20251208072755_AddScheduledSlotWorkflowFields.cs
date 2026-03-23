using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledSlotWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add confirmation and posting tracking fields (only if they don't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'ConfirmedByUserId'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""ConfirmedByUserId"" uuid NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'ConfirmedAt'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""ConfirmedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'PostedByUserId'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""PostedByUserId"" uuid NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'PostedAt'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""PostedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            // Add reschedule request fields (only if they don't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedDate'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleRequestedDate"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedTime'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleRequestedTime"" interval NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleReason'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleReason"" character varying(500) NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleNotes'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleNotes"" character varying(1000) NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedBySiId'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleRequestedBySiId"" uuid NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ScheduledSlots' AND column_name = 'RescheduleRequestedAt'
                    ) THEN
                        ALTER TABLE ""ScheduledSlots"" ADD COLUMN ""RescheduleRequestedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            // Update existing records: Change Status from "Planned" to "Draft" (new default)
            migrationBuilder.Sql(@"
                UPDATE ""ScheduledSlots""
                SET ""Status"" = 'Draft'
                WHERE ""Status"" = 'Planned' OR ""Status"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Status changes (optional - may lose data)
            migrationBuilder.Sql(@"
                UPDATE ""ScheduledSlots""
                SET ""Status"" = 'Planned'
                WHERE ""Status"" = 'Draft';
            ");

            // Remove reschedule request fields
            migrationBuilder.DropColumn(
                name: "RescheduleRequestedAt",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RescheduleRequestedBySiId",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RescheduleNotes",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RescheduleReason",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RescheduleRequestedTime",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "RescheduleRequestedDate",
                table: "ScheduledSlots");

            // Remove confirmation and posting tracking fields
            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "PostedByUserId",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "ScheduledSlots");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "ScheduledSlots");
        }
    }
}
