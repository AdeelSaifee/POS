using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalShiftsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OpeningCashAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExpectedCashAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CountedCashAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    VarianceAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    OpenedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SyncedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalShifts", x => x.Id);
                    table.CheckConstraint("CK_LocalShift_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalShift_OpenedByEmployeeId", "OpenedByEmployeeId > 0");
                    table.CheckConstraint("CK_LocalShift_OpeningCashAmount", "OpeningCashAmount > 0");
                    table.CheckConstraint("CK_LocalShift_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalShift_TerminalId", "TerminalId > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalShifts_Tenant_Terminal_Status",
                table: "LocalShifts",
                columns: new[] { "TenantId", "TerminalId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalShifts_Tenant_Terminal_Sequence",
                table: "LocalShifts",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalShifts");
        }
    }
}
