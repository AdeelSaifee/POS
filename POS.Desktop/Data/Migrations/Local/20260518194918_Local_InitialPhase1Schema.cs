using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class Local_InitialPhase1Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalRecoveryJournal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecoveryType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StatePayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredAction = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ResolvedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResolvedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ResolutionComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RetainUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalRecoveryJournal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalRetentionState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCleanupOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    OldestRetainedBusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalRetentionState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliationQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenderMethodId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalPaymentReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PaymentToken = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NextAttemptOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastAttemptOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastResultCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LastResultMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RetainUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliationQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrintQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ZReportId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PrintJobType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceiptTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    RenderedContent = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PrintedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RetainUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncCursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LastPullToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastSuccessfulPullOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastPushedChunkSequence = table.Column<long>(type: "INTEGER", nullable: true),
                    LastAckedChunkSequence = table.Column<long>(type: "INTEGER", nullable: true),
                    ServerBackoffUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncCursors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChunkSequence = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AckedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetainUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalRecoveryJournal_CorrelationId",
                table: "LocalRecoveryJournal",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalRecoveryJournal_Status_Type",
                table: "LocalRecoveryJournal",
                columns: new[] { "Status", "RecoveryType" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalRecoveryJournal_Tenant_Order",
                table: "LocalRecoveryJournal",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalRecoveryJournal_Tenant_IdempotencyKey",
                table: "LocalRecoveryJournal",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalRetentionState_Status",
                table: "LocalRetentionState",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_LocalRetentionState_Tenant_Terminal_Category",
                table: "LocalRetentionState",
                columns: new[] { "TenantId", "TerminalId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationQueue_CorrelationId",
                table: "PaymentReconciliationQueue",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationQueue_Status_NextAttempt",
                table: "PaymentReconciliationQueue",
                columns: new[] { "Status", "NextAttemptOn" });

            migrationBuilder.CreateIndex(
                name: "UX_PaymentReconciliationQueue_Tenant_IdempotencyKey",
                table: "PaymentReconciliationQueue",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PaymentReconciliationQueue_Tenant_Payment",
                table: "PaymentReconciliationQueue",
                columns: new[] { "TenantId", "PaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintQueue_CorrelationId",
                table: "PrintQueue",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintQueue_Status_Priority",
                table: "PrintQueue",
                columns: new[] { "Status", "Priority", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintQueue_Tenant_Order",
                table: "PrintQueue",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "UX_PrintQueue_Tenant_IdempotencyKey",
                table: "PrintQueue",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncCursors_Status",
                table: "SyncCursors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_SyncCursors_Tenant_Terminal_Stream",
                table: "SyncCursors",
                columns: new[] { "TenantId", "TerminalId", "StreamName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutbox_CorrelationId",
                table: "SyncOutbox",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutbox_Status_Order",
                table: "SyncOutbox",
                columns: new[] { "Status", "BusinessDate", "TerminalSequence" });

            migrationBuilder.CreateIndex(
                name: "UX_SyncOutbox_Tenant_IdempotencyKey",
                table: "SyncOutbox",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SyncOutbox_Tenant_Terminal_Sequence_Event",
                table: "SyncOutbox",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence", "EventType", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalRecoveryJournal");

            migrationBuilder.DropTable(
                name: "LocalRetentionState");

            migrationBuilder.DropTable(
                name: "PaymentReconciliationQueue");

            migrationBuilder.DropTable(
                name: "PrintQueue");

            migrationBuilder.DropTable(
                name: "SyncCursors");

            migrationBuilder.DropTable(
                name: "SyncOutbox");
        }
    }
}
