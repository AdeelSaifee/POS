using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddTerminalProvisioningTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TerminalProvisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: true),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalProvisioning", x => x.Id);
                    table.CheckConstraint("CK_TerminalProvisioning_Id", "Id = 1");
                    table.CheckConstraint("CK_TerminalProvisioning_LocationId", "LocationId IS NULL OR LocationId > 0");
                    table.CheckConstraint("CK_TerminalProvisioning_TenantId", "TenantId IS NULL OR TenantId > 0");
                    table.CheckConstraint("CK_TerminalProvisioning_TerminalId", "TerminalId IS NULL OR TerminalId > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminalProvisioning");
        }
    }
}
