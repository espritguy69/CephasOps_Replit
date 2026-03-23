using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceInstallerData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove all Service Installer data to prepare for fresh import
            // This migration handles foreign key dependencies properly

            // Step 1: Set nullable foreign keys to NULL
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" 
                SET ""AssignedSiId"" = NULL 
                WHERE ""AssignedSiId"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""StockMovements"" 
                SET ""ServiceInstallerId"" = NULL 
                WHERE ""ServiceInstallerId"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""StockLocations"" 
                SET ""LinkedServiceInstallerId"" = NULL 
                WHERE ""LinkedServiceInstallerId"" IS NOT NULL;
            ");

            // Step 2: Delete dependent records that require ServiceInstaller
            // (These cannot have NULL ServiceInstallerId, so must be deleted)
            // Using IF EXISTS to handle cases where tables might not exist yet
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'ScheduledSlots') THEN
                        DELETE FROM ""ScheduledSlots"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiAvailabilities') THEN
                        DELETE FROM ""SiAvailabilities"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiLeaveRequests') THEN
                        DELETE FROM ""SiLeaveRequests"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'SiRatePlans') THEN
                        DELETE FROM ""SiRatePlans"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'GponSiCustomRates') THEN
                        DELETE FROM ""GponSiCustomRates"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'JobEarningRecords') THEN
                        DELETE FROM ""JobEarningRecords"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PayrollLines') THEN
                        DELETE FROM ""PayrollLines"";
                    END IF;
                    
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PnlDetailPerOrders') THEN
                        DELETE FROM ""PnlDetailPerOrders"";
                    END IF;
                END $$;
            ");

            // Step 3: Delete ServiceInstallerContacts (will auto-delete via CASCADE, but explicit for clarity)
            migrationBuilder.Sql(@"DELETE FROM ""ServiceInstallerContacts"";");

            // Step 4: Delete all ServiceInstallers
            migrationBuilder.Sql(@"DELETE FROM ""ServiceInstallers"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
