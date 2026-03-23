using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateServiceInstallers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Update Orders.assignedSiId to point to the SI record we'll keep (oldest by CreatedAt)
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    duplicate_group RECORD;
                    keeper_id uuid;
                    duplicate_id uuid;
                BEGIN
                    -- For each group of duplicates (same Name + EmployeeId)
                    FOR duplicate_group IN
                        SELECT 
                            ""Name"",
                            ""EmployeeId"",
                            ARRAY_AGG(""Id"" ORDER BY ""CreatedAt"" ASC) as si_ids
                        FROM ""ServiceInstallers""
                        WHERE ""Name"" IS NOT NULL AND ""EmployeeId"" IS NOT NULL
                        GROUP BY ""Name"", ""EmployeeId""
                        HAVING COUNT(*) > 1
                    LOOP
                        -- The first ID (oldest) is the keeper
                        keeper_id := duplicate_group.si_ids[1];
                        
                        -- Update all orders assigned to any duplicate to point to the keeper
                        FOREACH duplicate_id IN ARRAY duplicate_group.si_ids[2:array_upper(duplicate_group.si_ids, 1)]
                        LOOP
                            UPDATE ""Orders""
                            SET ""AssignedSiId"" = keeper_id
                            WHERE ""AssignedSiId"" = duplicate_id;
                            
                            RAISE NOTICE 'Updated Orders from duplicate SI % to keeper SI %', duplicate_id, keeper_id;
                        END LOOP;
                    END LOOP;
                END $$;
            ");

            // Step 2: Delete ServiceInstallerContacts for duplicate SIs
            migrationBuilder.Sql(@"
                DELETE FROM ""ServiceInstallerContacts""
                WHERE ""ServiceInstallerId"" IN (
                    SELECT si.""Id""
                    FROM ""ServiceInstallers"" si
                    INNER JOIN (
                        SELECT 
                            ""Name"",
                            ""EmployeeId"",
                            MIN(""CreatedAt"") as keep_created_at
                        FROM ""ServiceInstallers""
                        WHERE ""Name"" IS NOT NULL AND ""EmployeeId"" IS NOT NULL
                        GROUP BY ""Name"", ""EmployeeId""
                        HAVING COUNT(*) > 1
                    ) dupes ON si.""Name"" = dupes.""Name"" 
                           AND si.""EmployeeId"" = dupes.""EmployeeId""
                           AND si.""CreatedAt"" > dupes.keep_created_at
                );
            ");

            // Step 3: Delete duplicate ServiceInstallers (keep oldest by CreatedAt)
            migrationBuilder.Sql(@"
                DELETE FROM ""ServiceInstallers""
                WHERE ""Id"" IN (
                    SELECT si.""Id""
                    FROM ""ServiceInstallers"" si
                    INNER JOIN (
                        SELECT 
                            ""Name"",
                            ""EmployeeId"",
                            MIN(""CreatedAt"") as keep_created_at
                        FROM ""ServiceInstallers""
                        WHERE ""Name"" IS NOT NULL AND ""EmployeeId"" IS NOT NULL
                        GROUP BY ""Name"", ""EmployeeId""
                        HAVING COUNT(*) > 1
                    ) dupes ON si.""Name"" = dupes.""Name"" 
                           AND si.""EmployeeId"" = dupes.""EmployeeId""
                           AND si.""CreatedAt"" > dupes.keep_created_at
                );
            ");

            // Step 4: Create unique index to prevent future duplicates
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ServiceInstallers_CompanyId_Name_EmployeeId"" 
                ON ""ServiceInstallers"" (
                    COALESCE(""CompanyId"", '00000000-0000-0000-0000-000000000000'),
                    ""Name"", 
                    COALESCE(""EmployeeId"", '')
                )
                WHERE ""Name"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_ServiceInstallers_CompanyId_Name_EmployeeId"";
            ");
            
            // Note: We cannot restore deleted duplicates in the Down migration
            // This is a data cleanup operation that should not be reversed
        }
    }
}

