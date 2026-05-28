using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalCashDrawerMovementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalCashDrawerMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorizedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    MovementType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SyncedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalCashDrawerMovements", x => x.Id);
                    table.CheckConstraint("CK_LocalCashDrawerMovement_Amount", "Amount > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_CorrelationId", "length(CorrelationId) > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_EmployeeId", "EmployeeId > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_IdempotencyKey", "length(IdempotencyKey) > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_ReasonCodeId", "ReasonCodeId > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_ShiftId", "ShiftId <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_TerminalId", "TerminalId > 0");
                    table.CheckConstraint("CK_LocalCashDrawerMovement_TerminalSequence", "TerminalSequence > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCashDrawerMovements_Tenant_Location_Terminal_BusinessDate",
                table: "LocalCashDrawerMovements",
                columns: new[] { "TenantId", "LocationId", "TerminalId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCashDrawerMovements_Tenant_ReasonCodeId",
                table: "LocalCashDrawerMovements",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCashDrawerMovements_Tenant_ShiftId",
                table: "LocalCashDrawerMovements",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalCashDrawerMovements_Tenant_IdempotencyKey",
                table: "LocalCashDrawerMovements",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LocalCashDrawerMovements_Tenant_Location_Terminal_Sequence",
                table: "LocalCashDrawerMovements",
                columns: new[] { "TenantId", "LocationId", "TerminalId", "TerminalSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalCashDrawerMovements");
        }
    }
}
