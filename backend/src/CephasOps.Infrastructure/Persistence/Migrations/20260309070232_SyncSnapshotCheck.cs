using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshotCheck : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_CompanyId_OrderId",
                table: "TaskItems",
                columns: new[] { "CompanyId", "OrderId" },
                filter: "\"OrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_State_WorkerId",
                table: "BackgroundJobs",
                columns: new[] { "State", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_WorkerId",
                table: "BackgroundJobs",
                column: "WorkerId");

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
                name: "EventProcessingLog");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

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
                name: "WorkflowTransitionHistory");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_CompanyId_OrderId",
                table: "TaskItems");

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
                name: "ClaimedAtUtc",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "BackgroundJobs");
        }
    }
}
