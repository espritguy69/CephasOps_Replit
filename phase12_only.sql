START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE TABLE "BillingPlans" (
        "Id" uuid NOT NULL,
        "Name" character varying(256) NOT NULL,
        "Slug" character varying(64) NOT NULL,
        "BillingCycle" integer NOT NULL,
        "Price" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BillingPlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE TABLE "TenantInvoices" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "TenantSubscriptionId" uuid,
        "InvoiceNumber" character varying(64) NOT NULL,
        "PeriodStartUtc" timestamp with time zone NOT NULL,
        "PeriodEndUtc" timestamp with time zone NOT NULL,
        "SubTotal" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "DueDateUtc" timestamp with time zone,
        "PaidAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantInvoices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE TABLE "TenantSubscriptions" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "BillingPlanId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "StartedAtUtc" timestamp with time zone NOT NULL,
        "CurrentPeriodEndUtc" timestamp with time zone,
        "ExternalSubscriptionId" character varying(256),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantSubscriptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE TABLE "TenantUsageRecords" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "MetricKey" character varying(64) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "PeriodStartUtc" timestamp with time zone NOT NULL,
        "PeriodEndUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantUsageRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_BillingPlans_IsActive" ON "BillingPlans" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE UNIQUE INDEX "IX_BillingPlans_Slug" ON "BillingPlans" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantInvoices_TenantId" ON "TenantInvoices" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE UNIQUE INDEX "IX_TenantInvoices_TenantId_InvoiceNumber" ON "TenantInvoices" ("TenantId", "InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantSubscriptions_BillingPlanId" ON "TenantSubscriptions" ("BillingPlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantSubscriptions_ExternalSubscriptionId" ON "TenantSubscriptions" ("ExternalSubscriptionId") WHERE "ExternalSubscriptionId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantSubscriptions_Status" ON "TenantSubscriptions" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantSubscriptions_TenantId" ON "TenantSubscriptions" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    CREATE INDEX "IX_TenantUsageRecords_TenantId_MetricKey_PeriodStartUtc" ON "TenantUsageRecords" ("TenantId", "MetricKey", "PeriodStartUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310041112_Phase12_SubscriptionBilling') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310041112_Phase12_SubscriptionBilling', '10.0.3');
    END IF;
END $EF$;
COMMIT;

