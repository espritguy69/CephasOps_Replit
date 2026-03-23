START TRANSACTION;
CREATE TABLE "OrderStatusChecklistItems" (
    "Id" uuid NOT NULL,
    "StatusCode" character varying(50) NOT NULL,
    "ParentChecklistItemId" uuid,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "OrderIndex" integer NOT NULL,
    "IsRequired" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedByUserId" uuid,
    "UpdatedByUserId" uuid,
    "CompanyId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedByUserId" uuid,
    "RowVersion" bytea,
    CONSTRAINT "PK_OrderStatusChecklistItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderStatusChecklistItems_OrderStatusChecklistItems_ParentC~" FOREIGN KEY ("ParentChecklistItemId") REFERENCES "OrderStatusChecklistItems" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "OrderStatusChecklistAnswers" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ChecklistItemId" uuid NOT NULL,
    "Answer" boolean NOT NULL,
    "AnsweredAt" timestamp with time zone NOT NULL,
    "AnsweredByUserId" uuid NOT NULL,
    "Remarks" character varying(1000),
    "CompanyId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "DeletedByUserId" uuid,
    "RowVersion" bytea,
    CONSTRAINT "PK_OrderStatusChecklistAnswers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderStatusChecklistAnswers_OrderStatusChecklistItems_Check~" FOREIGN KEY ("ChecklistItemId") REFERENCES "OrderStatusChecklistItems" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderStatusChecklistAnswers_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_OrderStatusChecklistAnswers_ChecklistItemId" ON "OrderStatusChecklistAnswers" ("ChecklistItemId");

CREATE INDEX "IX_OrderStatusChecklistAnswers_CompanyId_OrderId" ON "OrderStatusChecklistAnswers" ("CompanyId", "OrderId");

CREATE UNIQUE INDEX "IX_OrderStatusChecklistAnswers_OrderId_ChecklistItemId" ON "OrderStatusChecklistAnswers" ("OrderId", "ChecklistItemId");

CREATE INDEX "IX_OrderStatusChecklistItems_CompanyId_StatusCode_IsActive" ON "OrderStatusChecklistItems" ("CompanyId", "StatusCode", "IsActive");

CREATE INDEX "IX_OrderStatusChecklistItems_ParentChecklistItemId" ON "OrderStatusChecklistItems" ("ParentChecklistItemId");

CREATE INDEX "IX_OrderStatusChecklistItems_StatusCode_OrderIndex" ON "OrderStatusChecklistItems" ("StatusCode", "OrderIndex");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251207154424_AddOrderStatusChecklist', '10.0.0');

COMMIT;

