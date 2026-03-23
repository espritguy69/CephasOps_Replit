using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialPartnersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table already exists (may have been created via SQL script)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'MaterialPartners') THEN
                        CREATE TABLE ""MaterialPartners"" (
                            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                            ""MaterialId"" UUID NOT NULL,
                            ""PartnerId"" UUID NOT NULL,
                            ""CompanyId"" UUID NOT NULL,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE,
                            ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""DeletedAt"" TIMESTAMP WITH TIME ZONE,
                            ""DeletedByUserId"" UUID,
                            ""RowVersion"" BYTEA,
                            CONSTRAINT ""FK_MaterialPartners_Materials_MaterialId"" FOREIGN KEY (""MaterialId"")
                                REFERENCES ""Materials""(""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_MaterialPartners_Partners_PartnerId"" FOREIGN KEY (""PartnerId"")
                                REFERENCES ""Partners""(""Id"") ON DELETE RESTRICT,
                            CONSTRAINT ""UQ_MaterialPartners_CompanyId_MaterialId_PartnerId"" 
                                UNIQUE (""CompanyId"", ""MaterialId"", ""PartnerId"")
                        );

                        CREATE INDEX IF NOT EXISTS ""IX_MaterialPartners_CompanyId"" ON ""MaterialPartners"" (""CompanyId"");
                        CREATE INDEX IF NOT EXISTS ""IX_MaterialPartners_MaterialId"" ON ""MaterialPartners"" (""MaterialId"");
                        CREATE INDEX IF NOT EXISTS ""IX_MaterialPartners_PartnerId"" ON ""MaterialPartners"" (""PartnerId"");
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MaterialPartners_CompanyId_MaterialId_PartnerId"" 
                            ON ""MaterialPartners"" (""CompanyId"", ""MaterialId"", ""PartnerId"");
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialPartners");
        }
    }
}
