using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameInstallationTypeToOrderCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create OrderCategories table and copy data from InstallationTypes
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""OrderCategories"" (
                    ""Id"" uuid NOT NULL,
                    ""DepartmentId"" uuid NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Code"" character varying(50) NOT NULL,
                    ""Description"" character varying(500) NULL,
                    ""IsActive"" boolean NOT NULL,
                    ""DisplayOrder"" integer NOT NULL,
                    ""CompanyId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    ""IsDeleted"" boolean NOT NULL,
                    ""DeletedAt"" timestamp with time zone NULL,
                    ""DeletedByUserId"" uuid NULL,
                    ""RowVersion"" bytea NULL,
                    CONSTRAINT ""PK_OrderCategories"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_OrderCategories_Departments_DepartmentId"" FOREIGN KEY (""DepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL
                );

                -- Copy data from InstallationTypes to OrderCategories
                INSERT INTO ""OrderCategories"" (
                    ""Id"", ""DepartmentId"", ""Name"", ""Code"", ""Description"", ""IsActive"", 
                    ""DisplayOrder"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", 
                    ""DeletedAt"", ""DeletedByUserId"", ""RowVersion""
                )
                SELECT 
                    ""Id"", ""DepartmentId"", ""Name"", ""Code"", ""Description"", ""IsActive"", 
                    ""DisplayOrder"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", 
                    ""DeletedAt"", ""DeletedByUserId"", ""RowVersion""
                FROM ""InstallationTypes"";
            ");

            // Step 2: Create indexes on OrderCategories
            migrationBuilder.CreateIndex(
                name: "IX_OrderCategories_CompanyId_Code",
                table: "OrderCategories",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategories_CompanyId_DepartmentId",
                table: "OrderCategories",
                columns: new[] { "CompanyId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategories_CompanyId_IsActive",
                table: "OrderCategories",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCategories_DepartmentId",
                table: "OrderCategories",
                column: "DepartmentId");

            // Step 3: Drop foreign keys that reference InstallationTypes
            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_BuildingTypes_BuildingTypeId",
                table: "Buildings");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_InstallationTypes_InstallationTypeId",
                table: "Orders");

            // Step 4: Drop BuildingTypeId column and index (obsolete)
            migrationBuilder.DropIndex(
                name: "IX_Buildings_BuildingTypeId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "BuildingTypeId",
                table: "Buildings");

            // Step 5: Rename columns (preserves data)
            migrationBuilder.RenameColumn(
                name: "InstallationType",
                table: "PnlDetailPerOrders",
                newName: "OrderCategory");

            migrationBuilder.RenameColumn(
                name: "InstallationTypeId",
                table: "Orders",
                newName: "OrderCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId",
                table: "Orders",
                newName: "IX_Orders_OrderTypeId_OrderCategoryId_InstallationMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_InstallationTypeId",
                table: "Orders",
                newName: "IX_Orders_OrderCategoryId");

            migrationBuilder.RenameColumn(
                name: "InstallationTypeId",
                table: "GponSiJobRates",
                newName: "OrderCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_InstallationTypeId",
                table: "GponSiJobRates",
                newName: "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_OrderCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiJobRates_OrderTypeId_InstallationTypeId_InstallationM~",
                table: "GponSiJobRates",
                newName: "IX_GponSiJobRates_OrderTypeId_OrderCategoryId_InstallationMeth~");

            migrationBuilder.RenameColumn(
                name: "InstallationTypeId",
                table: "GponSiCustomRates",
                newName: "OrderCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_Installati~",
                table: "GponSiCustomRates",
                newName: "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_OrderCateg~");

            migrationBuilder.RenameColumn(
                name: "InstallationTypeId",
                table: "GponPartnerJobRates",
                newName: "OrderCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_GponPartnerJobRates_PartnerId_OrderTypeId_InstallationTypeI~",
                table: "GponPartnerJobRates",
                newName: "IX_GponPartnerJobRates_PartnerId_OrderTypeId_OrderCategoryId_I~");

            migrationBuilder.RenameIndex(
                name: "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_Installation~",
                table: "GponPartnerJobRates",
                newName: "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_OrderCategor~");

            // Step 6: Add new columns for Issue/Solution (only if they don't exist)
            // Note: These columns may have been added by manual SQL migration 20251219000000_AddIssueAndSolutionToOrders.sql
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Add AdditionalContactNumber to ParsedOrderDrafts if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'AdditionalContactNumber'
                    ) THEN
                        ALTER TABLE ""ParsedOrderDrafts"" 
                        ADD COLUMN ""AdditionalContactNumber"" character varying(100) NULL;
                    END IF;

                    -- Add Issue to ParsedOrderDrafts if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'Issue'
                    ) THEN
                        ALTER TABLE ""ParsedOrderDrafts"" 
                        ADD COLUMN ""Issue"" character varying(1000) NULL;
                    END IF;

                    -- Add Issue to Orders if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'Issue'
                    ) THEN
                        ALTER TABLE ""Orders"" 
                        ADD COLUMN ""Issue"" character varying(1000) NULL;
                    END IF;

                    -- Add Solution to Orders if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'Solution'
                    ) THEN
                        ALTER TABLE ""Orders"" 
                        ADD COLUMN ""Solution"" character varying(2000) NULL;
                    END IF;
                END $$;
            ");

            // Step 7: Create index for InstallationMethodId on Buildings (only if it doesn't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE schemaname = 'public' 
                        AND tablename = 'Buildings' 
                        AND indexname = 'IX_Buildings_InstallationMethodId'
                    ) THEN
                        CREATE INDEX ""IX_Buildings_InstallationMethodId"" ON ""Buildings"" (""InstallationMethodId"");
                    END IF;
                END $$;
            ");

            // Step 8: Add foreign keys (only if they don't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Add FK_Orders_OrderCategories_OrderCategoryId if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Orders_OrderCategories_OrderCategoryId'
                        AND table_name = 'Orders'
                    ) THEN
                        ALTER TABLE ""Orders""
                        ADD CONSTRAINT ""FK_Orders_OrderCategories_OrderCategoryId""
                        FOREIGN KEY (""OrderCategoryId"")
                        REFERENCES ""OrderCategories"" (""Id"")
                        ON DELETE SET NULL;
                    END IF;

                    -- Add FK_Buildings_InstallationMethods_InstallationMethodId if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Buildings_InstallationMethods_InstallationMethodId'
                        AND table_name = 'Buildings'
                    ) THEN
                        ALTER TABLE ""Buildings""
                        ADD CONSTRAINT ""FK_Buildings_InstallationMethods_InstallationMethodId""
                        FOREIGN KEY (""InstallationMethodId"")
                        REFERENCES ""InstallationMethods"" (""Id"")
                        ON DELETE SET NULL;
                    END IF;
                END $$;
            ");

            // Step 9: Drop old InstallationTypes table (data already copied)
            migrationBuilder.DropTable(
                name: "InstallationTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate InstallationTypes table and copy data back
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""InstallationTypes"" (
                    ""Id"" uuid NOT NULL,
                    ""DepartmentId"" uuid NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Code"" character varying(50) NOT NULL,
                    ""Description"" character varying(500) NULL,
                    ""IsActive"" boolean NOT NULL,
                    ""DisplayOrder"" integer NOT NULL,
                    ""CompanyId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    ""IsDeleted"" boolean NOT NULL,
                    ""DeletedAt"" timestamp with time zone NULL,
                    ""DeletedByUserId"" uuid NULL,
                    ""RowVersion"" bytea NULL,
                    CONSTRAINT ""PK_InstallationTypes"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_InstallationTypes_Departments_DepartmentId"" FOREIGN KEY (""DepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL
                );

                -- Copy data from OrderCategories back to InstallationTypes
                INSERT INTO ""InstallationTypes"" (
                    ""Id"", ""DepartmentId"", ""Name"", ""Code"", ""Description"", ""IsActive"", 
                    ""DisplayOrder"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", 
                    ""DeletedAt"", ""DeletedByUserId"", ""RowVersion""
                )
                SELECT 
                    ""Id"", ""DepartmentId"", ""Name"", ""Code"", ""Description"", ""IsActive"", 
                    ""DisplayOrder"", ""CompanyId"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", 
                    ""DeletedAt"", ""DeletedByUserId"", ""RowVersion""
                FROM ""OrderCategories"";
            ");

            // Drop foreign keys only if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop FK_Buildings_InstallationMethods_InstallationMethodId if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Buildings_InstallationMethods_InstallationMethodId'
                        AND table_name = 'Buildings'
                    ) THEN
                        ALTER TABLE ""Buildings""
                        DROP CONSTRAINT IF EXISTS ""FK_Buildings_InstallationMethods_InstallationMethodId"";
                    END IF;

                    -- Drop FK_Orders_OrderCategories_OrderCategoryId if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Orders_OrderCategories_OrderCategoryId'
                        AND table_name = 'Orders'
                    ) THEN
                        ALTER TABLE ""Orders""
                        DROP CONSTRAINT IF EXISTS ""FK_Orders_OrderCategories_OrderCategoryId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropTable(
                name: "OrderCategories");

            // Drop index only if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE schemaname = 'public' 
                        AND tablename = 'Buildings' 
                        AND indexname = 'IX_Buildings_InstallationMethodId'
                    ) THEN
                        DROP INDEX IF EXISTS ""IX_Buildings_InstallationMethodId"";
                    END IF;
                END $$;
            ");

            // Drop columns only if they exist (they may have been added by manual SQL migration)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop AdditionalContactNumber from ParsedOrderDrafts if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'AdditionalContactNumber'
                    ) THEN
                        ALTER TABLE ""ParsedOrderDrafts"" DROP COLUMN ""AdditionalContactNumber"";
                    END IF;

                    -- Drop Issue from ParsedOrderDrafts if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'Issue'
                    ) THEN
                        ALTER TABLE ""ParsedOrderDrafts"" DROP COLUMN ""Issue"";
                    END IF;

                    -- Drop Issue from Orders if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'Issue'
                    ) THEN
                        ALTER TABLE ""Orders"" DROP COLUMN ""Issue"";
                    END IF;

                    -- Drop Solution from Orders if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'Solution'
                    ) THEN
                        ALTER TABLE ""Orders"" DROP COLUMN ""Solution"";
                    END IF;
                END $$;
            ");

            migrationBuilder.RenameColumn(
                name: "OrderCategory",
                table: "PnlDetailPerOrders",
                newName: "InstallationType");

            migrationBuilder.RenameColumn(
                name: "OrderCategoryId",
                table: "Orders",
                newName: "InstallationTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderTypeId_OrderCategoryId_InstallationMethodId",
                table: "Orders",
                newName: "IX_Orders_OrderTypeId_InstallationTypeId_InstallationMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_OrderCategoryId",
                table: "Orders",
                newName: "IX_Orders_InstallationTypeId");

            migrationBuilder.RenameColumn(
                name: "OrderCategoryId",
                table: "GponSiJobRates",
                newName: "InstallationTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_OrderCategoryId",
                table: "GponSiJobRates",
                newName: "IX_GponSiJobRates_PartnerGroupId_OrderTypeId_InstallationTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiJobRates_OrderTypeId_OrderCategoryId_InstallationMeth~",
                table: "GponSiJobRates",
                newName: "IX_GponSiJobRates_OrderTypeId_InstallationTypeId_InstallationM~");

            migrationBuilder.RenameColumn(
                name: "OrderCategoryId",
                table: "GponSiCustomRates",
                newName: "InstallationTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_OrderCateg~",
                table: "GponSiCustomRates",
                newName: "IX_GponSiCustomRates_ServiceInstallerId_OrderTypeId_Installati~");

            migrationBuilder.RenameColumn(
                name: "OrderCategoryId",
                table: "GponPartnerJobRates",
                newName: "InstallationTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_GponPartnerJobRates_PartnerId_OrderTypeId_OrderCategoryId_I~",
                table: "GponPartnerJobRates",
                newName: "IX_GponPartnerJobRates_PartnerId_OrderTypeId_InstallationTypeI~");

            migrationBuilder.RenameIndex(
                name: "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_OrderCategor~",
                table: "GponPartnerJobRates",
                newName: "IX_GponPartnerJobRates_PartnerGroupId_OrderTypeId_Installation~");

            migrationBuilder.AddColumn<Guid>(
                name: "BuildingTypeId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingTypeId",
                table: "Buildings",
                column: "BuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationTypes_CompanyId_Code",
                table: "InstallationTypes",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallationTypes_CompanyId_DepartmentId",
                table: "InstallationTypes",
                columns: new[] { "CompanyId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallationTypes_CompanyId_IsActive",
                table: "InstallationTypes",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallationTypes_DepartmentId",
                table: "InstallationTypes",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_BuildingTypes_BuildingTypeId",
                table: "Buildings",
                column: "BuildingTypeId",
                principalTable: "BuildingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_InstallationTypes_InstallationTypeId",
                table: "Orders",
                column: "InstallationTypeId",
                principalTable: "InstallationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
