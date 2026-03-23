using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CephasOps.Infrastructure.Persistence.Migrations;

/// <summary>
/// No-op migration. Phase 7 schema (EventStore lease columns + EventStoreAttemptHistory) is provided by
/// 20260309065950_VerifyNoPending. This migration remains in the chain so existing migration history is valid;
/// Up/Down do nothing to avoid duplicate column/table creation when 20260309065950 was already applied.
/// See docs/DISTRIBUTED_PLATFORM_PHASE7_CLOSURE.md and apply-EventStorePhase7LeaseAndAttemptHistory.sql for script-only path.
/// </summary>
public partial class AddEventStorePhase7LeaseAndAttemptHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No-op: Phase 7 schema added by 20260309065950_VerifyNoPending.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op.
    }
}
