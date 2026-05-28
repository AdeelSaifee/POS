using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalTerminalSessionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalTerminalSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ShiftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    LoggedInOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LoggedOutOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalTerminalSessions", x => x.Id);
                    table.CheckConstraint("CK_LocalTerminalSession_DisplayName_NotEmpty", "DisplayName <> ''");
                    table.CheckConstraint("CK_LocalTerminalSession_EmployeeId", "EmployeeId > 0");
                    table.CheckConstraint("CK_LocalTerminalSession_EmployeeNumber_NotEmpty", "EmployeeNumber <> ''");
                    table.CheckConstraint("CK_LocalTerminalSession_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalTerminalSession_Role_NotEmpty", "Role <> ''");
                    table.CheckConstraint("CK_LocalTerminalSession_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalTerminalSession_TerminalId", "TerminalId > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalTerminalSessions_Tenant_Employee",
                table: "LocalTerminalSessions",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalTerminalSessions_Tenant_Status",
                table: "LocalTerminalSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalTerminalSessions_Tenant_Terminal_Sequence",
                table: "LocalTerminalSessions",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalTerminalSessions");
        }
    }
}
