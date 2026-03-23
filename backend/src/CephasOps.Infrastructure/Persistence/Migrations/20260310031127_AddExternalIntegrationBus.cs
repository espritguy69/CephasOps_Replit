using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIntegrationBus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "TaskItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderCategoryId",
                table: "ParsedOrderDrafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnmatchedMaterialCount",
                table: "ParsedOrderDrafts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnmatchedMaterialNamesJson",
                table: "ParsedOrderDrafts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CausationId",
                table: "EventStore",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "EventStore",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastClaimedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastClaimedBy",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorType",
                table: "EventStore",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartitionKey",
                table: "EventStore",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadVersion",
                table: "EventStore",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "EventStore",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingLeaseExpiresAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingNodeId",
                table: "EventStore",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAtUtc",
                table: "EventStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplayId",
                table: "EventStore",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RootEventId",
                table: "EventStore",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceModule",
                table: "EventStore",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceService",
                table: "EventStore",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpanId",
                table: "EventStore",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "EventStore",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAtUtc",
                table: "BackgroundJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerId",
                table: "BackgroundJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommandProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CommandType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResultJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandProcessingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ConnectorType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventProcessingLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProcessingLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventStoreAttemptHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StackTraceSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WasRetried = table.Column<bool>(type: "boolean", nullable: false),
                    WasDeadLettered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStoreAttemptHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ConnectorKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboundWebhookReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundWebhookReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalIdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConnectorKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    VerificationPassed = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationFailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HandlerErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HandlerAttemptCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundWebhookReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastErrorAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProcessingLeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LedgerFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayloadSnapshot = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderingStrategyId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Target = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastErrorAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessingNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProcessingLeaseExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundIntegrationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RootEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    SignatureHeaderValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    IsReplay = table.Column<bool>(type: "boolean", nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundIntegrationDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParsedMaterialAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AliasText = table.Column<string>(type: "text", nullable: false),
                    NormalizedAliasText = table.Column<string>(type: "text", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParsedMaterialAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RebuildExecutionLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RebuildTargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RebuildOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebuildExecutionLocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RebuildOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RebuildTargetId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScopeCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ToOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    BackgroundJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowsDeleted = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    SourceRecordCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResumeRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckpointAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastProcessedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastProcessedOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedCountAtLastCheckpoint = table.Column<int>(type: "integer", nullable: false),
                    CheckpointCount = table.Column<int>(type: "integer", nullable: false),
                    RetriedFromOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RerunReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebuildOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplayExecutionLock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayExecutionLock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplayOperationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplayOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SkippedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayOperationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplayOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    ReplayReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FromOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ToOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MaxEvents = table.Column<int>(type: "integer", nullable: true),
                    TotalMatched = table.Column<int>(type: "integer", nullable: true),
                    TotalEligible = table.Column<int>(type: "integer", nullable: true),
                    TotalExecuted = table.Column<int>(type: "integer", nullable: true),
                    TotalSucceeded = table.Column<int>(type: "integer", nullable: true),
                    TotalFailed = table.Column<int>(type: "integer", nullable: true),
                    ReplayCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplayTarget = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReplayMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    SkippedCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BackgroundJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResumeRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckpointAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastProcessedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastProcessedOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckpointCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedCountAtLastCheckpoint = table.Column<int>(type: "integer", nullable: true),
                    OrderingStrategyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RetriedFromOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RerunReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SafetyCutoffOccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SafetyWindowMinutes = table.Column<int>(type: "integer", nullable: true),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaBreaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    WarningThresholdSeconds = table.Column<int>(type: "integer", nullable: true),
                    EscalationThresholdSeconds = table.Column<int>(type: "integer", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkerInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProcessId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitionHistory",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitionHistory", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndpointUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AllowedEventTypes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SigningConfigJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AuthConfigJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsPaused = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectorEndpoints_ConnectorDefinitions_ConnectorDefinition~",
                        column: x => x.ConnectorDefinitionId,
                        principalTable: "ConnectorDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboundIntegrationAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboundDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBodySnippet = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundIntegrationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundIntegrationAttempts_OutboundIntegrationDeliveries_O~",
                        column: x => x.OutboundDeliveryId,
                        principalTable: "OutboundIntegrationDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CompensationDataJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_CompanyId_OrderId",
                table: "TaskItems",
                columns: new[] { "CompanyId", "OrderId" },
                filter: "\"OrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_PartitionKey",
                table: "EventStore",
                column: "PartitionKey",
                filter: "\"PartitionKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_PartitionKey_CreatedAtUtc_EventId",
                table: "EventStore",
                columns: new[] { "PartitionKey", "CreatedAtUtc", "EventId" },
                filter: "\"PartitionKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_ReplayId",
                table: "EventStore",
                column: "ReplayId",
                filter: "\"ReplayId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_RootEventId",
                table: "EventStore",
                column: "RootEventId",
                filter: "\"RootEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_Status_NextRetryAtUtc",
                table: "EventStore",
                columns: new[] { "Status", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_State_WorkerId",
                table: "BackgroundJobs",
                columns: new[] { "State", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_WorkerId",
                table: "BackgroundJobs",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandProcessingLogs_IdempotencyKey",
                table: "CommandProcessingLogs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandProcessingLogs_Status_CreatedAtUtc",
                table: "CommandProcessingLogs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorDefinitions_ConnectorKey",
                table: "ConnectorDefinitions",
                column: "ConnectorKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorEndpoints_CompanyId",
                table: "ConnectorEndpoints",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorEndpoints_ConnectorDefinitionId_CompanyId",
                table: "ConnectorEndpoints",
                columns: new[] { "ConnectorDefinitionId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_EventId",
                table: "EventProcessingLog",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_EventId_HandlerName",
                table: "EventProcessingLog",
                columns: new[] { "EventId", "HandlerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_ReplayOperationId",
                table: "EventProcessingLog",
                column: "ReplayOperationId",
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventProcessingLog_State_StartedAtUtc",
                table: "EventProcessingLog",
                columns: new[] { "State", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreAttemptHistory_EventId",
                table: "EventStoreAttemptHistory",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreAttemptHistory_EventId_AttemptNumber",
                table: "EventStoreAttemptHistory",
                columns: new[] { "EventId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdempotencyRecords_ConnectorKey_CompletedAtUtc",
                table: "ExternalIdempotencyRecords",
                columns: new[] { "ConnectorKey", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdempotencyRecords_IdempotencyKey",
                table: "ExternalIdempotencyRecords",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookReceipts_CompanyId",
                table: "InboundWebhookReceipts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookReceipts_ConnectorKey_ExternalIdempotencyKey",
                table: "InboundWebhookReceipts",
                columns: new[] { "ConnectorKey", "ExternalIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookReceipts_ConnectorKey_Status_ReceivedAtUtc",
                table: "InboundWebhookReceipts",
                columns: new[] { "ConnectorKey", "Status", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_CompanyId_Status",
                table: "JobExecutions",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Status_NextRunAtUtc",
                table: "JobExecutions",
                columns: new[] { "Status", "NextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_CompanyId_LedgerFamily_OccurredAtUtc",
                table: "LedgerEntries",
                columns: new[] { "CompanyId", "LedgerFamily", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_EntityType_EntityId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "EntityType", "EntityId", "LedgerFamily" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_RecordedAtUtc",
                table: "LedgerEntries",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReplayOperationId",
                table: "LedgerEntries",
                column: "ReplayOperationId",
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReplayOperationId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "ReplayOperationId", "LedgerFamily" },
                unique: true,
                filter: "\"ReplayOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_SourceEventId",
                table: "LedgerEntries",
                column: "SourceEventId",
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_SourceEventId_LedgerFamily",
                table: "LedgerEntries",
                columns: new[] { "SourceEventId", "LedgerFamily" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDispatches_CompanyId_Status",
                table: "NotificationDispatches",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDispatches_IdempotencyKey",
                table: "NotificationDispatches",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDispatches_Status_NextRetryAtUtc",
                table: "NotificationDispatches",
                columns: new[] { "Status", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationAttempts_OutboundDeliveryId",
                table: "OutboundIntegrationAttempts",
                column: "OutboundDeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationDeliveries_CompanyId_Status_CreatedAtUtc",
                table: "OutboundIntegrationDeliveries",
                columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationDeliveries_ConnectorEndpointId_Status_Cr~",
                table: "OutboundIntegrationDeliveries",
                columns: new[] { "ConnectorEndpointId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationDeliveries_EventType_Status",
                table: "OutboundIntegrationDeliveries",
                columns: new[] { "EventType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationDeliveries_IdempotencyKey",
                table: "OutboundIntegrationDeliveries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationDeliveries_NextRetryAtUtc",
                table: "OutboundIntegrationDeliveries",
                column: "NextRetryAtUtc",
                filter: "\"NextRetryAtUtc\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_RebuildOperationId",
                table: "RebuildExecutionLocks",
                column: "RebuildOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_RebuildTargetId_ScopeKey",
                table: "RebuildExecutionLocks",
                columns: new[] { "RebuildTargetId", "ScopeKey" },
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildExecutionLocks_ReleasedAtUtc",
                table: "RebuildExecutionLocks",
                column: "ReleasedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_BackgroundJobId",
                table: "RebuildOperations",
                column: "BackgroundJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_RebuildTargetId",
                table: "RebuildOperations",
                column: "RebuildTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_RequestedAtUtc",
                table: "RebuildOperations",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_ScopeCompanyId_RequestedAtUtc",
                table: "RebuildOperations",
                columns: new[] { "ScopeCompanyId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_State_RequestedAtUtc",
                table: "RebuildOperations",
                columns: new[] { "State", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RebuildOperations_WorkerId",
                table: "RebuildOperations",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_CompanyId",
                table: "ReplayExecutionLock",
                column: "CompanyId",
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_ReleasedAtUtc",
                table: "ReplayExecutionLock",
                column: "ReleasedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayExecutionLock_ReplayOperationId",
                table: "ReplayExecutionLock",
                column: "ReplayOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperationEvents_EventId",
                table: "ReplayOperationEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperationEvents_ReplayOperationId",
                table: "ReplayOperationEvents",
                column: "ReplayOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_CompanyId_RequestedAtUtc",
                table: "ReplayOperations",
                columns: new[] { "CompanyId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_CompanyId_State_RequestedAtUtc",
                table: "ReplayOperations",
                columns: new[] { "CompanyId", "State", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_RequestedAtUtc",
                table: "ReplayOperations",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_RequestedByUserId",
                table: "ReplayOperations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_RetriedFromOperationId",
                table: "ReplayOperations",
                column: "RetriedFromOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayOperations_WorkerId",
                table: "ReplayOperations",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId",
                table: "SlaBreaches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId_Severity",
                table: "SlaBreaches",
                columns: new[] { "CompanyId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CompanyId_Status_DetectedAtUtc",
                table: "SlaBreaches",
                columns: new[] { "CompanyId", "Status", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_CorrelationId",
                table: "SlaBreaches",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_RuleId",
                table: "SlaBreaches",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId",
                table: "SlaRules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId_Enabled_RuleType",
                table: "SlaRules",
                columns: new[] { "CompanyId", "Enabled", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaRules_CompanyId_TargetType_TargetName",
                table: "SlaRules",
                columns: new[] { "CompanyId", "TargetType", "TargetName" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInstances_IsActive_LastHeartbeatUtc",
                table: "WorkerInstances",
                columns: new[] { "IsActive", "LastHeartbeatUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInstances_LastHeartbeatUtc",
                table: "WorkerInstances",
                column: "LastHeartbeatUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_CorrelationId",
                table: "WorkflowInstances",
                column: "CorrelationId",
                filter: "\"CorrelationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_WorkflowType_EntityType_EntityId",
                table: "WorkflowInstances",
                columns: new[] { "WorkflowType", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowInstanceId",
                table: "WorkflowSteps",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionHistory_CompanyId_EntityType_EntityId",
                table: "WorkflowTransitionHistory",
                columns: new[] { "CompanyId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionHistory_OccurredAtUtc",
                table: "WorkflowTransitionHistory",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandProcessingLogs");

            migrationBuilder.DropTable(
                name: "ConnectorEndpoints");

            migrationBuilder.DropTable(
                name: "EventProcessingLog");

            migrationBuilder.DropTable(
                name: "EventStoreAttemptHistory");

            migrationBuilder.DropTable(
                name: "ExternalIdempotencyRecords");

            migrationBuilder.DropTable(
                name: "InboundWebhookReceipts");

            migrationBuilder.DropTable(
                name: "JobExecutions");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "NotificationDispatches");

            migrationBuilder.DropTable(
                name: "OutboundIntegrationAttempts");

            migrationBuilder.DropTable(
                name: "ParsedMaterialAliases");

            migrationBuilder.DropTable(
                name: "RebuildExecutionLocks");

            migrationBuilder.DropTable(
                name: "RebuildOperations");

            migrationBuilder.DropTable(
                name: "ReplayExecutionLock");

            migrationBuilder.DropTable(
                name: "ReplayOperationEvents");

            migrationBuilder.DropTable(
                name: "ReplayOperations");

            migrationBuilder.DropTable(
                name: "SlaBreaches");

            migrationBuilder.DropTable(
                name: "SlaRules");

            migrationBuilder.DropTable(
                name: "WorkerInstances");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "WorkflowTransitionHistory");

            migrationBuilder.DropTable(
                name: "ConnectorDefinitions");

            migrationBuilder.DropTable(
                name: "OutboundIntegrationDeliveries");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_CompanyId_OrderId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_EventStore_PartitionKey",
                table: "EventStore");

            migrationBuilder.DropIndex(
                name: "IX_EventStore_PartitionKey_CreatedAtUtc_EventId",
                table: "EventStore");

            migrationBuilder.DropIndex(
                name: "IX_EventStore_ReplayId",
                table: "EventStore");

            migrationBuilder.DropIndex(
                name: "IX_EventStore_RootEventId",
                table: "EventStore");

            migrationBuilder.DropIndex(
                name: "IX_EventStore_Status_NextRetryAtUtc",
                table: "EventStore");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_State_WorkerId",
                table: "BackgroundJobs");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_WorkerId",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OrderCategoryId",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "UnmatchedMaterialCount",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "UnmatchedMaterialNamesJson",
                table: "ParsedOrderDrafts");

            migrationBuilder.DropColumn(
                name: "CapturedAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "CausationId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastClaimedAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastClaimedBy",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "LastErrorType",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "PartitionKey",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "PayloadVersion",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingLeaseExpiresAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingNodeId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAtUtc",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ReplayId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "RootEventId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "SourceModule",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "SourceService",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "SpanId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "ClaimedAtUtc",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "BackgroundJobs");
        }
    }
}
