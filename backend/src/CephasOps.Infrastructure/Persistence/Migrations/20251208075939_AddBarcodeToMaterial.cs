using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeToMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Barcode column to Materials (only if it doesn't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Materials' AND column_name = 'Barcode'
                    ) THEN
                        ALTER TABLE ""Materials"" ADD COLUMN ""Barcode"" character varying(200) NULL;
                    END IF;
                END $$;
            ");

            // Create index (only if it doesn't exist)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_Materials_CompanyId_Barcode'
                    ) THEN
                        CREATE UNIQUE INDEX ""IX_Materials_CompanyId_Barcode"" 
                        ON ""Materials"" (""CompanyId"", ""Barcode"") 
                        WHERE ""Barcode"" IS NOT NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index (only if it exists)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_Materials_CompanyId_Barcode'
                    ) THEN
                        DROP INDEX ""IX_Materials_CompanyId_Barcode"";
                    END IF;
                END $$;
            ");

            // Drop Barcode column (only if it exists)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Materials' AND column_name = 'Barcode'
                    ) THEN
                        ALTER TABLE ""Materials"" DROP COLUMN ""Barcode"";
                    END IF;
                END $$;
            ");
        }
    }
}
